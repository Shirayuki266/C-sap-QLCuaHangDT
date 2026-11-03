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
                ("Images", "SELECT p.Code Product,a.Url,a.SortOrder FROM dbo.SAN_PHAM_ANH a JOIN dbo.SAN_PHAM p ON p.Id=a.ProductId ORDER BY p.Code,a.SortOrder,a.Url FOR JSON PATH")
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
