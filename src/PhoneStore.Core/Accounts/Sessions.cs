namespace PhoneStore.Core.Accounts;

public sealed record LoginRequest(string? Username, string? Password, string Surface = "customer");
public sealed record AccountIdentity(long Id, string Username, string Role);
public sealed record AccountSession(AccountIdentity Account, Guid SessionId, DateTimeOffset ExpiresAt);

public static class SessionPolicy
{
    // Revised report section 1.5.2.1: fixed eight hours, without sliding renewal.
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);
}

public interface IAccountSessions
{
    Task<AccountSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AccountSession?> ValidateAsync(long accountId, Guid sessionId, CancellationToken cancellationToken = default);
    Task RevokeAsync(long accountId, Guid sessionId, CancellationToken cancellationToken = default);
}
