using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class MvcCartChecks
{
    private static StoreDbContext Open(string connection) => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
    public static async Task Run()
    {
        await using var database = await TestDatabase.Create();
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "PhoneStore.slnx"))) root = root.Parent;
        await using var factory = new MvcFoundationFactory(database.ConnectionString, root!.FullName);
        MvcCatalogFixture data; long otherCustomer;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync();
            data = await MvcCatalogFixture.Seed(scope.ServiceProvider);
            otherCustomer = (await scope.ServiceProvider.GetRequiredService<IAccountRegistration>().RegisterAsync(new("cart-other-qa", MvcCatalogFixture.Password, "Khách QA khác", "0938123599"))).Id;
        }
        using var guest = factory.Browser(); using var customer = factory.Browser(); using var other = factory.Browser();
        async Task<string> Token(HttpClient client, string path = "/Cart/Index")
        {
            using var response = await client.GetAsync(path); response.EnsureSuccessStatusCode(); var html = await response.Content.ReadAsStringAsync();
            var match = Regex.Match(html, "name=\"csrf-token\" content=\"([^\"]+)\""); Check.True(match.Success, "Missing real Razor CSRF header token."); return WebUtility.HtmlDecode(match.Groups[1].Value);
        }
        async Task<HttpResponseMessage> Post(HttpClient client, string action, Dictionary<string,string> values)
        { values["__RequestVerificationToken"] = await Token(client, action == "/Account/Login" ? action : "/Cart/Index"); return await client.PostAsync(action, new FormUrlEncodedContent(values)); }
        async Task<HttpResponseMessage> Json(HttpClient client, string action, object body, string? token = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/Cart/" + action) { Content = JsonContent.Create(body) };
            request.Headers.Add("X-CSRF-TOKEN", token ?? await Token(client)); return await client.SendAsync(request);
        }
        async Task<string> CartSnapshot()
        { await using var db = Open(database.ConnectionString); return JsonSerializer.Serialize(await db.Cart.AsNoTracking().OrderBy(x => x.CustomerId).ThenBy(x => x.VariantId).ToListAsync()); }
        async Task<string> ProtectedSnapshot()
        {
            await using var db = Open(database.ConnectionString);
            return JsonSerializer.Serialize(new { Inventory = await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync(), Journal = await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(), Orders = await db.Orders.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(), Payments = await db.Payments.AsNoTracking().OrderBy(x=>x.OrderId).ToListAsync() });
        }
        async Task<int> Quantity(long customerId, long variant)
        { await using var db = Open(database.ConnectionString); return await db.Cart.Where(x=>x.CustomerId==customerId && x.VariantId==variant).Select(x=>x.Quantity).SingleOrDefaultAsync(); }
        Dictionary<string,string> Add(long product, long variant, string quantity) => new() { ["productId"] = product.ToString(), ["variantId"] = variant.ToString(), ["quantity"] = quantity };
        void Redirect(HttpResponseMessage response) => Check.True(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location?.OriginalString == "/Cart/Index", "Cart write did not PRG to its index.");
        var protectedBefore = await ProtectedSnapshot();
        await Check.Run("WEB.K10.03 Quote is public server-priced sanitized and leaves all cart/inventory SQL unchanged", async () =>
        {
            var before = await CartSnapshot(); using var response = await Json(guest, "Quote", new { lines = new[] { new { variantId = data.Black128, quantity = 2, price = 1, total = 1, userId = otherCustomer } }, customerId = otherCustomer });
            var body = await response.Content.ReadAsStringAsync(); var cart = await response.Content.ReadFromJsonAsync<CartView>();
            Check.True(response.StatusCode == HttpStatusCode.OK && cart!.Total == 24000000m && cart.Lines.Single().Available == 6 && before == await CartSnapshot(), "Quote trusted client money/identity or persisted a guest cart.");
            foreach (var field in new[] { "Cost", "OnHand", "Reserved", "ActorId", "OperationKey", "PasswordHash" }) Check.True(!body.Contains(field, StringComparison.OrdinalIgnoreCase), "Quote leaked forbidden field.");
            using var missing = await Json(guest, "Quote", new { lines = new[] { new { variantId = long.MaxValue, quantity = 1 } } });
            Check.True((await missing.Content.ReadFromJsonAsync<CartView>())!.Lines.Single().Issue == "NOT_FOUND", "Missing quote returned purchasable line.");
            foreach (var invalid in new object[] { new { lines = new[] { new { variantId = data.Black128, quantity = 0 } } }, new { lines = new[] { new { variantId = data.Black128, quantity = -1 } } }, new { lines = new[] { new { variantId = data.Black128, quantity = 1.5 } } }, new { lines = new object?[] { null } }, new { lines = (object?)null } })
            { using var rejected = await Json(guest, "Quote", invalid); Check.True(rejected.StatusCode == HttpStatusCode.BadRequest, "Malformed guest JSON was not400."); }
            Check.True(before == await CartSnapshot() && protectedBefore == await ProtectedSnapshot(), "Quote failure wrote SQL.");
        });
        foreach (var (client, username) in new[] { (customer, "catalog-qa-customer"), (other, "cart-other-qa") })
        { using var login = await Post(client, "/Account/Login", new() { ["Username"] = username, ["Password"] = MvcCatalogFixture.Password, ["ReturnUrl"] = "/Cart/Index" }); Redirect(login); }
        await Check.Run("WEB.BASE.02 cart form and JSON writes reject missing and wrong antiforgery without SQL writes", async () =>
        {
            var before = await CartSnapshot();
            foreach (var action in new[] { "Add", "Update", "Remove", "Quote", "Merge" })
            foreach (var invalid in new[] { "", "wrong-token" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/Cart/" + action) { Content = action is "Quote" or "Merge" ? JsonContent.Create(new { lines = new[] { new { variantId = data.Black128, quantity = 1 } } }) : new FormUrlEncodedContent(Add(data.MainId, data.Black128, "1")) };
                if (invalid.Length > 0) request.Headers.Add("X-CSRF-TOKEN", invalid);
                using var response = await customer.SendAsync(request); Check.True(response.StatusCode == HttpStatusCode.BadRequest, "Cart POST bypassed CSRF: " + action);
            }
            using var anonymousMerge = await Json(guest, "Merge", new { lines = Array.Empty<CartInput>() });
            Check.True(anonymousMerge.StatusCode == HttpStatusCode.Redirect && anonymousMerge.Headers.Location!.OriginalString.StartsWith("/Account/Login"), "Anonymous merge was not challenged.");
            Check.True(before == await CartSnapshot(), "CSRF rejection mutated cart SQL.");
        });
        await Check.Run("WEB.K09.02 real Cart/Add rejects missing inactive and cross-product selection before cart SQL", async () =>
        {
            var before = await CartSnapshot();
            foreach (var product in new[] { long.MaxValue, data.HiddenProductId, data.OtherId })
            {
                var token = await Token(customer); var cartCommands = factory.CartProbe.CartCommands; factory.CartProbe.Enabled = true;
                using var response = await customer.PostAsync("/Cart/Add", new FormUrlEncodedContent(new Dictionary<string,string>(Add(product, data.Black128, "1")) { ["__RequestVerificationToken"] = token }));
                factory.CartProbe.Enabled = false;
                Check.True(response.StatusCode == HttpStatusCode.NotFound && factory.CartProbe.CartCommands == cartCommands, "Association rejection touched cart or was not404.");
            }
            Check.True(before == await CartSnapshot(), "Rejected selection changed SQL.");
        });
        await Check.Run("WEB.K10.01 WEB.K10.02 real Add accumulates own lines ignores client price identity and preserves inventory", async () =>
        {
            for (var i=0;i<2;i++) { var fields=Add(data.MainId,data.Black128,"2"); fields["price"]="1"; fields["total"]="1"; fields["customerId"]=otherCustomer.ToString(); using var response=await Post(customer,"/Cart/Add",fields); Redirect(response); }
            Check.True(await Quantity(data.Customer.AccountId,data.Black128)==4 && await Quantity(otherCustomer,data.Black128)==0, "Add did not accumulate current customer only.");
            using var page=await customer.GetAsync("/Cart/Index"); var html=WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());
            Check.True(html.Contains("48.000.000 đ") && page.Headers.CacheControl?.NoStore==true && protectedBefore==await ProtectedSnapshot(), "Cart price/no-store/inventory mismatch.");
        });
        await Check.Run("WEB.K09.03 WEB.K09.04 POST rejects sold-out noninteger nonpositive over-limit quantities with no SQL mutation", async () =>
        {
            var before=await CartSnapshot();
            foreach(var quantity in new[]{"0","-1","1.5","abc","2147483648","2147483647","7"})
            { using var response=await Post(customer,"/Cart/Add",Add(data.MainId,data.Black128,quantity)); var html=await response.Content.ReadAsStringAsync(); Check.True(response.StatusCode==HttpStatusCode.OK && html.Contains("validation-summary-errors"), "Bad quantity escaped form validation: "+quantity); }
            using var sold=await Post(customer,"/Cart/Add",Add(data.MainId,data.Blue256,"1"));
            Check.True(sold.StatusCode==HttpStatusCode.OK && (await sold.Content.ReadAsStringAsync()).Contains("validation-summary-errors") && before==await CartSnapshot(), "Sold-out direct POST wrote cart.");
        });
        await Check.Run("WEB.K11.01 WEB.K11.02 update/remove use own session ignore forged identity and retain attempted form data", async () =>
        {
            using(var response=await Post(other,"/Cart/Add",Add(data.MainId,data.Black128,"1"))) Redirect(response);
            using(var response=await Post(customer,"/Cart/Update",new(){["variantId"]=data.Black128.ToString(),["quantity"]="3",["customerId"]=otherCustomer.ToString(),["price"]="1"})) Redirect(response);
            Check.True(await Quantity(data.Customer.AccountId,data.Black128)==3 && await Quantity(otherCustomer,data.Black128)==1,"Update changed another customer cart.");
            var before=await CartSnapshot(); using(var response=await Post(customer,"/Cart/Update",new(){["variantId"]=data.Black128.ToString(),["quantity"]="99"}))
            { var html=await response.Content.ReadAsStringAsync(); Check.True(response.StatusCode==HttpStatusCode.OK && html.Contains("value=\"99\"") && html.Contains("data-error-variant=\""+data.Black128+"\""),"Failure lost quantity or row association."); }
            Check.True(before==await CartSnapshot(),"Failed update partially persisted.");
            using(var response=await Post(customer,"/Cart/Remove",new(){["variantId"]=data.Black128.ToString(),["customerId"]=otherCustomer.ToString()})) Redirect(response);
            using var empty=await customer.GetAsync("/Cart/Index"); Check.True(WebUtility.HtmlDecode(await empty.Content.ReadAsStringAsync()).Contains("Giỏ hàng đang trống") && await Quantity(otherCustomer,data.Black128)==1,"Last remove or ownership failed.");
        });
        await Check.Run("WEB.K10.04 Merge max duplicate collapse cap drop and repeated request preserve exact SQL", async () =>
        {
            using(var add=await Post(customer,"/Cart/Add",Add(data.MainId,data.Black128,"4"))) Redirect(add);
            long hidden; await using(var db=Open(database.ConnectionString)) hidden=await db.Variants.Where(x=>x.ProductId==data.HiddenProductId).Select(x=>x.Id).SingleAsync();
            var payload=new { lines=new[]{new CartInput(data.Black128,10),new CartInput(data.Black256,2),new CartInput(data.Black256,1),new CartInput(hidden,1),new CartInput(data.Blue256,1)} };
            using(var response=await Json(customer,"Merge",payload))
            { var result=await response.Content.ReadFromJsonAsync<CartMergeResult>(); Check.True(response.IsSuccessStatusCode && result!.Cart.Lines.Count==2 && result.Cart.Total==120000000m && result.Adjusted.Count==3 && result.Adjusted.Any(x=>x.VariantId==data.Black128 && x.Quantity==6) && result.Adjusted.Count(x=>x.Quantity==0)==2,"DEC02 merge incorrect."); }
            var before=await CartSnapshot(); for(var i=0;i<2;i++){using var response=await Json(customer,"Merge",payload);Check.True(response.IsSuccessStatusCode,"Merge replay failed.");}
            Check.True(before==await CartSnapshot() && await Quantity(otherCustomer,data.Black128)==1 && protectedBefore==await ProtectedSnapshot(),"Merge replay doubled, crossed owner or reserved stock.");
        });
        await Check.Run("WEB.K11.03 cart read surfaces changed server price and inactive/short-stock issues on exact lines", async () =>
        {
            await using(var db=Open(database.ConnectionString))
            { var variant=await db.Variants.SingleAsync(x=>x.Id==data.Black128);variant.Price=13000000m;var product=await db.Products.SingleAsync(x=>x.Id==data.MainId);product.Active=false;await db.SaveChangesAsync(); }
            using var page=await customer.GetAsync("/Cart/Index");var html=WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());
            Check.True(page.IsSuccessStatusCode && html.Contains("13.000.000 đ") && html.Contains("Sản phẩm không còn bán") && html.Contains("data-variant-id=\""+data.Black128+"\""),"Cart did not surface changed server data on line.");
            foreach(var field in new[]{"OnHand","Reserved","PasswordHash","ActorId","OperationKey","name=\"Cost\""}) Check.True(!html.Contains(field,StringComparison.OrdinalIgnoreCase),"Cart HTML leaks forbidden field.");
            Check.True(protectedBefore==await ProtectedSnapshot(),"Cart phase changed inventory, journal, orders or money SQL.");
        });
    }
}
