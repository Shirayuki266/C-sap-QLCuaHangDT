using System.Net.Mail;
using System.Text.RegularExpressions;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;

namespace PhoneStore.Infrastructure.Accounts;

internal sealed record ValidatedProfile(string FullName, string Phone, string? Email, string Address);

internal static class ProfileInput
{
    public static ValidatedProfile Validate(ProfileChangeRequest request)
    {
        var name = Text(request.FullName, 120, "Họ tên");
        var phone = Text(request.Phone, 20, "Số điện thoại");
        if (!Regex.IsMatch(phone, @"\A\+?[0-9]{9,15}\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking))
            throw Invalid("Số điện thoại cần 9–15 chữ số và có thể bắt đầu bằng +.");
        var email = Text(request.Email, 254, "Email", optional: true).ToLowerInvariant();
        if (email.Length != 0 && (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email))
            throw Invalid("Email không hợp lệ.");
        return new(name, phone, email.Length == 0 ? null : email, Text(request.Address, 500, "Địa chỉ", optional: true));
    }

    private static string Text(string? input, int maximum, string label, bool optional = false)
    {
        var value = input?.Trim() ?? "";
        if ((!optional && value.Length == 0) || value.Length > maximum) throw Invalid($"{label} không hợp lệ (tối đa {maximum} ký tự).");
        return value;
    }
    private static BusinessException Invalid(string message) => new("INVALID_INPUT", message);
}
