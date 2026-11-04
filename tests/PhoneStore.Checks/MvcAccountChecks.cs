using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class MvcAccountChecks
{
    private const string Password = "MvcAccountsQa@12345";
    private const string Replacement = "MvcAccountsQa@98765";
    private static StoreDbContext Open(string connection) => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
    private static Dictionary<string, string> Registration(string username = "web-new", string phone = "0938123403", string email = "new-web@example.test") =>
        new() { ["Username"] = username, ["Password"] = Password, ["FullName"] = "Khách giữ dữ liệu", ["Phone"] = phone, ["Email"] = email, ["Address"] = "Địa chỉ đã nhập" };
    private static async Task<string> Token(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path); response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Check.True(match.Success, "Real Razor form has no antiforgery token: " + path);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private static async Task<HttpResponseMessage> Post(HttpClient client, string route, Dictionary<string, string> fields, string? source = null)
    {
        var values = new Dictionary<string,string>(fields) { ["__RequestVerificationToken"] = await Token(client, source ?? route) };
        return await client.PostAsync(route, new FormUrlEncodedContent(values));
    }
    private static Task<HttpResponseMessage> Login(HttpClient client, string username = "web-first", string password = Password, string destination = "/Account/Profile") =>
        Post(client, "/Account/Login", new() { ["Username"] = username, ["Password"] = password, ["ReturnUrl"] = destination });
    private static async Task<string> Body(HttpResponseMessage response) => WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    private static void Redirect(HttpResponseMessage response, string destination) => Check.True(
        response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location?.OriginalString == destination,
        "Expected PRG redirect to " + destination + ", got " + (int)response.StatusCode + " " + response.Headers.Location);
    private static void NoPasswords(string html) => Check.True(!html.Contains(Password) && !html.Contains(Replacement), "Response exposed a submitted password.");

    public static async Task Run()
    {
        await using var database = await TestDatabase.Create();
        await using (var db = Open(database.ConnectionString)) await new DatabaseMigrator(db).MigrateAsync();
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PhoneStore.slnx"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Solution root missing.");
        await using var factory = new MvcFoundationFactory(database.ConnectionString, directory.FullName);
        using var guest = factory.Browser();
        long firstId, secondId, orderId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider; var registration = services.GetRequiredService<IAccountRegistration>();
            firstId = (await registration.RegisterAsync(new("web-first", Password, "Khách thứ nhất", "0938123401", "first-web@example.test", "Địa chỉ ban đầu"))).Id;
            secondId = (await registration.RegisterAsync(new("web-second", Password, "Khách thứ hai", "0938123402", "second-web@example.test"))).Id;
            var db = services.GetRequiredService<StoreDbContext>();
            var admin = new Account { Username = "web-admin", Role = "ADMIN", Active = true };
            admin.PasswordHash = services.GetRequiredService<IPasswordHasher<Account>>().HashPassword(admin, Password);
            db.Accounts.Add(admin); await db.SaveChangesAsync();
            var sessions = services.GetRequiredService<IAccountSessions>();
            var a = await sessions.LoginAsync(new(admin.Username, Password, "admin")); var c = await sessions.LoginAsync(new("web-first", Password));
            var actor = new Actor(a.Account.Id, a.SessionId); var customer = new Actor(c.Account.Id, c.SessionId);
            var lookups = services.GetRequiredService<LookupService>();
            var brand = await lookups.Save(actor, "brands", 0, new("Web account QA brand"));
            var category = await lookups.Save(actor, "categories", 0, new("Web account QA category"));
            var products = services.GetRequiredService<ProductService>();
            var productId = await products.Save(actor, 0, new("WEB-ACCOUNT", "QA account phone", brand, category, "Specs", "Description", "", true,
                [new(0, "WEB-ACCOUNT-1", "Black", 128, 100m, 20)]));
            var product = await products.Read(actor, productId); var variant = product.Variants[0].Id;
            var supplier = await services.GetRequiredService<SupplierService>().Save(actor, 0, new("WEB-ACCOUNT-SUP", "QA supplier", "0938123499", "QA address"));
            var purchases = services.GetRequiredService<PurchaseService>();
            var receipt = await purchases.Save(actor, 0, new(supplier, [new(variant, 5, 100m)], RequestKey: Guid.NewGuid()));
            await purchases.Confirm(actor, receipt, new((await purchases.Read(actor, receipt)).Version, Guid.NewGuid()));
            var carts = services.GetRequiredService<CartService>(); await carts.Set(customer, new(variant, 1)); var cart = await carts.Read(customer);
            orderId = (await services.GetRequiredService<CheckoutService>().Place(customer, new(Guid.NewGuid(), "Người nhận cũ", "0938123401", "Địa chỉ đơn cũ",
                cart.Lines.Select(x => new PriceConfirmation(x.VariantId, x.Quantity, x.Price!.Value)).ToList()))).Id;
            await carts.Set(customer, new(variant, 2));
        }
        async Task<string> Snapshot(bool accounts = true, bool sessions = true)
        {
            await using var db = Open(database.ConnectionString);
            // Values including hashes/session IDs remain in memory and are never exported.
            return JsonSerializer.Serialize(new {
                Accounts = accounts ? await db.Accounts.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null,
                Customers = await db.Customers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Sessions = sessions ? await db.Sessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null,
                Inventory = await db.Inventory.AsNoTracking().OrderBy(x => x.VariantId).ToListAsync(),
                Variants = await db.Variants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Cart = await db.Cart.AsNoTracking().OrderBy(x => x.CustomerId).ThenBy(x => x.VariantId).ToListAsync(),
                Orders = await db.Orders.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Lines = await db.OrderLines.AsNoTracking().OrderBy(x => x.OrderId).ThenBy(x => x.VariantId).ToListAsync(),
                Payments = await db.Payments.AsNoTracking().OrderBy(x => x.OrderId).ToListAsync(),
                History = await db.OrderHistory.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Movements = await db.Movements.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
            });
        }
        await Check.Run("WEB.K01.01 WEB.K01.03 actual form registers CUSTOMER once without session and ignores overposting", async () =>
        {
            await using var db = Open(database.ConnectionString); var count = await db.Accounts.CountAsync(); var sessions = await db.Sessions.CountAsync();
            var fields = Registration(); fields["role"] = "ADMIN"; fields["active"] = "false"; fields["id"] = secondId.ToString();
            using var response = await Post(guest, "/Account/Register", fields); Redirect(response, "/Account/Login");
            Check.True(!response.Headers.TryGetValues("Set-Cookie", out var cookies) || cookies.All(x => !x.StartsWith("PhoneStore.Session=")), "Registration signed in automatically.");
            var account = await db.Accounts.SingleAsync(x => x.Username == "web-new");
            Check.True(await db.Accounts.CountAsync() == count + 1 && account.Role == "CUSTOMER" && account.Active && account.Id != secondId &&
                await db.Customers.AnyAsync(x => x.Id == account.Id) && await db.Sessions.CountAsync() == sessions, "Registration overposting or atomicity failed.");
            using var notice = await guest.GetAsync("/Account/Login"); Check.True((await Body(notice)).Contains("Đăng ký thành công"), "PRG notice missing.");
            using var repeat = await Post(guest, "/Account/Register", fields);
            Check.True(repeat.StatusCode == HttpStatusCode.OK && await db.Accounts.CountAsync() == count + 1, "Repeated registration created another account.");
        });
        await Check.Run("WEB.K01.02 duplicate username phone email are summary-only and preserve form and exact SQL snapshot", async () =>
        {
            foreach (var fields in new[] { Registration("web-first", "0938123411", "unique-a@example.test"), Registration("web-dup-phone", "0938123401", "unique-b@example.test"), Registration("web-dup-email", "0938123412", "FIRST-WEB@EXAMPLE.TEST") })
            {
                var before = await Snapshot(); using var response = await Post(guest, "/Account/Register", fields); var html = await Body(response);
                Check.True(response.StatusCode == HttpStatusCode.OK && html.Contains("validation-summary-errors") &&
                    !html.Contains("field-validation-error") && html.Contains(fields["Username"]) && html.Contains(fields["Phone"]) && html.Contains(fields["Email"]) &&
                    html.Contains(fields["FullName"]) && html.Contains(fields["Address"]) && await Snapshot() == before, "Duplicate guessed a field, lost safe data or changed SQL."); NoPasswords(html);
            }
        });
        await Check.Run("WEB.K01.04 WEB.K01.05 optional email multiple NULLs invalid fields and password boundaries", async () =>
        {
            foreach (var (username, phone) in new[] { ("web-null-a", "0938123421"), ("web-null-b", "0938123422") })
            { using var response = await Post(guest, "/Account/Register", Registration(username, phone, "")); Redirect(response, "/Account/Login"); }
            await using var db = Open(database.ConnectionString);
            Check.True(await db.Customers.CountAsync(x => x.Email == null) >= 2, "Multiple optional emails not stored as NULL.");
            var invalids = new[] { ("Email", "bad-email"), ("Password", "short1"), ("Password", new string('a', 10)), ("Password", new string('1', 10)), ("Password", new string('a',128)+"1") };
            foreach (var (field, value) in invalids)
            {
                var before = await Snapshot(); var fields = Registration("web-invalid", "0938123423"); fields[field] = value;
                using var response = await Post(guest, "/Account/Register", fields); var html = await Body(response);
                Check.True(response.StatusCode == HttpStatusCode.OK && html.Contains("field-validation-error") &&
                    (field == "Email" ? html.Contains(value) : !html.Contains(value)) && before == await Snapshot(), "Invalid input accepted, safe input lost or password echoed.");
                NoPasswords(html);
            }
            using var form = await guest.GetAsync("/Account/Register"); Check.True((await Body(form)).Contains("10–128 ký tự, gồm chữ và số"), "Password policy missing before submission.");
            foreach (var (username, phone, password) in new[] { ("web-bound-10", "0938123424", "abcdefghj1"), ("web-bound-128", "0938123425", new string('a',127)+"1") })
            { var fields = Registration(username, phone, ""); fields["Password"] = password; using var response = await Post(guest, "/Account/Register", fields); Redirect(response, "/Account/Login"); }
        });
        await Check.Run("WEB.K02.02 WEB.K02.03 credential failures locked and ADMIN accounts share one generic form error without sessions", async () =>
        {
            await using var db = Open(database.ConnectionString); await db.Accounts.Where(x => x.Id == secondId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Active, false));
            var before = await Snapshot(); string? summary = null;
            foreach (var pair in new[] { ("no-web-account", Password), ("web-first", "WrongQa@123"), ("web-second", "WrongQa@123"), ("web-second", Password), ("web-admin", Password) })
            {
                using var response = await Login(guest, pair.Item1, pair.Item2); var html = await Body(response);
                var text = Regex.Match(html, "<div[^>]*class=\"validation-summary-errors\"[\\s\\S]*?</div>").Value;
                Check.True(response.StatusCode == HttpStatusCode.OK && text.Length > 0 && (summary == null || summary == text) && before == await Snapshot(), "Login discloses account state or created a session.");
                summary = text; NoPasswords(html); Check.True(!html.Contains(pair.Item2), "Failed login echoed password.");
            }
            await db.Accounts.Where(x => x.Id == secondId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Active, true));
        });
        using var first = factory.Browser(); using var otherDevice = factory.Browser();
        await Check.Run("WEB.K02.01 WEB.K02.05 WEB.BASE.05 real login local redirects fixed cookie and already-signed-in guard", async () =>
        {
            using var response = await Login(first); Redirect(response, "/Account/Profile");
            var cookie = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("PhoneStore.Session="));
            Check.True(cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase) && cookie.Contains("secure", StringComparison.OrdinalIgnoreCase), "MVC cookie flags wrong.");
            await using var db = Open(database.ConnectionString); Check.True(await db.Sessions.Where(x => x.AccountId == firstId).AllAsync(x => x.ExpiresAt > DateTime.UtcNow.AddHours(7.9) && x.ExpiresAt <= DateTime.UtcNow.AddHours(8)), "MVC session is not fixed eight hours.");
            using var profile = await first.GetAsync("/Account/Profile"); Check.True(profile.StatusCode == HttpStatusCode.OK && profile.Headers.CacheControl?.NoStore == true &&
                (!profile.Headers.TryGetValues("Set-Cookie", out var refreshed) || refreshed.All(x => !x.StartsWith("PhoneStore.Session="))), "Personal page cached or session sliding.");
            var count = await db.Sessions.CountAsync(); using var signedIn = await first.GetAsync("/Account/Login"); Redirect(signedIn, "/Home/Index");
            using var register = await first.GetAsync("/Account/Register"); Redirect(register, "/Home/Index");
            Check.True(count == await db.Sessions.CountAsync(), "Already signed-in visit created another session.");
            foreach (var destination in new[] { "https://evil.example", "//evil.example", "/\\evil.example", "https://localhost.evil.example" })
            {
                using var client = factory.Browser(); using var login = await Login(client, destination: destination); Redirect(login, "/Home/Index");
            }
            using var secondLogin = await Login(otherDevice); Redirect(secondLogin, "/Account/Profile");
        });
        await Check.Run("WEB.K04.04 anonymous profile and password challenge locally with no-store", async () =>
        {
            foreach (var route in new[] { "/Account/Profile", "/Account/Password" })
            { using var response = await guest.GetAsync(route); Check.True(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.OriginalString.StartsWith("/Account/Login?returnUrl=", StringComparison.Ordinal) && response.Headers.CacheControl?.NoStore == true, "Anonymous personal route did not challenge safely."); }
        });
        var validProfile = new Dictionary<string,string> { ["FullName"] = "Khách đã cập nhật", ["Phone"] = "0938123401", ["Email"] = "first-web@example.test", ["Address"] = "Địa chỉ mới" };
        await Check.Run("WEB.K04.01 WEB.K04.03 profile binds session ownership ignores identity role active and preserves order snapshots", async () =>
        {
            var fields = new Dictionary<string,string>(validProfile) { ["customerId"] = secondId.ToString(), ["id"] = secondId.ToString(), ["role"] = "ADMIN", ["active"] = "false", ["Username"] = "hijacked" };
            await using var db = Open(database.ConnectionString); var oldOrder = JsonSerializer.Serialize(await db.Orders.AsNoTracking().SingleAsync(x => x.Id == orderId));
            var other = JsonSerializer.Serialize(await db.Customers.AsNoTracking().SingleAsync(x => x.Id == secondId));
            var inventory = JsonSerializer.Serialize(await db.Inventory.AsNoTracking().OrderBy(x => x.VariantId).ToListAsync());
            using var response = await Post(first, "/Account/Profile", fields); Redirect(response, "/Account/Profile");
            using var rendered = await first.GetAsync("/Account/Profile"); var html = await Body(rendered);
            Check.True(html.Contains(validProfile["FullName"]) && html.Contains(validProfile["Address"]) && html.Contains("Đã lưu thông tin") && !html.Contains("hijacked"), "Profile PRG lost saved values.");
            var stored = await db.Customers.AsNoTracking().SingleAsync(x => x.Id == firstId); var account = await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == firstId);
            Check.True(stored.FullName == validProfile["FullName"] && stored.Address == validProfile["Address"] && account.Role == "CUSTOMER" && account.Active && account.Username == "web-first" &&
                other == JsonSerializer.Serialize(await db.Customers.AsNoTracking().SingleAsync(x => x.Id == secondId)) && oldOrder == JsonSerializer.Serialize(await db.Orders.AsNoTracking().SingleAsync(x => x.Id == orderId)) &&
                inventory == JsonSerializer.Serialize(await db.Inventory.AsNoTracking().OrderBy(x => x.VariantId).ToListAsync()), "Profile overposting changed foreign records, role, stock or old order.");
        });
        await Check.Run("WEB.K04.02 duplicate profile fields summary-only exact SQL rollback and own unchanged identity accepted", async () =>
        {
            foreach (var (field, value) in new[] { ("Phone", "0938123402"), ("Email", "SECOND-WEB@EXAMPLE.TEST") })
            {
                var before = await Snapshot(); var fields = new Dictionary<string,string>(validProfile) { [field] = value };
                using var response = await Post(first, "/Account/Profile", fields); var html = await Body(response);
                Check.True(response.StatusCode == HttpStatusCode.OK && html.Contains("validation-summary-errors") && !html.Contains("field-validation-error") && html.Contains(value) &&
                    html.Contains(fields["FullName"]) && html.Contains(fields["Address"]) && before == await Snapshot(), "Profile duplicate lost values or partially wrote.");
            }
            using var unchanged = await Post(first, "/Account/Profile", validProfile); Redirect(unchanged, "/Account/Profile");
            var beforeInvalid = await Snapshot(); using var invalid = await Post(first, "/Account/Profile", new(validProfile) { ["Phone"] = "invalid" });
            Check.True(invalid.StatusCode == HttpStatusCode.OK && (await Body(invalid)).Contains("field-validation-error") && beforeInvalid == await Snapshot(), "Invalid profile partly updated SQL.");
        });
        await Check.Run("WEB.BASE.02 account POST missing and invalid antiforgery preserve complete SQL snapshot", async () =>
        {
            foreach (var (client, route, fields) in new[] { (guest, "/Account/Register", Registration("csrf-web", "0938123451", "")),
                (guest, "/Account/Login", new Dictionary<string,string> { ["Username"] = "web-first", ["Password"] = Password }),
                (first, "/Account/Profile", validProfile), (first, "/Account/Password", new Dictionary<string,string> { ["CurrentPassword"] = Password, ["NewPassword"] = Replacement }),
                (first, "/Account/Logout", new Dictionary<string,string>()) })
                foreach (var token in new[] { "", "invalid-token" })
                {
                    var before = await Snapshot(); var values = new Dictionary<string,string>(fields); if (token.Length > 0) values["__RequestVerificationToken"] = token;
                    using var response = await client.PostAsync(route, new FormUrlEncodedContent(values));
                    Check.True(response.StatusCode == HttpStatusCode.BadRequest && before == await Snapshot(), "Account POST without CSRF changed state: " + route);
                }
        });
        await Check.Run("WEB.BASE.04 profile names and addresses are encoded in actual Razor form HTML", async () =>
        {
            var fields = new Dictionary<string,string>(validProfile) { ["FullName"] = "<script>web-profile-name</script>", ["Address"] = "<script>web-profile-address</script>" };
            using var saved = await Post(first, "/Account/Profile", fields); Redirect(saved, "/Account/Profile");
            using var page = await first.GetAsync("/Account/Profile"); var html = await page.Content.ReadAsStringAsync();
            Check.True(html.Contains("&lt;script&gt;web-profile-name&lt;/script&gt;") && html.Contains("&lt;script&gt;web-profile-address&lt;/script&gt;") && !html.Contains("<script>web-profile"), "Profile values rendered executable HTML.");
            foreach (var forbidden in new[] { "PasswordHash", "OnHand", "Reserved", "ActorId", "OperationKey", "ConnectionStrings", "data_source" })
                Check.True(!html.Contains(forbidden, StringComparison.OrdinalIgnoreCase), "Profile HTML exposes forbidden field.");
            using var restored = await Post(first, "/Account/Profile", validProfile); Redirect(restored, "/Account/Profile");
        });
        await Check.Run("WEB.K05.01 invalid current/new password gives field errors with no SQL mutation or echoed secrets", async () =>
        {
            foreach (var pair in new[] { ("WrongQa@123", Replacement, "CurrentPassword"), (Password, "short1", "NewPassword"), (Password, "abcdefghij", "NewPassword"), (Password, "1234567890", "NewPassword") })
            {
                var before = await Snapshot(); using var response = await Post(first, "/Account/Password", new() { ["CurrentPassword"] = pair.Item1, ["NewPassword"] = pair.Item2 }); var html = await Body(response);
                Check.True(response.StatusCode == HttpStatusCode.OK && html.Contains("data-valmsg-for=\"" + pair.Item3 + "\"") && html.Contains("field-validation-error") && before == await Snapshot() && !html.Contains(pair.Item1) && !html.Contains(pair.Item2), "Password error mapping, redaction or rollback failed.");
                NoPasswords(html);
            }
        });
        await Check.Run("WEB.K03.01 WEB.K03.02 POST logout revokes only current session cookie replay fails and business SQL preserved", async () =>
        {
            using var currentDevice = factory.Browser(); using var login = await Login(currentDevice); var cookie = login.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("PhoneStore.Session=")).Split(';')[0];
            var business = await Snapshot(sessions:false); await using var db = Open(database.ConnectionString); var sessionsBefore = await db.Sessions.Where(x => x.AccountId == firstId).CountAsync();
            using var get = await currentDevice.GetAsync("/Account/Logout"); Check.True(get.StatusCode == HttpStatusCode.MethodNotAllowed && sessionsBefore == await db.Sessions.Where(x => x.AccountId == firstId).CountAsync(), "GET logged out.");
            using var logout = await Post(currentDevice, "/Account/Logout", new(), "/Account/Profile"); Redirect(logout, "/Home/Index");
            Check.True(await Snapshot(sessions:false) == business && await db.Sessions.Where(x => x.AccountId == firstId).CountAsync() == sessionsBefore - 1, "Logout deleted business records or other sessions.");
            using var request = new HttpRequestMessage(HttpMethod.Get, "/Account/Profile"); request.Headers.Add("Cookie", cookie);
            using var replay = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false, BaseAddress = new Uri("https://localhost") });
            using var rejected = await replay.SendAsync(request); Check.True(rejected.StatusCode == HttpStatusCode.Redirect, "Old cookie remained usable.");
            using var stillSignedIn = await otherDevice.GetAsync("/Account/Profile"); Check.True(stillSignedIn.IsSuccessStatusCode, "Other device was logged out.");
            using var anonymous = await currentDevice.GetAsync("/Home/Index"); var html = await Body(anonymous);
            Check.True(!html.Contains("Giỏ hàng (2)") && !html.Contains("Khách đã cập nhật"), "Logout leaked previous user's cart/profile.");
        });
        await Check.Run("WEB.K03.03 expired session logout redirects to login without effects or 500", async () =>
        {
            using var expired = factory.Browser(); using var login = await Login(expired, "web-second"); var token = await Token(expired, "/Account/Profile");
            await using var db = Open(database.ConnectionString); await db.Sessions.Where(x => x.AccountId == secondId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
            var before = await Snapshot(); using var response = await expired.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = token }));
            Check.True(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.OriginalString.StartsWith("/Account/Login", StringComparison.Ordinal) && before == await Snapshot(), "Expired logout changed SQL or returned500.");
        });
        await Check.Run("WEB.K05.02 WEB.K05.03 successful password PRG revokes all SQL sessions and both devices", async () =>
        {
            var business = await Snapshot(accounts:false, sessions:false); await using var db = Open(database.ConnectionString);
            var oldHash = (await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == firstId)).PasswordHash;
            Check.True(await db.Sessions.CountAsync(x => x.AccountId == firstId) >= 2, "Revocation fixture needs two devices.");
            using var response = await Post(first, "/Account/Password", new() { ["CurrentPassword"] = Password, ["NewPassword"] = Replacement }); Redirect(response, "/Account/Login");
            NoPasswords(await Body(response)); Check.True(await db.Sessions.CountAsync(x => x.AccountId == firstId) == 0 &&
                oldHash != (await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == firstId)).PasswordHash && business == await Snapshot(accounts:false,sessions:false), "Password revocation changed business records or left sessions.");
            foreach (var device in new[] { first, otherDevice }) { using var denied = await device.GetAsync("/Account/Profile"); Check.True(denied.StatusCode == HttpStatusCode.Redirect, "Password change left a device active."); }
            using var page = await first.GetAsync("/Account/Login"); var html = await Body(page); Check.True(html.Contains("đăng xuất mọi thiết bị"), "Password-change notice missing."); NoPasswords(html);
            using var oldLogin = await Login(first); Check.True(oldLogin.StatusCode == HttpStatusCode.OK && (await Body(oldLogin)).Contains("validation-summary-errors"), "Old password still accepted.");
            using var newLogin = await Login(first, password:Replacement); Redirect(newLogin, "/Account/Profile");
        });
        await Check.Run("WEB.BASE.08 ADMIN cookie cannot edit customer profile or password while admin API remains valid", async () =>
        {
            using var admin = factory.Browser(); var token = await Token(admin, "/Account/Login"); using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new LoginRequest("web-admin", Password, "admin")) };
            request.Headers.Add("X-CSRF-TOKEN", token); using var login = await admin.SendAsync(request); login.EnsureSuccessStatusCode();
            var before = await Snapshot();
            foreach (var route in new[] { "/Account/Profile", "/Account/Password" })
            { using var denied = await admin.GetAsync(route); Check.True(denied.StatusCode == HttpStatusCode.Forbidden, "ADMIN entered personal customer form."); }
            using var api = await admin.GetAsync("/api/admin/session"); Check.True(api.IsSuccessStatusCode && before == await Snapshot(), "MVC changed admin API behavior or SQL.");
        });
    }
}
