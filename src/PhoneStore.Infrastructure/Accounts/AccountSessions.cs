using System.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Infrastructure.Accounts;

// [UC01.02, UC01.04, UC05.01, UC05.02] Dịch vụ quản lý phiên đăng nhập (tạo, xác thực, thu hồi) trong bảng PHIEN.
public sealed class AccountSessions(StoreDbContext db, IPasswordHasher<Account> passwords, TimeProvider clock) : IAccountSessions
{
    // Băm giả để luôn thực hiện bước VerifyHashedPassword ngay cả khi không tìm thấy tài khoản (chống timing attack).
    private static readonly string DummyHash = new PasswordHasher<Account>().HashPassword(new Account(), Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    // [UC01.02, UC05.01] Xác thực đăng nhập theo bề mặt customer/admin và cấp phiên cố định 8 giờ.
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
        // [UC01.02 - E1 / UC05.01 - E2] Kiểm tra tồn tại, mật khẩu, trạng thái Active và đúng vai trò (CUSTOMER/ADMIN).
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
        var verification = passwords.VerifyHashedPassword(account ?? new Account(), account?.PasswordHash ?? DummyHash, request.Password);
        if (account is null || verification == PasswordVerificationResult.Failed || !account.Active || account.Role != role)
            throw Denied();
        // [UC01.02 - A1] Nếu thuật toán băm cũ cần nâng cấp thì cập nhật PasswordHash mới trước khi commit.
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            account.PasswordHash = passwords.HashPassword(account, request.Password);

        // Tạo phiên có thời hạn cố định SessionPolicy.Lifetime (8 giờ), không gia hạn trượt.
        var expiresAt = clock.GetUtcNow().Add(SessionPolicy.Lifetime);
        var session = new LoginSession { Id = Guid.NewGuid(), AccountId = account.Id, ExpiresAt = expiresAt.UtcDateTime };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(new(account.Id, account.Username, account.Role), session.Id, expiresAt);
    }

    // Kiểm tra cookie phiên còn hiệu lực trong PHIEN và tài khoản vẫn Active ở mọi request cần xác thực.
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

    // [UC01.04, UC05.02] Thu hồi phiên hiện tại khi người dùng đăng xuất (xóa đúng 1 bản ghi PHIEN).
    public async Task RevokeAsync(long accountId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result=sys.sp_getapplock @Resource=N'PhoneStore:write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
            IF @result<0 THROW 51000,N'Không thể khóa giao dịch.',1;
            """, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        if (!await db.Accounts.AsNoTracking().AnyAsync(x => x.Id == accountId && x.Active, cancellationToken) ||
            await db.Sessions.Where(x => x.Id == sessionId && x.AccountId == accountId && x.ExpiresAt > now).ExecuteDeleteAsync(cancellationToken) != 1)
            throw new BusinessException("UNAUTHORIZED", "Phiên đăng nhập đã hết hạn hoặc đã được thu hồi.");
        await transaction.CommitAsync(cancellationToken);
    }

    private static BusinessException Denied() => new("UNAUTHORIZED", "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản không được phép đăng nhập.");
}
