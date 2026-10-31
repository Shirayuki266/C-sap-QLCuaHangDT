using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using PhoneStore.Core.Commerce;

namespace PhoneStore.Web.Models;

public sealed class CheckoutViewModel
{
    public Guid RequestKey { get; set; }
    [Required(ErrorMessage = "Phiên xác nhận không hợp lệ. Vui lòng tải lại trang đặt hàng.")]
    public string? Confirmation { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận."), StringLength(120, ErrorMessage = "Tên người nhận tối đa 120 ký tự.")]
    public string? Recipient { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại."), RegularExpression(@"\s*\+?[0-9]{9,15}\s*", ErrorMessage = "Số điện thoại cần 9–15 chữ số, có thể bắt đầu bằng +.")]
    public string? Phone { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng."), StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự.")]
    public string? Address { get; set; }
    [Required(ErrorMessage = "Chỉ hỗ trợ thanh toán COD."), RegularExpression("^COD$", ErrorMessage = "Chỉ hỗ trợ thanh toán COD.")]
    public string? PaymentMethod { get; set; }
    [BindNever, ValidateNever] public CartView Cart { get; set; } = new([], 0, false);
    [BindNever] public bool PriceChanged { get; set; }
}
