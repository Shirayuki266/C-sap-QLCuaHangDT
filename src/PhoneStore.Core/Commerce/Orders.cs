using System.Text.Json.Serialization;
namespace PhoneStore.Core.Commerce;
public sealed record PriceConfirmation([property:JsonRequired] long VariantId,[property:JsonRequired] int Quantity,[property:JsonRequired] decimal Price);
public sealed record CheckoutInput(Guid RequestKey,string? Recipient,string? Phone,string? Address,List<PriceConfirmation>? Lines,string PaymentMethod="COD");
public sealed class PriceChangedException(CartView cart):Exception("Giỏ hàng hoặc giá đã thay đổi. Vui lòng kiểm tra và xác nhận lại.")
{
    public CartView Cart {get;}=cart;
}
public sealed record OrderLineView(long VariantId,string Name,string Sku,string Description,int Quantity,decimal Price,decimal Total);
public sealed record OrderHistoryView(long Id,string? From,string To,long ActorId,string Actor,DateTimeOffset At,string? Reason);
public sealed record OrderView(long Id,string Code,long CustomerId,string Recipient,string Phone,string Address,string Status,string Payment,
    decimal Total,DateTimeOffset CreatedAt,DateTimeOffset? CompletedAt,Guid RequestKey,string Version,bool ReturnReceived,List<OrderLineView> Lines,List<OrderHistoryView> History);
