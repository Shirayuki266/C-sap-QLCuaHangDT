using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Infrastructure.Accounts;

public sealed class AccountProfiles(StoreDbContext db, TimeProvider clock) : IAccountProfiles
{
    private async Task<Account> Require(long accountId, Guid sessionId, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId && x.Active, cancellationToken)
            ?? throw new BusinessException("UNAUTHORIZED", "Phiên đăng nhập không hợp lệ.");
        if (account.Role != "CUSTOMER") throw new BusinessException("FORBIDDEN", "Chức năng này chỉ dành cho khách hàng.");
        var now = clock.GetUtcNow().UtcDateTime;
        if (!await db.Sessions.AsNoTracking().AnyAsync(x => x.Id == sessionId && x.AccountId == accountId && x.ExpiresAt > now, cancellationToken))
            throw new BusinessException("UNAUTHORIZED", "Phiên đăng nhập đã hết hạn hoặc đã được thu hồi.");
        return account;
    }

    public async Task<CustomerProfile> ReadAsync(long accountId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var account = await Require(accountId, sessionId, cancellationToken);
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken)
            ?? throw new BusinessException("NOT_FOUND", "Không tìm thấy hồ sơ khách hàng.");
        return new(customer.Id, account.Username, customer.FullName, customer.Phone, customer.Email, customer.Address);
    }

    public async Task UpdateAsync(long accountId, Guid sessionId, ProfileChangeRequest request, CancellationToken cancellationToken = default)
    {
        var input = ProfileInput.Validate(request);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=N'PhoneStore:write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
                IF @result<0 THROW 51000,N'Không thể khóa giao dịch.',1;
                """, cancellationToken);
            await Require(accountId, sessionId, cancellationToken);
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken)
                ?? throw new BusinessException("NOT_FOUND", "Không tìm thấy hồ sơ khách hàng.");
            customer.FullName = input.FullName; customer.Phone = input.Phone; customer.Email = input.Email; customer.Address = input.Address;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(CancellationToken.None); db.ChangeTracker.Clear();
            throw new BusinessException("INVALID_INPUT", "Số điện thoại hoặc email đã tồn tại. Vui lòng kiểm tra và nhập lại.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None); db.ChangeTracker.Clear(); throw;
        }
    }
}
