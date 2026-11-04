using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.DemoData.Verification;

public static class BusinessManifest
{
    // Natural keys replace identity IDs; salted hashes, rowversions, sessions and migration time are excluded.
    // Fixed SQL projections and ORDER BY make the hash independent of reset identity counters.
    public static Task<SortedDictionary<string, JsonElement>> Read(StoreDbContext db) =>
        new BusinessKernel(db, TimeProvider.System).Read(async () =>
        {
            var data = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
            (string Name, string Sql)[] projections =
            [
                ("Accounts", "SELECT Username,Role,Active,CreatedAt FROM dbo.TAI_KHOAN ORDER BY Username FOR JSON PATH,INCLUDE_NULL_VALUES"),
                ("Customers", "SELECT a.Username,c.FullName,c.Phone,c.Email,c.Address FROM dbo.KHACH_HANG c JOIN dbo.TAI_KHOAN a ON a.Id=c.Id ORDER BY a.Username FOR JSON PATH,INCLUDE_NULL_VALUES"),
                ("Brands", "SELECT Name,Active FROM dbo.HANG ORDER BY Name FOR JSON PATH"),
                ("Categories", "SELECT Name,Active FROM dbo.DANH_MUC ORDER BY Name FOR JSON PATH"),
                ("Suppliers", "SELECT Code,Name,Phone,Address,Active FROM dbo.NHA_CUNG_CAP ORDER BY Code FOR JSON PATH"),
                ("Products", "SELECT p.Code,p.Name,b.Name Brand,c.Name Category,p.Specifications,p.Description,p.ImageUrl,p.Active FROM dbo.SAN_PHAM p JOIN dbo.HANG b ON b.Id=p.BrandId JOIN dbo.DANH_MUC c ON c.Id=p.CategoryId ORDER BY p.Code FOR JSON PATH"),
                ("Variants", "SELECT v.Sku,p.Code Product,v.Color,v.StorageGb,v.Cost,v.ProfitPercent,v.Price,v.Active,i.OnHand,i.Reserved FROM dbo.BIEN_THE v JOIN dbo.SAN_PHAM p ON p.Id=v.ProductId JOIN dbo.TON_KHO i ON i.VariantId=v.Id ORDER BY v.Sku FOR JSON PATH"),
                ("Images", "SELECT p.Code Product,a.Url,a.SortOrder FROM dbo.SAN_PHAM_ANH a JOIN dbo.SAN_PHAM p ON p.Id=a.ProductId ORDER BY p.Code,a.SortOrder,a.Url FOR JSON PATH"),
                ("Purchases", "SELECT p.Code,s.Code Supplier,a.Username Creator,b.Username Confirmer,p.Status,p.Total,p.CreatedAt,p.ConfirmedAt FROM dbo.PHIEU_NHAP p JOIN dbo.NHA_CUNG_CAP s ON s.Id=p.SupplierId JOIN dbo.TAI_KHOAN a ON a.Id=p.CreatorId LEFT JOIN dbo.TAI_KHOAN b ON b.Id=p.ConfirmerId ORDER BY p.Code FOR JSON PATH,INCLUDE_NULL_VALUES"),
                ("PurchaseLines", "SELECT p.Code Purchase,v.Sku,l.Quantity,l.Cost FROM dbo.PHIEU_NHAP_CT l JOIN dbo.PHIEU_NHAP p ON p.Id=l.PurchaseId JOIN dbo.BIEN_THE v ON v.Id=l.VariantId ORDER BY p.Code,v.Sku FOR JSON PATH"),
                ("Orders", "SELECT o.Code,c.Username Customer,o.Recipient,o.Phone,o.Address,o.Status,o.Total,o.CreatedAt,o.CompletedAt,o.RequestKey FROM dbo.DON_HANG o JOIN dbo.TAI_KHOAN c ON c.Id=o.CustomerId ORDER BY o.Code FOR JSON PATH,INCLUDE_NULL_VALUES"),
                ("OrderLines", "SELECT o.Code [Order],v.Sku Variant,l.Name,l.Sku,l.Description,l.Quantity,l.Price FROM dbo.DON_HANG_CT l JOIN dbo.DON_HANG o ON o.Id=l.OrderId JOIN dbo.BIEN_THE v ON v.Id=l.VariantId ORDER BY o.Code,v.Sku FOR JSON PATH"),
                ("Payments", "SELECT o.Code [Order],p.Method,p.Status,p.Amount,p.CollectedAt,a.Username Actor FROM dbo.THANH_TOAN p JOIN dbo.DON_HANG o ON o.Id=p.OrderId LEFT JOIN dbo.TAI_KHOAN a ON a.Id=p.ActorId ORDER BY o.Code FOR JSON PATH,INCLUDE_NULL_VALUES"),
                ("History", "SELECT o.Code [Order],h.[From],h.[To],a.Username Actor,h.At,h.Reason FROM dbo.LICH_SU_DON h JOIN dbo.DON_HANG o ON o.Id=h.OrderId JOIN dbo.TAI_KHOAN a ON a.Id=h.ActorId ORDER BY o.Code,h.At FOR JSON PATH,INCLUDE_NULL_VALUES"),
                ("Movements", "SELECT v.Sku,a.Username Actor,o.Code [Order],p.Code Purchase,m.Kind,m.DeltaOnHand,m.DeltaReserved,m.At,m.Reason,m.OperationKey FROM dbo.NHAT_KY_KHO m JOIN dbo.BIEN_THE v ON v.Id=m.VariantId JOIN dbo.TAI_KHOAN a ON a.Id=m.ActorId LEFT JOIN dbo.DON_HANG o ON o.Id=m.OrderId LEFT JOIN dbo.PHIEU_NHAP p ON p.Id=m.PurchaseId ORDER BY v.Sku,m.At,m.OperationKey FOR JSON PATH,INCLUDE_NULL_VALUES"),
                ("Carts", "SELECT a.Username Customer,v.Sku,l.Quantity,l.UpdatedAt FROM dbo.GIO_HANG_CT l JOIN dbo.TAI_KHOAN a ON a.Id=l.CustomerId JOIN dbo.BIEN_THE v ON v.Id=l.VariantId ORDER BY a.Username,v.Sku FOR JSON PATH")
            ];
            foreach (var (name, sql) in projections)
            {
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
                command.CommandText = sql;
                var text = new StringBuilder();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) text.Append(reader.GetString(0));
                data.Add(name, JsonDocument.Parse(text.Length == 0 ? "[]" : text.ToString()).RootElement.Clone());
            }
            return data;
        });

    public static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant();
}
