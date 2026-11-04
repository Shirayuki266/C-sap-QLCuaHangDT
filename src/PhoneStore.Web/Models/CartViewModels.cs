using System.ComponentModel.DataAnnotations;
using PhoneStore.Core.Commerce;

namespace PhoneStore.Web.Models;

public sealed class CartChangeForm
{
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn sản phẩm hợp lệ.")]
    public long ProductId { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn phiên bản hợp lệ.")]
    public long VariantId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải là số nguyên dương.")]
    public int Quantity { get; set; }
}

public sealed class CartQuantityForm
{
    [Range(1, long.MaxValue)] public long VariantId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải là số nguyên dương.")] public int Quantity { get; set; }
}

public sealed class CartQuoteForm
{
    [Required, MaxLength(100)] public List<CartInput>? Lines { get; set; }
}

public sealed record CartPageViewModel(CartView Cart, bool Guest, long? ErrorVariant = null, string? AttemptedQuantity = null);
