using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class SessionChecks
{
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 30, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    public static async Task Run(string connection)
    {
        StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
        var clock = new TestClock(); var passwords = new PasswordHasher<Account>();
        const string password = "SessionChecks@12345";
        long customerId;
        await using (var db = Open())
        {
            var customer = new Account { Username = "session-customer" };
            var admin = new Account { Username = "session-admin", Role = "ADMIN" };
            var locked = new Account { Username = "session-locked", Active = false };
            foreach (var account in new[] { customer, admin, locked }) account.PasswordHash = passwords.HashPassword(account, password);
            db.AddRange(customer, admin, locked); await db.SaveChangesAsync(); customerId = customer.Id;
        }
        async Task<AccountSession> Login(LoginRequest request)
        { await using var db = Open(); return await new AccountSessions(db, passwords, clock).LoginAsync(request); }
        async Task<AccountSession?> Validate(long account, Guid session)
        { await using var db = Open(); return await new AccountSessions(db, passwords, clock).ValidateAsync(account, session); }
        await Check.Run("K02/A01 invalid, locked or wrong-surface credentials create no sessions", async () =>
        {
            foreach (var input in new[] {
                new LoginRequest("session-customer", "wrong"), new("unknown-account", password), new("session-locked", password),
                new("session-customer", password, "admin"), new("session-admin", password), new(null, null), new("session-customer", new string('a',129)) })
            {
                try { await Login(input); throw new InvalidOperationException("Invalid credentials accepted."); }
                catch (BusinessException exception) when (exception.Code == "UNAUTHORIZED") { }
            }
            try { await Login(new("session-customer", password, "staff")); throw new InvalidOperationException("Invalid surface accepted."); }
            catch (BusinessException exception) when (exception.Code == "INVALID_INPUT") { }
            await using var db = Open();
            Check.True(!await db.Sessions.AnyAsync(x => x.AccountId == customerId), "Failed login created sessions.");
        });
        await Check.Run("K02/A01 persist role-specific sessions with exact fixed eight-hour expiry", async () =>
        {
            var customer = await Login(new(" SESSION-CUSTOMER ", password));
            var admin = await Login(new("session-admin", password, "admin"));
            Check.True(customer.Account.Role == "CUSTOMER" && admin.Account.Role == "ADMIN" && customer.ExpiresAt == clock.Now.AddHours(8), "Login role/lifetime was incorrect.");
            Check.True(await Validate(admin.Account.Id, customer.SessionId) is null, "Session accepted with a different account ID.");
            clock.Now = clock.Now.AddHours(7);
            var valid = await Validate(customer.Account.Id, customer.SessionId);
            Check.True(valid?.ExpiresAt == customer.ExpiresAt, "Session validation slid the expiry.");
            clock.Now = customer.ExpiresAt;
            Check.True(await Validate(customer.Account.Id, customer.SessionId) is null, "Session remained valid at exact expiry boundary.");
        });
        await Check.Run("SQL session validation observes deletion and current active account state", async () =>
        {
            var session = await Login(new("session-customer", password));
            await using (var db = Open()) await db.Accounts.Where(x => x.Id == customerId).ExecuteUpdateAsync(x => x.SetProperty(a => a.Active, false));
            Check.True(await Validate(customerId, session.SessionId) is null, "Locked account retained access.");
            await using (var db = Open())
            {
                await db.Accounts.Where(x => x.Id == customerId).ExecuteUpdateAsync(x => x.SetProperty(a => a.Active, true));
                await db.Sessions.Where(x => x.Id == session.SessionId).ExecuteDeleteAsync();
            }
            Check.True(await Validate(customerId, session.SessionId) is null, "Deleted session retained access.");
        });
    }
}
