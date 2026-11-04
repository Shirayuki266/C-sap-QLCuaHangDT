namespace PhoneStore.Core.Accounts;

public sealed record ProfileChangeRequest(string? FullName, string? Phone, string? Email = null, string? Address = null);
public sealed record CustomerProfile(long Id, string Username, string FullName, string Phone, string? Email, string Address);

public interface IAccountProfiles
{
    Task<CustomerProfile> ReadAsync(long accountId, Guid sessionId, CancellationToken cancellationToken = default);
    Task UpdateAsync(long accountId, Guid sessionId, ProfileChangeRequest request, CancellationToken cancellationToken = default);
}
