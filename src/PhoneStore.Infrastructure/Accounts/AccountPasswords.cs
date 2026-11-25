using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Infrastructure.Accounts;

// [UC01.03, UC05.02] Dịch vụ đổi mật khẩu cho cả Khách hàng và Quản trị viên.
public sealed class AccountPasswords(StoreDbContext db, IPasswordHasher<Account> passwords, TimeProvider clock) : IAccountPasswords
{
    // [UC01.03, UC05.02] Xác minh mật khẩu cũ, cập nhật PasswordHash mới và xóa toàn bộ phiên của tài khoản.
    public async Task ChangeAsync(long accountId, Guid sessionId, PasswordChangeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.CurrentPassword) || request.CurrentPassword.Length > 128)
            throw new BusinessException("INVALID_INPUT", "Vui lòng nhập mật khẩu hiện tại hợp lệ.");
        var replacement = PasswordInput.Validate(request.NewPassword);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=N'PhoneStore:write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
                IF @result<0 THROW 51000,N'Không thể khóa giao dịch.',1;
                """, cancellationToken);
            var now = clock.GetUtcNow().UtcDateTime;
            var account = await db.Accounts.SingleOrDefaultAsync(x => x.Id == accountId && x.Active, cancellationToken);
            if (account is null || !await db.Sessions.AnyAsync(x => x.Id == sessionId && x.AccountId == accountId && x.ExpiresAt > now, cancellationToken))
                throw new BusinessException("UNAUTHORIZED", "Phiên đăng nhập đã hết hạn hoặc đã được thu hồi.");
            // [UC01.03 - E1 / UC05.02 - E1] Từ chối nếu mật khẩu hiện tại không khớp.
            if (passwords.VerifyHashedPassword(account, account.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
                throw new BusinessException("INVALID_INPUT", "Mật khẩu hiện tại không đúng.");
            // Lưu mật khẩu băm mới và thu hồi toàn bộ phiên đăng nhập hiện có trong PHIEN.
            account.PasswordHash = passwords.HashPassword(account, replacement);
            await db.Sessions.Where(x => x.AccountId == accountId).ExecuteDeleteAsync(cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }
}
