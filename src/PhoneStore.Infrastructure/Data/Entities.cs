namespace PhoneStore.Infrastructure.Data;

public sealed class Account
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "CUSTOMER";
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class Customer
{
    public long Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string Address { get; set; } = "";
}
public sealed class LoginSession
{
    public Guid Id { get; set; }
    public long AccountId { get; set; }
    public DateTime ExpiresAt { get; set; }
}
public sealed class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
}
public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
}
public sealed class Product
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int BrandId { get; set; }
    public int CategoryId { get; set; }
    public string Specifications { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public bool Active { get; set; } = true;
}
public sealed class ProductImage
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Url { get; set; } = "";
    public int SortOrder { get; set; }
}
public sealed class Variant
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Sku { get; set; } = "";
    public string Color { get; set; } = "";
    public int StorageGb { get; set; }
    public decimal Cost { get; set; }
    public decimal ProfitPercent { get; set; }
    public decimal Price { get; set; }
    public bool Active { get; set; } = true;
    public byte[] Version { get; set; } = [];
}
public sealed class Inventory
{
    public long VariantId { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public byte[] Version { get; set; } = [];
}
public sealed class CartLine
{
    public long CustomerId { get; set; }
    public long VariantId { get; set; }
    public int Quantity { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class Order
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public long CustomerId { get; set; }
    public string Recipient { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string Status { get; set; } = "CHO_XAC_NHAN";
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid RequestKey { get; set; }
    public byte[] Version { get; set; } = [];
}
public sealed class OrderLine
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long VariantId { get; set; }
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
public sealed class Payment
{
    public long OrderId { get; set; }
    public string Method { get; set; } = "COD";
    public string Status { get; set; } = "CHUA_THU";
    public decimal Amount { get; set; }
    public DateTime? CollectedAt { get; set; }
    public long? ActorId { get; set; }
}
public sealed class OrderHistory
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string? From { get; set; }
    public string To { get; set; } = "";
    public long ActorId { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
public sealed class Supplier
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public bool Active { get; set; } = true;
}
public sealed class Purchase
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public long SupplierId { get; set; }
    public long CreatorId { get; set; }
    public long? ConfirmerId { get; set; }
    public string Status { get; set; } = "NHAP";
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public byte[] Version { get; set; } = [];
}
public sealed class PurchaseLine
{
    public long Id { get; set; }
    public long PurchaseId { get; set; }
    public long VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal Cost { get; set; }
}
public sealed class StockMovement
{
    public long Id { get; set; }
    public long VariantId { get; set; }
    public long ActorId { get; set; }
    public long? OrderId { get; set; }
    public long? PurchaseId { get; set; }
    public string Kind { get; set; } = "";
    public int DeltaOnHand { get; set; }
    public int DeltaReserved { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public Guid OperationKey { get; set; }
}
