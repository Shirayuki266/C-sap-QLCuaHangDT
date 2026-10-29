using System.Data;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Infrastructure.Accounts;

public sealed class AccountRegistration(StoreDbContext db, IPasswordHasher<Account> passwords) : IAccountRegistration
{
    public async Task<RegistrationResult> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var username = Text(request.Username, 100, "Tên đăng nhập").ToLowerInvariant();
        if (!Regex.IsMatch(username, @"\A[a-z0-9_.@-]{3,100}\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking))
            throw Invalid("Tên đăng nhập cần 3–100 ký tự Latin, số hoặc _ . @ -.");
        var password = request.Password;
        if (password is null || password.Length < 10 || password.Length > 128 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw Invalid("Mật khẩu cần 10–128 ký tự, gồm chữ và số.");
        var name = Text(request.FullName, 120, "Họ tên");
        var phone = Text(request.Phone, 20, "Số điện thoại");
        if (!Regex.IsMatch(phone, @"\A\+?[0-9]{9,15}\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking))
            throw Invalid("Số điện thoại cần 9–15 chữ số và có thể bắt đầu bằng +.");
        var email = Text(request.Email, 254, "Email", optional: true).ToLowerInvariant();
        if (email.Length != 0 && (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email))
            throw Invalid("Email không hợp lệ.");
        var address = Text(request.Address, 500, "Địa chỉ", optional: true);

        var account = new Account { Username = username, Role = "CUSTOMER", Active = true };
        account.PasswordHash = passwords.HashPassword(account, password);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=N'PhoneStore:write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
                IF @result<0 THROW 51000,N'Không thể khóa giao dịch.',1;
                """, cancellationToken);
            db.Accounts.Add(account);
            await db.SaveChangesAsync(cancellationToken);
            db.Customers.Add(new Customer { Id = account.Id, FullName = name, Phone = phone, Email = email.Length == 0 ? null : email, Address = address });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(account.Id, account.Username, account.Role);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw Invalid("Tên đăng nhập, số điện thoại hoặc email đã tồn tại. Vui lòng kiểm tra và nhập lại.");
        }
        catch (SqlException exception) when (exception.Number == 51000)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw new BusinessException("CONCURRENT_UPDATE", "Hệ thống đang xử lý yêu cầu khác. Vui lòng thử lại.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private static string Text(string? input, int maximum, string label, bool optional = false)
    {
        var value = input?.Trim() ?? "";
        if ((!optional && value.Length == 0) || value.Length > maximum)
            throw Invalid($"{label} không hợp lệ (tối đa {maximum} ký tự).");
        return value;
    }

    private static BusinessException Invalid(string message) => new("INVALID_INPUT", message);
}
