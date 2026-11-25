namespace PhoneStore.Core.Commerce;
// [UC08.02, UC08.03] DTO lập phiếu nhập nháp và xem chi tiết phiếu nhập kho.
public sealed record PurchaseLineInput(long VariantId, int Quantity, decimal Cost);
public sealed record PurchaseInput(long SupplierId, List<PurchaseLineInput>? Lines, string? Version = null, Guid RequestKey = default);
public sealed record PurchaseLineView(long VariantId, string Sku, string Name, int Quantity, decimal Cost, decimal Total);
public sealed record PurchaseView(long Id, string Code, long SupplierId, string Supplier, string Status, decimal Total,
    long CreatorId, long? ConfirmerId, DateTimeOffset CreatedAt, DateTimeOffset? ConfirmedAt, string Version, List<PurchaseLineView> Lines);
