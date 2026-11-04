using PhoneStore.Core;

namespace PhoneStore.Infrastructure.Accounts;

internal static class PasswordInput
{
    public static string Validate(string? password)
    {
        if (password is null || password.Length < 10 || password.Length > 128 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new BusinessException("INVALID_INPUT", "Mật khẩu cần 10–128 ký tự, gồm chữ và số.");
        return password;
    }
}
