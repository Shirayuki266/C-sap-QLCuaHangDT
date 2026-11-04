using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal sealed class CommerceContext(string connection, Uri address, string prefix) : IDisposable
{
    private static int sequence;
    public const string Password = "CommerceChecks@12345";
    public Actor Admin { get; private set; } = null!;
    public Actor Customer { get; private set; } = null!;
    public ApiTestClient AdminClient { get; } = new(address);
    public ApiTestClient CustomerClient { get; } = new(address);
    public StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
    public BusinessKernel Kernel(StoreDbContext db) => new(db, TimeProvider.System);
    public async Task Initialize()
    {
        await using var db = Open(); var hasher = new PasswordHasher<Account>();
        var admin = new Account { Username = prefix + "-admin", Role = "ADMIN" }; admin.PasswordHash = hasher.HashPassword(admin, Password);
        db.Add(admin); await db.SaveChangesAsync();
        var phone = "098" + (10000000 + Interlocked.Increment(ref sequence)).ToString();
        var customer = await new AccountRegistration(db, hasher).RegisterAsync(new(prefix + "-customer", Password, "Khách kiểm thử", phone));
        var sessions = new AccountSessions(db, hasher, TimeProvider.System);
        var a = await sessions.LoginAsync(new(admin.Username, Password, "admin")); var c = await sessions.LoginAsync(new(customer.Username, Password));
        Admin = new(a.Account.Id, a.SessionId); Customer = new(c.Account.Id, c.SessionId);
        using var ar = await AdminClient.Send("/api/auth/login", new LoginRequest(admin.Username, Password, "admin"));
        using var cr = await CustomerClient.Send("/api/auth/login", new LoginRequest(customer.Username, Password));
        Check.True(ar.IsSuccessStatusCode && cr.IsSuccessStatusCode, "Commerce fixture authentication failed.");
    }
    public static async Task Reject(string code, Func<Task> action)
    {
        try { await action(); } catch (BusinessException error) when (error.Code == code) { return; }
        throw new InvalidOperationException("Expected business error " + code);
    }
    public async Task RefreshCustomerSession()
    {
        await using var db=Open();
        var username=await db.Accounts.Where(x=>x.Id==Customer.AccountId).Select(x=>x.Username).SingleAsync();
        var login=await new AccountSessions(db,new PasswordHasher<Account>(),TimeProvider.System).LoginAsync(new(username,Password));
        Customer=new(login.Account.Id,login.SessionId);
    }
    public void Dispose() { AdminClient.Dispose(); CustomerClient.Dispose(); }
}
