using System.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Infrastructure.Accounts;

public sealed class AccountSessions(StoreDbContext db, IPasswordHasher<Account> passwords, TimeProvider clock) : IAccountSessions
{
    private static readonly string DummyHash = new PasswordHasher<Account>().HashPassword(new Account(), Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    public async Task<AccountSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var role = request.Surface switch
        {
            "customer" => "CUSTOMER",
            "admin" => "ADMIN",
            _ => throw new BusinessException("INVALID_INPUT", "Bề mặt đăng nhập phải là customer hoặc admin.")
        };
        var username = request.Username?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(username) || username.Length > 100 || string.IsNullOrEmpty(request.Password) || request.Password.Length > 128)
            throw Denied();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result=sys.sp_getapplock @Resource=N'PhoneStore:write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
            IF @result<0 THROW 51000,N'Không thể khóa giao dịch.',1;
            """, cancellationToken);
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
        var verification = passwords.VerifyHashedPassword(account ?? new Account(), account?.PasswordHash ?? DummyHash, request.Password);
        if (account is null || verification == PasswordVerificationResult.Failed || !account.Active || account.Role != role)
            throw Denied();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            account.PasswordHash = passwords.HashPassword(account, request.Password);

        var expiresAt = clock.GetUtcNow().Add(SessionPolicy.Lifetime);
        var session = new LoginSession { Id = Guid.NewGuid(), AccountId = account.Id, ExpiresAt = expiresAt.UtcDateTime };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(new(account.Id, account.Username, account.Role), session.Id, expiresAt);
    }

    public async Task<AccountSession?> ValidateAsync(long accountId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var row = await (from session in db.Sessions.AsNoTracking()
                         join account in db.Accounts.AsNoTracking() on session.AccountId equals account.Id
                         where session.Id == sessionId && session.AccountId == accountId && session.ExpiresAt > now && account.Active
                         select new { account.Id, account.Username, account.Role, session.ExpiresAt })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : new(new(row.Id, row.Username, row.Role), sessionId,
            new DateTimeOffset(DateTime.SpecifyKind(row.ExpiresAt, DateTimeKind.Utc)));
    }

    private static BusinessException Denied() => new("UNAUTHORIZED", "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản không được phép đăng nhập.");
}
