using System.Globalization;

namespace PhoneStore.Admin.Client;

public sealed record ServerSettings(string BaseUrl, string? ExpectedEnvironment = null, string? ExpectedFingerprint = null);
public sealed record ReadyInfo(string Status, string Environment, string Fingerprint);
public sealed record AdminIdentity(long Id, string Username, string Role, DateTimeOffset ExpiresAt);
public sealed record PageResult<T>(List<T> Items, int Total, int Page, int PageSize);
public sealed record LoginInput(string Username, string Password, string Surface = "admin");
public sealed record PasswordInput(string CurrentPassword, string NewPassword);
public sealed record EmptyInput;
public sealed record TokenResult(string Token);

public interface IAdminApi : IDisposable
{
    event Action? SessionEnded;
    AdminIdentity? Identity { get; }
    bool NeedsReconciliation { get; }
    Task<ReadyInfo> ReadinessAsync(CancellationToken cancellationToken = default);
    Task<AdminIdentity> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<AdminIdentity?> RecoverSessionAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default);
    Task CommandAsync<T>(string path, T input, CommandIntent intent, CancellationToken cancellationToken = default);
    Task ReconcileAsync(Func<CancellationToken, Task> readAuthoritativeResult, CancellationToken cancellationToken = default);
}

// Kept in memory for the lifetime of one user intention; future screens reuse this instance after reconciliation.
public sealed class CommandIntent
{
    public Guid Key { get; } = Guid.NewGuid();
}

public static class AdminFormatting
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    public static string Money(decimal value) => value.ToString(decimal.Truncate(value) == value ? "N0" : "N2", Vietnamese) + " đ";
    public static string Time(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", Vietnamese);
    public static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static string ListQuery(string? query, int page = 1, int pageSize = 20)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new AdminApiException("INVALID_INPUT", 400);
        return $"?query={Uri.EscapeDataString(query ?? "")}&page={page}&pageSize={pageSize}";
    }
}
