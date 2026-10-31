using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Web.Storefront;

namespace PhoneStore.Checks;

internal static class MvcFoundationChecks
{
    private static async Task<string> Token(HttpClient client, string path = "/__foundation/token")
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        using var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return data.RootElement.GetProperty("token").GetString()!;
    }
    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, HttpContent content, bool token = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        if (token) request.Headers.Add("X-CSRF-TOKEN", await Token(client));
        return await client.SendAsync(request);
    }
    private static async Task<HttpResponseMessage> Login(HttpClient client, string username, string surface = "customer")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            { Content = JsonContent.Create(new LoginRequest(username, CommerceContext.Password, surface)) };
        request.Headers.Add("X-CSRF-TOKEN", await Token(client, "/api/security/token"));
        return await client.SendAsync(request);
    }
    public static async Task Run()
    {
        await using var database = await TestDatabase.Create();
        await using (var db = Open(database.ConnectionString)) await new DatabaseMigrator(db).MigrateAsync();
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PhoneStore.slnx"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Solution root missing.");
        await using var factory = new MvcFoundationFactory(database.ConnectionString, directory.FullName);
        using var guest = factory.Browser();
        await Check.Run("MVC foundation real Razor HTML encodes configured text and serves responsive assets", async () =>
        {
            using var response = await guest.GetAsync("/Home/Index"); var html = await response.Content.ReadAsStringAsync();
            Check.True(response.StatusCode == HttpStatusCode.OK && response.Content.Headers.ContentType?.MediaType == "text/html" &&
                html.Contains("lang=\"vi\"") && html.Contains("&lt;script&gt;private-html-probe&lt;/script&gt;") && !html.Contains("<script>private-html-probe"), "Razor rendering/encoding failed.");
            foreach (var path in new[] { "/css/storefront.css", "/js/storefront.js" })
            { using var asset = await guest.GetAsync(path); Check.True(asset.StatusCode == HttpStatusCode.OK, "Website asset missing."); }
            Check.True(WebsiteFormatting.Money(23980000m) == "23.980.000 đ" && WebsiteFormatting.Money(1.25m) == "1,25 đ" &&
                WebsiteFormatting.Time(new DateTime(2026, 10, 31, 18, 30, 0, DateTimeKind.Utc)) == "01/11/2026 01:30", "Money/time formatting lost precision or timezone.");
        });
        await Check.Run("MVC foundation HTML errors do not change API JSON errors or expose exception details", async () =>
        {
            using var missing = await guest.GetAsync("/missing-foundation-page");
            Check.True(missing.StatusCode == HttpStatusCode.NotFound && missing.Content.Headers.ContentType?.MediaType == "text/html", "Website404 is not HTML.");
            using var failure = await guest.GetAsync("/__foundation/error"); var html = await failure.Content.ReadAsStringAsync();
            Check.True(failure.StatusCode == HttpStatusCode.InternalServerError && html.Contains("Mã tra cứu") && !html.Contains("private-failure-details") && !html.Contains("System.InvalidOperationException"), "Website500 leaked internal details.");
            using var api = await guest.GetAsync("/api/admin/orders");
            Check.True(api.StatusCode == HttpStatusCode.Unauthorized && api.Content.Headers.ContentType?.MediaType == "application/json", "API401 changed to a redirect or HTML.");
            using var apiMissing = await guest.GetAsync("/api/foundation-missing");
            Check.True(apiMissing.StatusCode == HttpStatusCode.NotFound && apiMissing.Content.Headers.ContentType?.MediaType == "application/json", "API404 changed.");
        });
        await Check.Run("MVC foundation protected redirect is local and personal responses are no-store", async () =>
        {
            using var response = await guest.GetAsync("/Cart/__foundation?returnUrl=https://evil.example");
            Check.True(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.OriginalString.StartsWith("/Account/Login?returnUrl=%2FCart%2F__foundation", StringComparison.Ordinal) &&
                response.Headers.CacheControl?.NoStore == true, "Website challenge is not a local no-store redirect.");
        });
        await Check.Run("MVC foundation Production500 is friendly HTML and never a developer page", async () =>
        {
            await using var production = new MvcFoundationFactory(database.ConnectionString, directory.FullName, "Production");
            using var browser = production.Browser();
            using var response = await browser.GetAsync("/__foundation/error"); var html = await response.Content.ReadAsStringAsync();
            Check.True(response.StatusCode == HttpStatusCode.InternalServerError && html.Contains("Mã tra cứu") &&
                !html.Contains("private-failure-details") && !html.Contains("StackTrace") && !html.Contains("MvcFoundationProbeController"), "Production error exposes implementation.");
        });
        await Check.Run("MVC foundation global antiforgery rejects form and JSON POST without side effects", async () =>
        {
            using var noToken = await Post(guest, "/__foundation/json", JsonContent.Create(new { value = 1 }), false);
            Check.True(noToken.StatusCode == HttpStatusCode.BadRequest, "Missing JSON token accepted.");
            using var wrong = new HttpRequestMessage(HttpMethod.Post, "/__foundation/json") { Content = JsonContent.Create(new { value = 1 }) };
            wrong.Headers.Add("X-CSRF-TOKEN", "invalid"); using var rejected = await guest.SendAsync(wrong);
            Check.True(rejected.StatusCode == HttpStatusCode.BadRequest, "Invalid JSON token accepted.");
            using var accepted = await Post(guest, "/__foundation/json", JsonContent.Create(new { value = 1 }));
            Check.True(accepted.StatusCode == HttpStatusCode.OK, "Valid header token rejected.");
        });
        var username = "mvc-foundation-customer";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAccountRegistration>().RegisterAsync(new(username, CommerceContext.Password, "Khách MVC", "0919234567", "mvc@example.test"));
            var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            var admin = new Account { Username = "mvc-foundation-admin", Role = "ADMIN" };
            admin.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>().HashPassword(admin, CommerceContext.Password);
            db.Accounts.Add(admin); await db.SaveChangesAsync();
        }
        using var customer = factory.Browser();
        await Check.Run("MVC approved duplicate summary retains model values and registration unique failures roll back all rows", async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var registrations = scope.ServiceProvider.GetRequiredService<IAccountRegistration>();
            var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            async Task<string> Snapshot() => JsonSerializer.Serialize(new {
                Accounts = await db.Accounts.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Profiles = await db.Customers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Sessions = await db.Sessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
            });
            var before = await Snapshot();
            foreach (var input in new[] {
                new RegistrationRequest(username, CommerceContext.Password, "Retain username", "0919234568", "unique-1@example.test"),
                new RegistrationRequest("mvc-duplicate-phone", CommerceContext.Password, "Retain phone", "0919234567", "unique-2@example.test"),
                new RegistrationRequest("mvc-duplicate-email", CommerceContext.Password, "Retain email", "0919234569", "mvc@example.test") })
            {
                var model = new ModelStateDictionary();
                model.SetModelValue("Username", input.Username, input.Username);
                model.SetModelValue("FullName", input.FullName, input.FullName);
                model.SetModelValue("Phone", input.Phone, input.Phone);
                model.SetModelValue("Email", input.Email, input.Email);
                try { await registrations.RegisterAsync(input); throw new InvalidOperationException("Duplicate was accepted."); }
                catch (BusinessException error) when (error.Code == "INVALID_INPUT") { FormErrors.AddSummary(model, error); }
                Check.True(model[string.Empty]!.Errors.Count == 1 && model.Where(x => x.Key != string.Empty).All(x => x.Value!.Errors.Count == 0) &&
                    model["FullName"]!.AttemptedValue == input.FullName && model["Phone"]!.AttemptedValue == input.Phone && before == await Snapshot(),
                    "Duplicate guessed a field, lost input or partially registered.");
            }
        });
        await Check.Run("MVC foundation shared cookie validates SQL session and retains fixed eight-hour security", async () =>
        {
            using var login = await Login(customer, username); login.EnsureSuccessStatusCode();
            var cookie = login.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("PhoneStore.Session=", StringComparison.Ordinal));
            Check.True(cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase) && cookie.Contains("secure", StringComparison.OrdinalIgnoreCase), "Session cookie flags missing.");
            using var authorized = await customer.GetAsync("/Cart/__foundation");
            Check.True(authorized.StatusCode == HttpStatusCode.OK && authorized.Headers.CacheControl?.NoStore == true && !authorized.Headers.Contains("Set-Cookie"), "Session renewed or protected access failed.");
            var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
            Check.True(!options.SlidingExpiration && options.ExpireTimeSpan == TimeSpan.FromHours(8), "Cookie lifetime changed.");
            await using var db = Open(database.ConnectionString);
            var session = await db.Sessions.AsNoTracking().SingleAsync();
            Check.True((session.ExpiresAt - DateTime.UtcNow).TotalHours is > 7.9 and <= 8, "Persisted session is not fixed eight hours.");
        });
        await Check.Run("MVC foundation admin cookie cannot enter customer website but keeps API authorization", async () =>
        {
            using var admin = factory.Browser(); using var login = await Login(admin, "mvc-foundation-admin", "admin"); login.EnsureSuccessStatusCode();
            using var denied = await admin.GetAsync("/Cart/__foundation");
            Check.True(denied.StatusCode == HttpStatusCode.Forbidden && denied.Content.Headers.ContentType?.MediaType == "text/html", "ADMIN entered customer surface.");
            using var allowed = await admin.GetAsync("/api/admin/session"); Check.True(allowed.StatusCode == HttpStatusCode.OK, "ADMIN API session changed.");
            using var rejected = await customer.GetAsync("/api/admin/session"); Check.True(rejected.StatusCode == HttpStatusCode.Forbidden && rejected.Content.Headers.ContentType?.MediaType == "application/json", "CUSTOMER API403 changed.");
        });
        await Check.Run("MVC foundation service codes remain non500 and duplicate summary never guesses a field", async () =>
        {
            foreach (var code in new[] { "INVALID_INPUT", "VALIDATION_FAILED", "DUPLICATE_VALUE", "FORBIDDEN", "NOT_FOUND", "CONCURRENT_UPDATE", "STALE_VERSION", "INVALID_STATE", "OUT_OF_STOCK", "PRICE_CHANGED", "REQUEST_KEY_CONFLICT", "CART_EMPTY", "PAYMENT_METHOD_UNSUPPORTED", "VARIANT_INACTIVE", "ALREADY_APPLIED" })
            {
                using var response = await guest.GetAsync("/__foundation/business/" + code);
                Check.True((int)response.StatusCode is >= 400 and < 500, "Service code became500: " + code);
            }
            var state = new ModelStateDictionary(); state.SetModelValue("Phone", "retained", "retained");
            FormErrors.AddSummary(state, new("INVALID_INPUT", "Tên đăng nhập, số điện thoại hoặc email đã tồn tại."));
            Check.True(state[string.Empty]!.Errors.Count == 1 && state["Phone"]!.Errors.Count == 0 && state["Phone"]!.AttemptedValue == "retained", "Summary guessed a field or lost form values.");
        });
        await VerifySelection(factory, customer, database.ConnectionString);
        await Check.Run("MVC foundation revoked and expired SQL sessions redirect with no renewal or new business data", async () =>
        {
            await using var db = Open(database.ConnectionString);
            var id = await db.Accounts.Where(x => x.Username == username).Select(x => x.Id).SingleAsync();
            await db.Sessions.Where(x => x.AccountId == id).ExecuteUpdateAsync(set => set.SetProperty(x => x.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
            using var expired = await customer.GetAsync("/Cart/__foundation");
            Check.True(expired.StatusCode == HttpStatusCode.Redirect, "Expired session accepted or500.");
            using var again = await Login(customer, username); again.EnsureSuccessStatusCode();
            await db.Sessions.Where(x => x.AccountId == id).ExecuteDeleteAsync();
            using var revoked = await customer.GetAsync("/Cart/__foundation");
            Check.True(revoked.StatusCode == HttpStatusCode.Redirect, "Revoked session accepted or500.");
        });
    }
    private static StoreDbContext Open(string connection) => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);

    private static async Task VerifySelection(MvcFoundationFactory factory, HttpClient customer, string connection)
    {
        await Check.Run("MVC approved product-variant adapter rejects mismatch404 before any cart SQL and preserves H R prices histories", async () =>
        {
            // Build normal stock fixtures through the existing services, not a mock database.
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            var session = await db.Sessions.Join(db.Accounts.Where(x => x.Role == "ADMIN"), s => s.AccountId, a => a.Id,
                (s, a) => new { AccountId = a.Id, SessionId = s.Id }).SingleAsync();
            var admin = new Actor(session.AccountId, session.SessionId);
            var lookups = scope.ServiceProvider.GetRequiredService<LookupService>();
            var brand = await lookups.Save(admin, "brands", 0, new("MVC fixture brand"));
            var category = await lookups.Save(admin, "categories", 0, new("MVC fixture category"));
            var products = scope.ServiceProvider.GetRequiredService<ProductService>();
            var first = await products.Save(admin, 0, new("MVC-P1", "MVC phone A", brand, category, "Specs", "Description", "", true,
                [new(0, "MVC-SKU-1", "Black", 128, 100m, 20)]));
            var second = await products.Save(admin, 0, new("MVC-P2", "MVC phone B", brand, category, "Specs", "Description", "", true,
                [new(0, "MVC-SKU-2", "White", 256, 200m, 20)]));
            var own = await products.Read(admin, first); var foreign = await products.Read(admin, second);
            var supplier = await scope.ServiceProvider.GetRequiredService<SupplierService>().Save(admin, 0,
                new("MVC-SUP", "MVC supplier", "0901234599", "QA address"));
            var purchases = scope.ServiceProvider.GetRequiredService<PurchaseService>();
            var receipt = await purchases.Save(admin, 0, new(supplier,
                [new(own.Variants[0].Id, 3, 100m), new(foreign.Variants[0].Id, 3, 200m)], RequestKey: Guid.NewGuid()));
            var draft = await purchases.Read(admin, receipt);
            await purchases.Confirm(admin, receipt, new(draft.Version, Guid.NewGuid()));
            async Task<string> Snapshot()
            {
                await using var sql = Open(connection);
                return JsonSerializer.Serialize(new {
                    Stock = await sql.Inventory.AsNoTracking().OrderBy(x => x.VariantId).ToListAsync(),
                    Variants = await sql.Variants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                    Cart = await sql.Cart.AsNoTracking().OrderBy(x => x.CustomerId).ThenBy(x => x.VariantId).ToListAsync(),
                    Orders = await sql.Orders.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                    Lines = await sql.OrderLines.AsNoTracking().OrderBy(x => x.OrderId).ThenBy(x => x.VariantId).ToListAsync(),
                    Payments = await sql.Payments.AsNoTracking().OrderBy(x => x.OrderId).ToListAsync(),
                    History = await sql.OrderHistory.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                    Movements = await sql.Movements.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
                });
            }
            var before = await Snapshot();
            foreach (var pair in new[] { (first, foreign.Variants[0].Id), (long.MaxValue, own.Variants[0].Id) })
            {
                factory.CartProbe.Enabled = true;
                using var response = await Post(customer, "/__foundation/selection", new FormUrlEncodedContent(new Dictionary<string,string>
                    { ["productId"] = pair.Item1.ToString(), ["variantId"] = pair.Item2.ToString(), ["quantity"] = "1" }));
                factory.CartProbe.Enabled = false;
                Check.True(response.StatusCode == HttpStatusCode.NotFound && factory.CartProbe.CartCommands == 0 && before == await Snapshot(),
                    "Invalid association called cart SQL or changed inventory/prices/money/history.");
            }
            using (var noToken = await Post(customer, "/__foundation/selection", new FormUrlEncodedContent(new Dictionary<string,string>
                { ["productId"] = first.ToString(), ["variantId"] = own.Variants[0].Id.ToString(), ["quantity"] = "1" }), false))
                Check.True(noToken.StatusCode == HttpStatusCode.BadRequest && before == await Snapshot(), "Missing form CSRF changed DB.");
            factory.CartProbe.Enabled = true;
            using var success = await Post(customer, "/__foundation/selection", new FormUrlEncodedContent(new Dictionary<string,string>
                { ["productId"] = first.ToString(), ["variantId"] = own.Variants[0].Id.ToString(), ["quantity"] = "1" }));
            factory.CartProbe.Enabled = false;
            Check.True(success.StatusCode == HttpStatusCode.NoContent && factory.CartProbe.CartCommands > 0, "Valid association did not call the real cart service.");
            await using var after = Open(connection);
            Check.True(await after.Cart.CountAsync() == 1 && await after.Cart.Select(x => x.Quantity).SingleAsync() == 1 &&
                await after.Inventory.AllAsync(x => x.OnHand == 3 && x.Reserved == 0) && !await after.Orders.AnyAsync() &&
                await after.Movements.CountAsync() == 2, "Valid add changed stock, orders or journal.");
            var current = await products.Read(admin, first);
            await products.Save(admin, first, ProductChecks.Input(current) with { Active = false });
            var discontinuedBefore = await Snapshot();
            factory.CartProbe.Enabled = true;
            var count = factory.CartProbe.CartCommands;
            using var discontinued = await Post(customer, "/__foundation/selection", new FormUrlEncodedContent(new Dictionary<string,string>
                { ["productId"] = first.ToString(), ["variantId"] = own.Variants[0].Id.ToString(), ["quantity"] = "1" }));
            factory.CartProbe.Enabled = false;
            Check.True(discontinued.StatusCode == HttpStatusCode.NotFound && factory.CartProbe.CartCommands == count && discontinuedBefore == await Snapshot(),
                "Discontinued product reached cart or changed DB.");
        });
    }
}
