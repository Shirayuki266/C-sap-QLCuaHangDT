using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PhoneStore.Web.Models;

// Presentation validation mirrors DEC.01; existing services validate every command independently.
public sealed class CustomerPasswordAttribute : ValidationAttribute
{
    public const string Policy = "Mật khẩu cần 10–128 ký tự, gồm chữ và số.";
    public CustomerPasswordAttribute() : base(Policy) { }
    public override bool IsValid(object? value) => value is string password && password.Length is >= 10 and <= 128 &&
        password.Any(char.IsLetter) && password.Any(char.IsDigit);
}

public class ProfileFields
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(120, ErrorMessage = "Họ tên tối đa 120 ký tự.")]
    public string? FullName { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại."), RegularExpression(@"\s*\+?[0-9]{9,15}\s*", ErrorMessage = "Số điện thoại cần 9–15 chữ số, có thể bắt đầu bằng +.")]
    public string? Phone { get; set; }
    [StringLength(254, ErrorMessage = "Email tối đa 254 ký tự."), EmailAddress(ErrorMessage = "Vui lòng nhập email hợp lệ.")]
    public string? Email { get; set; }
    [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự.")]
    public string? Address { get; set; }
}

public sealed class ProfileViewModel : ProfileFields
{
    [BindNever] public string? Username { get; set; }
}

public sealed class RegisterViewModel : ProfileFields
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập."), RegularExpression(@"\s*[a-zA-Z0-9_.@-]{3,100}\s*", ErrorMessage = "Tên đăng nhập cần 3–100 ký tự Latin, số hoặc _ . @ -.")]
    public string? Username { get; set; }
    [CustomerPassword, DataType(DataType.Password)] public string? Password { get; set; }
}

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập."), StringLength(100, ErrorMessage = "Tên đăng nhập tối đa 100 ký tự.")]
    public string? Username { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), StringLength(128, ErrorMessage = "Mật khẩu tối đa 128 ký tự."), DataType(DataType.Password)]
    public string? Password { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại."), StringLength(128, ErrorMessage = "Mật khẩu tối đa 128 ký tự."), DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }
    [CustomerPassword, DataType(DataType.Password)] public string? NewPassword { get; set; }
}
