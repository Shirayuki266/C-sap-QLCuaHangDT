using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Infrastructure.Commerce;

public sealed class BusinessKernel(StoreDbContext db, TimeProvider clock)
{
    public StoreDbContext Db => db;
    public DateTime Now => clock.GetUtcNow().UtcDateTime;
    public async Task<Account> Require(Actor actor, string role, CancellationToken ct = default)
    {
        var now = Now;
        var account = await (from session in db.Sessions.AsNoTracking()
                             join user in db.Accounts.AsNoTracking() on session.AccountId equals user.Id
                             where session.Id == actor.SessionId && user.Id == actor.AccountId && user.Active && session.ExpiresAt > now
                             select user).SingleOrDefaultAsync(ct)
            ?? throw Rules.Error("UNAUTHORIZED", "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
        if (account.Role != role) throw Rules.Error("FORBIDDEN", "Bạn không có quyền thực hiện chức năng này.");
        return account;
    }
    public async Task<T> Write<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            var nested = await action(); await db.SaveChangesAsync(ct); return nested;
        }
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=N'PhoneStore:write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
                IF @result<0 THROW 51000,N'Không thể khóa giao dịch.',1;
                """, ct);
            var result = await action(); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return result;
        }
        catch (Exception error)
        {
            try { await transaction.RollbackAsync(CancellationToken.None); }
            catch (SqlException) { /* A broken connection must not replace the original failure. */ }
            catch (InvalidOperationException) { /* Commit outcome may need request-key reconciliation. */ }
            db.ChangeTracker.Clear();
            if (error is DbUpdateConcurrencyException) throw Rules.Error("CONCURRENT_UPDATE", "Dữ liệu đã thay đổi. Vui lòng tải lại.");
            var sql = error as SqlException ?? error.InnerException as SqlException;
            if (sql?.Number is 2601 or 2627) throw Rules.Error("INVALID_INPUT", "Mã hoặc thông tin duy nhất đã tồn tại.");
            if (sql?.Number is 547 or 8115 || error is OverflowException) throw Rules.Error("INVALID_INPUT", "Dữ liệu vượt giới hạn hoặc vi phạm ràng buộc.");
            if (sql?.Number is 51000 or 1205) throw Rules.Error("CONCURRENT_UPDATE", "Giao dịch đang bận. Vui lòng tải lại trước khi thử lại.");
            throw;
        }
    }
}
