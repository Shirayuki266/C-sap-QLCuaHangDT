using System.Text.Json.Serialization;
namespace PhoneStore.Core.Commerce;
public sealed record CartInput([property:JsonRequired] long VariantId,[property:JsonRequired] int Quantity);
public sealed record CartLineView(long VariantId,string? Name,string? Sku,string? Color,int? StorageGb,int Quantity,decimal? Price,int Available,bool Sellable,string? Issue);
public sealed record CartView(List<CartLineView> Lines,decimal Total,bool HasIssues);
public sealed record CartAdjustment(long VariantId,int RequestedQuantity,int Quantity,string Reason);
public sealed record CartMergeResult(CartView Cart,List<CartAdjustment> Adjusted);
