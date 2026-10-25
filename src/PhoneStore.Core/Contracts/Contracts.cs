using System.ComponentModel.DataAnnotations;
namespace PhoneStore.Core.Contracts;
public sealed record LoginRequest([Required(ErrorMessage="Vui lòng nhập tên đăng nhập.")] string Username, [Required(ErrorMessage="Vui lòng nhập mật khẩu.")] string Password);
public sealed record RegisterRequest([Required(ErrorMessage="Vui lòng nhập tên đăng nhập.")] string Username, [Required(ErrorMessage="Vui lòng nhập mật khẩu.")] string Password, [Required(ErrorMessage="Vui lòng nhập họ tên.")] string FullName, [Required(ErrorMessage="Vui lòng nhập số điện thoại.")] string Phone, string? Email, string? Address);
public sealed record ProfileRequest([Required(ErrorMessage="Vui lòng nhập họ tên.")] string FullName, [Required(ErrorMessage="Vui lòng nhập số điện thoại.")] string Phone, string? Email, string? Address);
public sealed record PasswordRequest([Required(ErrorMessage="Vui lòng nhập mật khẩu hiện tại.")] string CurrentPassword, [Required(ErrorMessage="Vui lòng nhập mật khẩu mới.")] string NewPassword);
public sealed record UserDto(long Id, string Username, string Role, [Required(ErrorMessage="Vui lòng nhập họ tên.")] string FullName, [Required(ErrorMessage="Vui lòng nhập số điện thoại.")] string Phone, string? Email, string? Address);
public sealed record LookupDto(int Id, string Name, bool Active);
public sealed record LookupRequest(string Name, bool Active);
public sealed record VariantInput(long Id, string Sku, string Color, int StorageGb, decimal Price, bool Active, string? Version = null);
public sealed record ProductImageInput(string Url,int SortOrder);
public sealed record ProductInput(string Code, string Name, int BrandId, int CategoryId, string Specifications, string Description, string? ImageUrl, bool Active, List<VariantInput> Variants,List<ProductImageInput>? Images=null);
public sealed record ProductDto(long Id, string Code, string Name, int BrandId, int CategoryId, string Brand, string Category, string Specifications, string Description, string? ImageUrl, bool Active, List<VariantDto> Variants,List<ProductImageInput>? Images=null);
public sealed record VariantDto(long Id, string Sku, string Color, int StorageGb, decimal Price, bool Active, int OnHand, int Reserved, int Available, string Version);
public sealed record Page<T>(List<T> Items, int Total, int PageNumber, int PageSize);
public sealed record CatalogFilter(string? Query = null, int? BrandId = null, decimal? MinPrice = null, decimal? MaxPrice = null, int? StorageGb = null, string? Sort = null, int Page = 1, int PageSize = 12, string? Color = null);
public sealed record SearchSuggestion(string Label,string Kind,string Url);
public sealed record PublicVariantDto(long Id,string Sku,string Color,int StorageGb,decimal Price,bool Active,int Available);
public sealed record PublicProductDto(long Id,string Code,string Name,int BrandId,int CategoryId,string Brand,string Category,string Specifications,string Description,string? ImageUrl,bool Active,List<PublicVariantDto> Variants,List<ProductImageInput>? Images)
{
    public static PublicProductDto From(ProductDto p)=>new(p.Id,p.Code,p.Name,p.BrandId,p.CategoryId,p.Brand,p.Category,p.Specifications,p.Description,p.ImageUrl,p.Active,p.Variants.Select(v=>new PublicVariantDto(v.Id,v.Sku,v.Color,v.StorageGb,v.Price,v.Active,v.Available)).ToList(),p.Images);
}
public sealed record CartInput(long VariantId, int Quantity);
public sealed record CartLineDto(long VariantId, string Name, string Sku, string Color, int StorageGb, decimal Price, int Quantity, int Available, bool Active)
{ public decimal Total => Price * Quantity; }
public sealed record CartDto(List<CartLineDto> Lines) { public decimal Total => Lines.Sum(x => x.Total); }
public sealed record PriceConfirmation(long VariantId, int Quantity, decimal Price);
public sealed record CheckoutRequest(Guid RequestKey, string Recipient, string Phone, string Address, List<PriceConfirmation> Lines, string PaymentMethod="COD");
public sealed record TransitionRequest(string Version, Guid OperationKey, string? Reason = null);
public sealed record OrderLineDto(long VariantId, string Name, string Sku, string Variant, int Quantity, decimal Price) { public decimal Total => Quantity * Price; }
public sealed record HistoryDto(string? From, string To, DateTime At, string Actor, string? Reason);
public sealed record OrderDto(long Id, string Code, long CustomerId, string Customer, string Recipient, string Phone, string Address, string Status, string Payment, decimal Total, DateTime CreatedAt, DateTime? CompletedAt, bool ReturnReceived, string Version, List<OrderLineDto> Lines, List<HistoryDto> History);
public sealed record CustomerDto(long Id, string Username, string FullName, string Phone, string? Email, string? Address, bool Active);
public sealed record CustomerInput(string FullName, string Phone, string? Email, string? Address, bool Active);
public sealed record SupplierDto(long Id, string Code, string Name, string? Phone, string? Address, bool Active);
public sealed record SupplierInput(string Code, string Name, string? Phone, string? Address, bool Active);
public sealed record PurchaseLineInput(long VariantId, int Quantity, decimal Cost);
public sealed record PurchaseInput(long SupplierId, List<PurchaseLineInput> Lines, string? Version = null);
public sealed record PurchaseLineDto(long VariantId, string Sku, string Name, int Quantity, decimal Cost) { public decimal Total => Quantity * Cost; }
public sealed record PurchaseDto(long Id, string Code, long SupplierId, string Supplier, string Status, decimal Total, DateTime CreatedAt, DateTime? ConfirmedAt, string Creator, string? Confirmer, string Version, List<PurchaseLineDto> Lines);
public sealed record StockDto(long VariantId, string Sku, string Name, string Brand, string Color, int StorageGb, int OnHand, int Reserved, int Available, string Version);
public sealed record AdjustmentInput(int Counted, string Reason, string Version, Guid OperationKey);
public sealed record MovementDto(long Id, long VariantId, string Sku, string Kind, int DeltaOnHand, int DeltaReserved, DateTime At, string Actor, string? Document, string? Reason);
public sealed record ReportRow(string Label, int Count, decimal Value, int? OnHand = null, int? Reserved = null, int? Available = null);
public sealed record ReportResult(string Kind, List<ReportRow> Rows) { public decimal Total => Rows.Sum(x => x.Value); public int Count => Rows.Sum(x => x.Count); }
public static class Labels
{
    public static string Status(string status) => status switch { "CHO_XAC_NHAN" => "Chờ xác nhận", "DA_XAC_NHAN" => "Đã xác nhận", "DANG_GIAO" => "Đang giao", "HOAN_TAT" => "Hoàn tất", "DA_HUY" => "Đã hủy", "GIAO_THAT_BAI" => "Giao thất bại", "NHAP" => "Nháp", "DA_THU" => "Đã thu COD", "CHUA_THU" => "Chưa thu COD", "GIU" => "Giữ hàng", "HUY_GIU" => "Giải phóng giữ", "XUAT_GIAO" => "Xuất giao", "NHAN_LAI" => "Nhận lại", "DIEU_CHINH" => "Kiểm kê", _ => status };
    public static DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(7);
}



