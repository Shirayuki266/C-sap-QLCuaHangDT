using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class SessionHttpChecks
{
    public static async Task Run(Uri address, string connection)
    {
        StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
        const string password = "HttpSessions@12345";
        long customerId;
        await using (var db = Open())
        {
            var customer = new Account { Username = "http-session-customer" };
            var admin = new Account { Username = "http-session-admin", Role = "ADMIN" };
            var locked = new Account { Username = "http-session-locked", Active = false };
            var hasher = new PasswordHasher<Account>();
            foreach (var account in new[] { customer, admin, locked }) account.PasswordHash = hasher.HashPassword(account, password);
            db.AddRange(customer, admin, locked); await db.SaveChangesAsync(); customerId = customer.Id;
        }
        using var guest = new ApiTestClient(address);
        using var customerClient = new ApiTestClient(address);
        using var adminClient = new ApiTestClient(address);
        async Task Status(HttpResponseMessage response, HttpStatusCode expected, string code)
        {
            using (response)
            {
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Check.True(response.StatusCode == expected && body.RootElement.GetProperty("code").GetString() == code, "Authentication HTTP error contract failed.");
                Check.True(response.Headers.Location is null, "API authentication redirected to HTML.");
            }
        }
        await Check.Run("HTTP login rejects CSRF, invalid credentials, locked accounts and role injection", async () =>
        {
            await Status(await guest.Http.GetAsync("/api/auth/session"), HttpStatusCode.Unauthorized, "UNAUTHORIZED");
            await Status(await guest.Send("/api/auth/login", new LoginRequest("http-session-customer", password), csrf: false), HttpStatusCode.BadRequest, "INVALID_INPUT");
            foreach (var request in new[] { new LoginRequest("http-session-customer", "wrong"), new("missing-user", password),
                new("http-session-locked", password), new("http-session-customer", password, "admin"), new("http-session-admin", password) })
                await Status(await guest.Send("/api/auth/login", request), HttpStatusCode.Unauthorized, "UNAUTHORIZED");
            await Status(await guest.Send("/api/auth/login", new LoginRequest("http-session-customer", password, "staff")), HttpStatusCode.BadRequest, "INVALID_INPUT");
            await Status(await guest.Send("/api/auth/login", new { username = "http-session-customer", password, role = "ADMIN" }), HttpStatusCode.BadRequest, "INVALID_INPUT");
            await using var db = Open();
            Check.True(!await db.Sessions.AnyAsync(x => x.AccountId == customerId), "Rejected login created a session.");
        });
        string customerCookie = "";
        await Check.Run("HTTP customer/admin login issues protected cookies and safe session identities", async () =>
        {
            var earliest = DateTimeOffset.UtcNow.AddHours(8);
            using var response = await customerClient.Send("/api/auth/login", new LoginRequest(" HTTP-SESSION-CUSTOMER ", password));
            var text = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(text);
            Check.True(response.IsSuccessStatusCode && json.RootElement.GetProperty("role").GetString() == "CUSTOMER", "Customer login failed.");
            var expiry = json.RootElement.GetProperty("expiresAt").GetDateTimeOffset();
            Check.True(expiry >= earliest && expiry <= DateTimeOffset.UtcNow.AddHours(8), "Login expiry is not eight hours.");
            Check.True(!text.Contains("sessionId", StringComparison.OrdinalIgnoreCase) && !text.Contains("password", StringComparison.OrdinalIgnoreCase), "Login response exposed secrets.");
            var cookie = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("PhoneStore.Session=", StringComparison.Ordinal));
            Check.True(cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase), "Missing cookie protection attributes.");
            customerCookie = customerClient.SessionCookie();
            using var adminLogin = await adminClient.Send("/api/auth/login", new LoginRequest("http-session-admin", password, "admin"));
            Check.True(adminLogin.IsSuccessStatusCode, "Admin login failed.");
            using var sessionResponse = await customerClient.Http.GetAsync("/api/auth/session");
            Check.True(sessionResponse.IsSuccessStatusCode && !sessionResponse.Headers.TryGetValues("Set-Cookie", out _), "Session read failed or slid the cookie lifetime.");
            using var adminSession = await adminClient.Http.GetAsync("/api/admin/session");
            Check.True(adminSession.IsSuccessStatusCode, "ADMIN session was denied.");
            await Status(await customerClient.Http.GetAsync("/api/admin/session"), HttpStatusCode.Forbidden, "FORBIDDEN");
        });
        await Check.Run("Authenticated re-login and K01 registration are rejected without extra writes", async () =>
        {
            await using var db = Open(); var sessions = await db.Sessions.CountAsync(); var accounts = await db.Accounts.CountAsync();
            await Status(await customerClient.Send("/api/auth/login", new LoginRequest("http-session-customer", password)), HttpStatusCode.Conflict, "INVALID_STATE");
            await Status(await customerClient.Send("/api/auth/register", new RegistrationRequest("must-not-register", "Registration@12345", "Tên", "0991234562")), HttpStatusCode.Conflict, "INVALID_STATE");
            Check.True(await db.Sessions.CountAsync() == sessions && await db.Accounts.CountAsync() == accounts, "Signed-in guest commands changed data.");
        });
        await Check.Run("HTTP session validation rejects tampering, expired/revoked sessions and changed roles", async () =>
        {
            using var tampered = new ApiTestClient(address); tampered.Replay(customerCookie + "invalid");
            await Status(await tampered.Http.GetAsync("/api/auth/session"), HttpStatusCode.Unauthorized, "UNAUTHORIZED");
            await using (var db = Open()) await db.Sessions.Where(x => x.AccountId == customerId).ExecuteUpdateAsync(x => x.SetProperty(s => s.ExpiresAt, DateTime.UtcNow.AddSeconds(-1)));
            using var expired = new ApiTestClient(address); expired.Replay(customerCookie);
            await Status(await expired.Http.GetAsync("/api/auth/session"), HttpStatusCode.Unauthorized, "UNAUTHORIZED");
            using var fresh = new ApiTestClient(address);
            using var login = await fresh.Send("/api/auth/login", new LoginRequest("http-session-customer", password));
            Check.True(login.IsSuccessStatusCode, "Fresh login failed.");
            var replay = fresh.SessionCookie();
            await using (var db = Open()) await db.Accounts.Where(x => x.Id == customerId).ExecuteUpdateAsync(x => x.SetProperty(a => a.Active, false));
            await Status(await fresh.Http.GetAsync("/api/auth/session"), HttpStatusCode.Unauthorized, "UNAUTHORIZED");
            await using (var db = Open()) await db.Accounts.Where(x => x.Id == customerId).ExecuteUpdateAsync(x => x.SetProperty(a => a.Active, true).SetProperty(a => a.Role, "ADMIN"));
            using var changedRole = new ApiTestClient(address); changedRole.Replay(replay);
            await Status(await changedRole.Http.GetAsync("/api/admin/session"), HttpStatusCode.Unauthorized, "UNAUTHORIZED");
            await using (var db = Open())
            {
                await db.Accounts.Where(x => x.Id == customerId).ExecuteUpdateAsync(x => x.SetProperty(a => a.Role, "CUSTOMER"));
                await db.Sessions.Where(x => x.AccountId == customerId).ExecuteDeleteAsync();
            }
            using var revoked = new ApiTestClient(address); revoked.Replay(replay);
            await Status(await revoked.Http.GetAsync("/api/auth/session"), HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        });
    }
}
