using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class LogoutChecks
{
    public static async Task Run(Uri address, string connection)
    {
        StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
        const string password = "LogoutChecks@12345";
        long customerId, adminId;
        await using (var db = Open())
        {
            var hasher = new PasswordHasher<Account>();
            var customer = new Account { Username = "logout-customer" };
            var admin = new Account { Username = "logout-admin", Role = "ADMIN" };
            customer.PasswordHash = hasher.HashPassword(customer, password); admin.PasswordHash = hasher.HashPassword(admin, password);
            db.AddRange(customer, admin); await db.SaveChangesAsync(); customerId = customer.Id; adminId = admin.Id;
        }
        using var first = new ApiTestClient(address); using var second = new ApiTestClient(address); using var adminClient = new ApiTestClient(address);
        async Task Login(ApiTestClient client, string name, string surface = "customer")
        {
            using var response = await client.Send("/api/auth/login", new LoginRequest(name, password, surface));
            Check.True(response.IsSuccessStatusCode, "Logout fixture login failed.");
        }
        await Login(first, "logout-customer"); await Login(second, "logout-customer"); await Login(adminClient, "logout-admin", "admin");
        var oldCookie = first.SessionCookie();
        await Check.Run("K03/A02 CSRF failure does not revoke any valid session", async () =>
        {
            using var response = await first.Send("/api/auth/logout", new { }, csrf: false);
            Check.True(response.StatusCode == HttpStatusCode.BadRequest, "Logout accepted missing CSRF.");
            await using var db = Open();
            Check.True(await db.Sessions.CountAsync(x => x.AccountId == customerId) == 2, "Failed CSRF revoked sessions.");
        });
        await Check.Run("K03 revokes only the current session and rejects its captured cookie", async () =>
        {
            using var response = await first.Send("/api/auth/logout", new { accountId = adminId, sessionId = Guid.NewGuid() });
            Check.True(response.StatusCode == HttpStatusCode.NoContent, "Logout did not succeed.");
            using var current = await first.Http.GetAsync("/api/auth/session");
            using var otherDevice = await second.Http.GetAsync("/api/auth/session");
            using var replay = new ApiTestClient(address); replay.Replay(oldCookie);
            using var replayed = await replay.Http.GetAsync("/api/auth/session");
            Check.True(current.StatusCode == HttpStatusCode.Unauthorized && replayed.StatusCode == HttpStatusCode.Unauthorized && otherDevice.IsSuccessStatusCode, "Logout session boundaries failed.");
            await using var db = Open();
            Check.True(await db.Sessions.CountAsync(x => x.AccountId == customerId) == 1 && await db.Sessions.CountAsync(x => x.AccountId == adminId) == 1, "Logout affected another device/account.");
        });
        await Check.Run("A02 logout and unauthenticated repeats preserve other sessions", async () =>
        {
            using var adminLogout = await adminClient.Send("/api/auth/logout", new { });
            Check.True(adminLogout.StatusCode == HttpStatusCode.NoContent, "ADMIN logout failed.");
            using var repeated = await first.Send("/api/auth/logout", new { });
            Check.True(repeated.StatusCode == HttpStatusCode.Unauthorized, "Already logged-out request was accepted.");
            using var stillActive = await second.Http.GetAsync("/api/auth/session");
            Check.True(stillActive.IsSuccessStatusCode, "Repeated logout revoked another device.");
        });
        await Check.Run("Concurrent logout revokes a session once without server errors", async () =>
        {
            var cookie = second.SessionCookie();
            using var racerA = new ApiTestClient(address); using var racerB = new ApiTestClient(address);
            racerA.Replay(cookie); racerB.Replay(cookie);
            var responses = await Task.WhenAll(racerA.Send("/api/auth/logout", new { }), racerB.Send("/api/auth/logout", new { }));
            try
            {
                Check.True(responses.Count(x => x.StatusCode == HttpStatusCode.NoContent) == 1 &&
                    responses.All(x => x.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Unauthorized), "Concurrent logout produced wrong status/duplicate success.");
            }
            finally { foreach (var response in responses) response.Dispose(); }
            await using var db = Open();
            Check.True(!await db.Sessions.AnyAsync(x => x.AccountId == customerId), "Concurrent logout left the session valid.");
        });
        await Check.Run("Logout service refuses foreign session ownership", async () =>
        {
            await using var db = Open();
            var sessions = new AccountSessions(db, new PasswordHasher<Account>(), TimeProvider.System);
            var session = await sessions.LoginAsync(new("logout-customer", password));
            try { await sessions.RevokeAsync(adminId, session.SessionId); throw new InvalidOperationException("Foreign session revoked."); }
            catch (BusinessException exception) when (exception.Code == "UNAUTHORIZED") { }
            Check.True(await sessions.ValidateAsync(customerId, session.SessionId) is not null, "Foreign logout changed the session.");
        });
    }
}
