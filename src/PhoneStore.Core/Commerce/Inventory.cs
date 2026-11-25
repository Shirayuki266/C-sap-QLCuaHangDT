using System.Text.Json.Serialization;
namespace PhoneStore.Core.Commerce;
// [UC08.04, UC08.05] DTO tồn kho hiện tại, nhật ký biến động NHAT_KY_KHO và lệnh kiểm kê điều chỉnh.
public sealed record StockCountInput([property: JsonRequired] int Counted, string? Reason, string? Version, Guid OperationKey);
public sealed record StockView(long VariantId, long ProductId, string Name, string Sku, string Brand, string Color, int StorageGb,
    int OnHand, int Reserved, int Available, string Version);
public sealed record MovementView(long Id, long VariantId, string Sku, string Kind, int DeltaOnHand, int DeltaReserved,
    DateTimeOffset At, long ActorId, string Actor, long? OrderId, long? PurchaseId, string? Reason, Guid OperationKey);
