namespace PhoneStore.Core.Accounts;

// [UC01.01] Hợp đồng yêu cầu và kết quả đăng ký tài khoản CUSTOMER mới.
public sealed record RegistrationRequest(string? Username, string? Password, string? FullName, string? Phone,
    string? Email = null, string? Address = null);

public sealed record RegistrationResult(long Id, string Username, string Role);

public interface IAccountRegistration
{
    Task<RegistrationResult> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken = default);
}
