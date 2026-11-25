namespace PhoneStore.Core.Accounts;

// [UC01.03, UC05.02] Hợp đồng đổi mật khẩu tài khoản.
public sealed record PasswordChangeRequest(string? CurrentPassword, string? NewPassword);

public interface IAccountPasswords
{
    Task ChangeAsync(long accountId, Guid sessionId, PasswordChangeRequest request, CancellationToken cancellationToken = default);
}
