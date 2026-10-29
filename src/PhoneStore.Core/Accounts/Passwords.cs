namespace PhoneStore.Core.Accounts;

public sealed record PasswordChangeRequest(string? CurrentPassword, string? NewPassword);

public interface IAccountPasswords
{
    Task ChangeAsync(long accountId, Guid sessionId, PasswordChangeRequest request, CancellationToken cancellationToken = default);
}
