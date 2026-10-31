using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class MvcCatalogChecks
{
    private static StoreDbContext Open(string connection) => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
    private static long[] Cards(string html) => Regex.Matches(html, "data-product-id=\"(\\d+)\"").Select(m => long.Parse(m.Groups[1].Value)).ToArray();
    private static void Safe(string html)
    {
        foreach (var value in new[] { "OnHand", "Reserved", "PasswordHash", "ActorId", "OperationKey", "ProfitPercent", "ConnectionStrings", "name=\"Cost\"", MvcCatalogFixture.Password })
            Check.True(!html.Contains(value, StringComparison.OrdinalIgnoreCase), "Public HTML exposes forbidden field/secret.");
    }
    public static async Task Run()
    {
        await using var database = await TestDatabase.Create();
        await using (var db = Open(database.ConnectionString)) await new DatabaseMigrator(db).MigrateAsync();
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "PhoneStore.slnx"))) root = root.Parent;
        if (root is null) throw new InvalidOperationException("Solution root missing.");
        await using var factory = new MvcFoundationFactory(database.ConnectionString, root.FullName);
        using var guest = factory.Browser();
        async Task<(HttpResponseMessage Response, string Html)> Get(string path)
        { var response = await guest.GetAsync(path); var html = await response.Content.ReadAsStringAsync(); Safe(html); return (response, html); }
        await Check.Run("WEB.HOME.01 empty monthly sales hides bestseller section without fake products", async () =>
        {
            var (response, html) = await Get("/Home/Index"); using (response)
                Check.True(response.IsSuccessStatusCode && !html.Contains("id=\"bestsellers\"") && html.Contains("id=\"about\"") && html.Contains("id=\"contact\"") && html.Contains("<footer"), "Empty homepage lacks content or fabricates bestseller rows.");
        });
        MvcCatalogFixture data;
        await using (var scope = factory.Services.CreateAsyncScope()) data = await MvcCatalogFixture.Seed(scope.ServiceProvider);
        async Task<string> Snapshot()
        {
            await using var db = Open(database.ConnectionString);
            // Full snapshots remain in RAM. Never export credential hashes, tokens or sessions.
            return JsonSerializer.Serialize(new {
                Inventory = await db.Inventory.AsNoTracking().OrderBy(v => v.VariantId).ToListAsync(),
                Variants = await db.Variants.AsNoTracking().OrderBy(v => v.Id).ToListAsync(),
                Products = await db.Products.AsNoTracking().OrderBy(v => v.Id).ToListAsync(),
                Images = await db.ProductImages.AsNoTracking().OrderBy(v => v.Id).ToListAsync(),
                Accounts = await db.Accounts.AsNoTracking().OrderBy(v => v.Id).ToListAsync(),
                Profiles = await db.Customers.AsNoTracking().OrderBy(v => v.Id).ToListAsync(),
                Sessions = await db.Sessions.AsNoTracking().OrderBy(v => v.Id).ToListAsync(),
                Cart = await db.Cart.AsNoTracking().OrderBy(v => v.CustomerId).ThenBy(v => v.VariantId).ToListAsync(),
                Orders = await db.Orders.AsNoTracking().OrderBy(v => v.Id).ToListAsync(),
                Lines = await db.OrderLines.AsNoTracking().OrderBy(v => v.OrderId).ThenBy(v => v.VariantId).ToListAsync(),
                Payments = await db.Payments.AsNoTracking().OrderBy(v => v.OrderId).ToListAsync(),
                History = await db.OrderHistory.AsNoTracking().OrderBy(v => v.Id).ToListAsync(),
                Movements = await db.Movements.AsNoTracking().OrderBy(v => v.Id).ToListAsync()
            });
        }
        await Check.Run("WEB.K06.01 public catalog DEC03 visibility exact totals and disjoint pages preserve SQL", async () =>
        {
            var before = await Snapshot(); var ids = new List<long>();
            foreach (var page in new[] { 1, 2 })
            {
                var (response, html) = await Get("/Catalog/Index?pageSize=2&page=" + page); using (response)
                { Check.True(response.StatusCode == HttpStatusCode.OK && html.Contains("data-result-total=\"4\"") && Cards(html).Length == 2,
                    $"Catalog page/total incorrect: status={(int)response.StatusCode}, total={Regex.Match(html, "data-result-total=\\\"([^\\\"]*)\\\"").Groups[1].Value}, cards={Cards(html).Length}."); ids.AddRange(Cards(html)); }
            }
            Check.True(ids.Distinct().Count() == 4 && new[] { data.MainId, data.OtherId, data.SplitId, data.OutOfStockId }.All(ids.Contains) && before == await Snapshot(), "Visibility, pagination or read-only SQL invariant failed.");
        });
        await Check.Run("WEB.K06.02 WEB.K07.02 empty search and whitespace/no-match return correct structure and recovery", async () =>
        {
            foreach (var query in new[] { "", "   " })
            { var (response, html) = await Get("/Catalog/Index?q=" + Uri.EscapeDataString(query)); using (response) Check.True(response.IsSuccessStatusCode && Cards(html).Length == 4, "Whitespace filtered catalog."); }
            var before = await Snapshot(); var (missing, body) = await Get("/Catalog/Index?q=qa-no-such-device"); using (missing)
                Check.True(missing.IsSuccessStatusCode && Cards(body).Length == 0 && body.Contains("data-result-total=\"0\"") && WebUtility.HtmlDecode(body).Contains("Thử từ khóa khác") && before == await Snapshot(), "No-match is not a friendly read-only empty state.");
        });
        await Check.Run("WEB.K06.03 invalid pagination is consistently400 HTML without partial results or500", async () =>
        {
            var before = await Snapshot();
            foreach (var query in new[] { "page=0", "page=-1", "page=abc", "page=2147483648", "page=2147483647&pageSize=100", "pageSize=0", "pageSize=-1", "pageSize=101", "pageSize=abc" })
            { var (response, html) = await Get("/Catalog/Index?" + query); using (response) Check.True(response.StatusCode == HttpStatusCode.BadRequest && Cards(html).Length == 0, "Pagination did not follow400 policy: " + query); }
            var (beyond, empty) = await Get("/Catalog/Index?page=999"); using (beyond) Check.True(beyond.IsSuccessStatusCode && Cards(empty).Length == 0 && empty.Contains("data-result-total=\"4\""), "Valid page beyond last is not empty with exact total.");
            Check.True(before == await Snapshot(), "Rejected pagination mutated SQL.");
        });
        await Check.Run("WEB.K07.01 special SQL-like and HTML query strings remain encoded and literal without SQL writes", async () =>
        {
            var before = await Snapshot();
            foreach (var query in new[] { "<script>catalog-search-probe</script>", "' OR 1=1 --", "%_[]", "Aurora & <b>test</b>" })
            {
                var (response, html) = await Get("/Catalog/Index?q=" + Uri.EscapeDataString(query)); using (response)
                    Check.True(response.IsSuccessStatusCode && Cards(html).Length == 0 && WebUtility.HtmlDecode(html).Contains(query) && !html.Contains("<script>catalog-search-probe</script>"), "Search became SQL/executable HTML or lost term.");
            }
            Check.True(before == await Snapshot(), "Hostile-looking search mutated SQL.");
        });
        var combined = $"q=Aurora&brandId={data.BrandId}&minPrice=20000000&maxPrice=25000000&storageGb=256&color=" + Uri.EscapeDataString("Đen") + "&sort=price-desc&pageSize=1";
        await Check.Run("WEB.K08.01 combined same-variant filtering retains query on page links and displays filtered server price", async () =>
        {
            var before = await Snapshot(); var (response, html) = await Get("/Catalog/Index?" + combined); using (response)
            {
                Check.True(response.IsSuccessStatusCode && Cards(html).SequenceEqual(new[] { data.MainId }) && html.Contains("data-result-total=\"2\"") && WebUtility.HtmlDecode(html).Contains("24.000.000 đ") && !WebUtility.HtmlDecode(html).Contains("12.000.000 đ"), "Filtered/sorted card price or same-variant match wrong.");
                var next = Regex.Match(html, "href=\"([^\"]+)\" rel=\"next\""); Check.True(next.Success, "Next-page link missing.");
                var nextUrl = WebUtility.HtmlDecode(next.Groups[1].Value); var decoded = Uri.UnescapeDataString(nextUrl);
                foreach (var value in new[] { "q=Aurora", "brandId=" + data.BrandId, "minPrice=20000000", "maxPrice=25000000", "storageGb=256", "color=Đen", "sort=price-desc", "pageSize=1", "page=2" }) Check.True(decoded.Contains(value), "Paging lost filter: " + value);
                var (second, body) = await Get(nextUrl); using (second) Check.True(second.IsSuccessStatusCode && Cards(body).SequenceEqual(new[] { data.OtherId }) && WebUtility.HtmlDecode(body).Contains("22.000.000 đ"), "Second page changed filter/order.");
            }
            Check.True(before == await Snapshot(), "Filtering changed SQL.");
        });
        await Check.Run("WEB.K08.02 removing individual filters and all filters preserves remaining query and totals", async () =>
        {
            var (response, html) = await Get("/Catalog/Index?" + combined); using (response)
            {
                var links = Regex.Matches(html, "href=\"([^\"]+)\"").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).Where(url => url.StartsWith("/Catalog/Index?", StringComparison.Ordinal)).ToList();
                var colorRemoved = links.First(url => !url.Contains("color=") && url.Contains("storageGb=256") && url.Contains("minPrice=20000000"));
                var (changed, body) = await Get(colorRemoved); using (changed) Check.True(changed.IsSuccessStatusCode && body.Contains("data-result-total=\"3\""), "Removing only color lost other filters or total.");
                var allRemoved = links.First(url => url.Contains("q=Aurora") && !url.Contains("brandId=") && !url.Contains("minPrice=") && !url.Contains("storageGb=") && !url.Contains("color="));
                var (reset, result) = await Get(allRemoved); using (reset) Check.True(reset.IsSuccessStatusCode && result.Contains("data-result-total=\"3\""), "Reset did not preserve remaining search.");
            }
        });
        await Check.Run("WEB.K08.03 inverted prices unknown sort and malformed filter values return soft HTML errors", async () =>
        {
            var before = await Snapshot();
            foreach (var query in new[] { "minPrice=20&maxPrice=10", "sort=DROP%20TABLE", "minPrice=bad", "minPrice=-1", "minPrice=1.123", "storageGb=0", "brandId=-1" })
            { var (response, html) = await Get("/Catalog/Index?" + query); using (response) Check.True(response.StatusCode == HttpStatusCode.OK && html.Contains("validation-summary-errors") && Cards(html).Length == 0, "Filter invalidity became500/silent empty data: " + query); }
            Check.True(before == await Snapshot(), "Invalid filter changed SQL.");
        });
        await Check.Run("WEB.K09.01 details and no-JS variant GET show exact server prices availability without physical stock", async () =>
        {
            var before = await Snapshot();
            foreach (var pair in new[] { (data.Black128, "12.000.000 đ", "max=\"6\""), (data.Black256, "24.000.000 đ", "max=\"7\""), (data.Blue256, "18.000.000 đ", "max=\"0\"") })
            { var (response, html) = await Get($"/Catalog/Details/{data.MainId}?variantId={pair.Item1}"); using (response) Check.True(response.IsSuccessStatusCode && WebUtility.HtmlDecode(html).Contains(pair.Item2) && html.Contains(pair.Item3), "Selected variant price/availability differs from SQL-backed DTO."); }
            await using var db = Open(database.ConnectionString); var blue = await db.Inventory.AsNoTracking().SingleAsync(v => v.VariantId == data.Blue256);
            Check.True(blue.OnHand == 8 && blue.Reserved == 8 && before == await Snapshot(), "Fully reserved fixture or read invariant failed.");
        });
        await Check.Run("WEB.K09.02 PARTIAL GET404 and adapter selection reject mismatch before cart SQL; POST deferred", async () =>
        {
            var before = await Snapshot(); var cartCount = factory.CartProbe.CartCommands; factory.CartProbe.Enabled = true;
            try
            {
                foreach (var id in new[] { long.MaxValue, data.HiddenProductId, data.HiddenBrandId, data.HiddenCategoryId, data.HiddenVariantsId })
                { var (response, _) = await Get("/Catalog/Details/" + id); using (response) Check.True(response.StatusCode == HttpStatusCode.NotFound, "Unavailable product detail is not404."); }
                var (mismatch, _) = await Get($"/Catalog/Details/{data.MainId}?variantId={data.OtherVariant}"); using (mismatch) Check.True(mismatch.StatusCode == HttpStatusCode.NotFound, "Foreign variant selection not404.");
                await using var scope = factory.Services.CreateAsyncScope(); var store = scope.ServiceProvider.GetRequiredService<IStoreService>();
                await CommerceContext.Reject("NOT_FOUND", async () => await store.AddToCartAsync(data.Customer, data.MainId, data.OtherVariant, 1));
                Check.True(cartCount == factory.CartProbe.CartCommands && before == await Snapshot(), "Mismatch reached cart SQL or mutated DB.");
            }
            finally { factory.CartProbe.Enabled = false; }
        });
        await Check.Run("WEB.K09.03 WEB.K09.04 PARTIAL form schema unavailable text integer availability bounds; no Cart Add action", async () =>
        {
            var (response, html) = await Get($"/Catalog/Details/{data.MainId}?variantId={data.Blue256}"); using (response)
            {
                var form = Regex.Match(html, "<form[^>]*action=\"/Cart/Add\"[\\s\\S]*?</form>").Value;
                Check.True(form.Contains("method=\"post\"") && form.Contains("name=\"productId\"") && form.Contains("name=\"variantId\"") && form.Contains("name=\"quantity\"") &&
                    Regex.Matches(form, "name=\"__RequestVerificationToken\"").Count == 1 && form.Contains("step=\"1\"") && form.Contains("min=\"1\"") && form.Contains("max=\"0\"") &&
                    Regex.IsMatch(form, "<button[^>]*disabled[^>]*id=\"add-to-cart\"") && WebUtility.HtmlDecode(form).Contains("Hết hàng"), "Add-form fields/token/stock UI incorrect.");
            }
            var (available, body) = await Get($"/Catalog/Details/{data.MainId}?variantId={data.Black128}"); using (available) Check.True(body.Contains("max=\"6\"") && body.Contains("step=\"1\""), "Positive quantity bounds missing.");
            // No POST is made here: the real Cart/Add action and server assertions belong to step4.
        });
        await Check.Run("WEB.HOME.01 real homepage monthly rankings and public prices match bestseller service with unchanged SQL", async () =>
        {
            var before = await Snapshot(); var (response, html) = await Get("/Home/Index"); using (response)
            {
                Check.True(response.IsSuccessStatusCode && html.Contains("id=\"bestsellers\"") && Cards(html).SequenceEqual(new[] { data.OtherId, data.MainId }) && html.Contains("id=\"about\"") && html.Contains("id=\"contact\"") && html.Contains("<footer"), "Home ranking/sections wrong.");
                Check.True(WebUtility.HtmlDecode(html).Contains("22.000.000 đ") && WebUtility.HtmlDecode(html).Contains("12.000.000 đ") && before == await Snapshot(), "Bestseller prices/read-only invariant failed.");
            }
        });
        await Check.Run("WEB.K07.01 catalog text and attributes encode script-like product metadata", async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope(); var products = scope.ServiceProvider.GetRequiredService<ProductService>(); var current = await products.Read(data.Admin, data.OutOfStockId);
            var original = ProductChecks.Input(current); var name = "QA <script>catalog-name</script>";
            await products.Save(data.Admin, current.Id, original with { Name = name, Description = "<script>catalog-description</script>", Specifications = "<img src=x onerror=alert(1)>" });
            try
            {
                var before = await Snapshot();
                foreach (var route in new[] { "/Catalog/Index", "/Catalog/Details/" + current.Id })
                { var (response, html) = await Get(route); using (response) Check.True(response.IsSuccessStatusCode && WebUtility.HtmlDecode(html).Contains(name) && !html.Contains("<script>catalog-name</script>") && !html.Contains("<img src=x onerror=alert(1)>"), "Metadata was executable HTML."); }
                Check.True(before == await Snapshot(), "Encoded reads changed SQL.");
            }
            finally { var latest = await products.Read(data.Admin, current.Id); await products.Save(data.Admin, current.Id, original with { Variants = ProductChecks.Input(latest).Variants }); }
        });
        await Check.Run("WEB.K06.02 query DB failure is a real HTML500 trace response rather than empty catalog", async () =>
        {
            await using var db = Open(database.ConnectionString); var before = await Snapshot();
            await db.Database.ExecuteSqlRawAsync("EXEC sp_rename N'dbo.BIEN_THE', N'BIEN_THE_QA_FAULT';");
            try { var (response, html) = await Get("/Catalog/Index"); using (response) Check.True(response.StatusCode == HttpStatusCode.InternalServerError && WebUtility.HtmlDecode(html).Contains("Mã tra cứu") && !html.Contains("SqlException") && !WebUtility.HtmlDecode(html).Contains("Chưa tìm thấy điện thoại phù hợp"), "DB failure was hidden as an empty result."); }
            finally { await db.Database.ExecuteSqlRawAsync("EXEC sp_rename N'dbo.BIEN_THE_QA_FAULT', N'BIEN_THE';"); }
            Check.True(before == await Snapshot(), "Read-failure injection changed business rows.");
        });
    }
}
