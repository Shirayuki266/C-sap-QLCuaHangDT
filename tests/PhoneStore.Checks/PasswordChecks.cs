using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class PasswordChecks
{
    public static async Task Run(Uri address, string connection)
    {
        StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
        const string oldPassword = "PasswordOld@12345", newPassword = "PasswordNew@12345";
        var hasher = new PasswordHasher<Account>();
        async Task<Account> Create(string name, string role = "CUSTOMER")
        {
            await using var db = Open(); var account = new Account { Username = name, Role = role };
            account.PasswordHash = hasher.HashPassword(account, oldPassword); db.Add(account); await db.SaveChangesAsync(); return account;
        }
        async Task Login(ApiTestClient client, string name, string role = "CUSTOMER", string password = oldPassword)
        {
            using var result = await client.Send("/api/auth/login", new LoginRequest(name, password, role == "ADMIN" ? "admin" : "customer"));
            Check.True(result.IsSuccessStatusCode, "Password fixture login failed.");
        }
        foreach (var role in new[] { "CUSTOMER", "ADMIN" })
        {
            var account = await Create("password-" + role.ToLowerInvariant(), role);
            using var first = new ApiTestClient(address); using var second = new ApiTestClient(address);
            await Login(first, account.Username, role); await Login(second, account.Username, role);
            var firstCookie = first.SessionCookie(); var secondCookie = second.SessionCookie();
            await Check.Run($"K05/A03 {role} invalid input/CSRF preserves password and sessions", async () =>
            {
                foreach (var input in new[] { new PasswordChangeRequest("wrong", newPassword), new(oldPassword, "short1"), new(null, newPassword), new(oldPassword, null) })
                {
                    using var response = await first.Send("/api/account/password", input);
                    Check.True(response.StatusCode == HttpStatusCode.BadRequest, "Invalid password change was accepted.");
                }
                using var csrf = await first.Send("/api/account/password", new PasswordChangeRequest(oldPassword, newPassword), csrf: false);
                using var overpost = await first.Send("/api/account/password", new { currentPassword = oldPassword, newPassword, accountId = long.MaxValue });
                Check.True(csrf.StatusCode == HttpStatusCode.BadRequest && overpost.StatusCode == HttpStatusCode.BadRequest, "Password CSRF/actor overposting was accepted.");
                await using var db = Open();
                var stored = await db.Accounts.SingleAsync(x => x.Id == account.Id);
                Check.True(stored.PasswordHash == account.PasswordHash && await db.Sessions.CountAsync(x => x.AccountId == account.Id) == 2, "Invalid password request changed data.");
            });
            await Check.Run($"K05/A03 {role} change revokes every old cookie and requires new credentials", async () =>
            {
                await using var before = Open(); var unrelated = await before.Sessions.CountAsync(x => x.AccountId != account.Id);
                using var changed = await first.Send("/api/account/password", new PasswordChangeRequest(oldPassword, newPassword));
                Check.True(changed.StatusCode == HttpStatusCode.NoContent, "Password change failed.");
                foreach (var cookie in new[] { firstCookie, secondCookie })
                {
                    using var replay = new ApiTestClient(address); replay.Replay(cookie);
                    using var response = await replay.Http.GetAsync("/api/auth/session");
                    Check.True(response.StatusCode == HttpStatusCode.Unauthorized, "Old cookie survived password change.");
                }
                await using var db = Open();
                Check.True(!await db.Sessions.AnyAsync(x => x.AccountId == account.Id) && await db.Sessions.CountAsync(x => x.AccountId != account.Id) == unrelated, "Session revocation crossed account boundaries.");
                using var fresh = new ApiTestClient(address);
                using var denied = await fresh.Send("/api/auth/login", new LoginRequest(account.Username, oldPassword, role == "ADMIN" ? "admin" : "customer"));
                Check.True(denied.StatusCode == HttpStatusCode.Unauthorized, "Old password still worked.");
                await Login(fresh, account.Username, role, newPassword);
            });
        }
        await Check.Run("Concurrent password changes revalidate the SQL session under the write lock", async () =>
        {
            var account = await Create("password-race");
            async Task<AccountSession> Session() { await using var db = Open(); return await new AccountSessions(db, hasher, TimeProvider.System).LoginAsync(new(account.Username, oldPassword)); }
            var first = await Session(); var second = await Session();
            async Task<bool> Change(Guid session, string password)
            {
                await using var db = Open();
                try { await new AccountPasswords(db, hasher, TimeProvider.System).ChangeAsync(account.Id, session, new(oldPassword, password)); return true; }
                catch (BusinessException exception) when (exception.Code == "UNAUTHORIZED") { return false; }
            }
            var results = await Task.WhenAll(Change(first.SessionId, "ConcurrentA@12345"), Change(second.SessionId, "ConcurrentB@12345"));
            Check.True(results.Count(x => x) == 1, "More than one password change accepted the revoked sessions.");
            await using var verify = Open(); Check.True(!await verify.Sessions.AnyAsync(x => x.AccountId == account.Id), "Concurrent password changes left a session alive.");
        });
        await Check.Run("Late SQL password write failure rolls back hash and all session deletions", async () =>
        {
            var account = await Create("password-rollback");
            await using var db = Open();
            var sessions = new AccountSessions(db, hasher, TimeProvider.System);
            var first = await sessions.LoginAsync(new(account.Username, oldPassword));
            await sessions.LoginAsync(new(account.Username, oldPassword));
            // A fixture-only CHECK rejects the replacement hash after the service deletes sessions.
            // Proper SQL literal escaping applies even to this test-only base64 value.
            var literal = account.PasswordHash.Replace("'", "''", StringComparison.Ordinal);
            var constraint = "ALTER TABLE dbo.TAI_KHOAN ADD CONSTRAINT CK_TestPasswordRollback CHECK (Username<>N'password-rollback' OR PasswordHash=N'" + literal + "')";
            await db.Database.ExecuteSqlRawAsync(constraint);
            try
            {
                var rejected = false;
                try { await new AccountPasswords(db, hasher, TimeProvider.System).ChangeAsync(account.Id, first.SessionId, new(oldPassword, newPassword)); }
                catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 }) { rejected = true; }
                Check.True(rejected, "SQL failure fixture did not reject the password write.");
                await using var verify = Open();
                Check.True((await verify.Accounts.SingleAsync(x => x.Id == account.Id)).PasswordHash == account.PasswordHash &&
                    await verify.Sessions.CountAsync(x => x.AccountId == account.Id) == 2, "Password transaction failed to roll back all effects.");
            }
            finally { await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.TAI_KHOAN DROP CONSTRAINT CK_TestPasswordRollback"); }
        });
    }
}
