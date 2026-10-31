using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.WebUtilities;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;

namespace PhoneStore.Web.Models;

public sealed class OrderQuery
{
    [StringLength(100, ErrorMessage = "Từ khóa tối đa 100 ký tự.")] public string? Code { get; set; }
    public string? Status { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Trang cần là số nguyên dương.")] public int Page { get; set; } = 1;
    [Range(1, 100, ErrorMessage = "Mỗi trang cần từ 1 đến 100 đơn.")] public int PageSize { get; set; } = 10;
    public string PageUrl(int page) => QueryHelpers.AddQueryString("/Orders/Index", new Dictionary<string,string?> {
        ["code"] = Code, ["status"] = Status, ["from"] = From, ["to"] = To,
        ["page"] = page.ToString(CultureInfo.InvariantCulture), ["pageSize"] = PageSize.ToString(CultureInfo.InvariantCulture) });
}
public sealed record OrderListViewModel(OrderQuery Filter, Page<CustomerOrderSummary>? Results)
{
    public long TotalPages => Results is null ? 0 : ((long)Results.Total + Results.PageSize - 1) / Results.PageSize;
}
public class CancelOrderForm
{
    public long Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập lý do hủy đơn."), StringLength(1000, ErrorMessage = "Lý do tối đa 1000 ký tự.")]
    public string? Reason { get; set; }
    [Required(ErrorMessage = "Thiếu phiên bản đơn. Vui lòng tải lại trang.")]
    public string? Version { get; set; }
}
public sealed class OrderDetailsViewModel : CancelOrderForm
{
    [BindNever, ValidateNever] public CustomerOrderSnapshot Order { get; set; } = null!;
}
