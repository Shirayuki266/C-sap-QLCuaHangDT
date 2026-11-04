using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.DemoData.Verification;

// Independent persisted-data checks: no dependency on Generation or its arithmetic.
// D2 will add the complete temporal/order verifier; these are D1 foundation checks only.
public static class FoundationVerifier
{
    public static async Task Verify(StoreDbContext db, string scale, string imagesDirectory)
    {
        var full = scale == "full";
        if (await db.Products.CountAsync() != (full ? 50 : 6) ||
            await db.Variants.CountAsync() != (full ? 125 : 15) ||
            await db.Customers.CountAsync() != (full ? 60 : 6) ||
            await db.Accounts.CountAsync() != (full ? 61 : 7) ||
            await db.Brands.CountAsync() != 10 || await db.Categories.CountAsync() != 6 ||
            await db.Suppliers.CountAsync() != (full ? 12 : 3) ||
            await db.Sessions.AnyAsync() || await db.Accounts.CountAsync(x => !x.Active) != (full ? 4 : 1))
            throw new InvalidOperationException("Sai quy mô dữ liệu nền.");
        string[] queries =
        [
            "SELECT COUNT(*) AS [Value] FROM dbo.BIEN_THE WHERE Price<>ROUND(Cost*(1+ProfitPercent/100),2) OR Cost<=0 OR ProfitPercent NOT BETWEEN 0 AND 10000",
            "SELECT COUNT(*) AS [Value] FROM dbo.SAN_PHAM WHERE LEN(Name)=0 OR LEN(Name)>200 OR LEN(Description)=0 OR DATALENGTH(Description)>100000 OR LEN(Specifications)=0 OR DATALENGTH(Specifications)>40000 OR LEN(ImageUrl)>1000",
            "SELECT COUNT(*) AS [Value] FROM (SELECT Description FROM dbo.SAN_PHAM GROUP BY Description HAVING COUNT(*)>1) d",
            "SELECT COUNT(*) AS [Value] FROM dbo.KHACH_HANG WHERE LEN(FullName)>120 OR LEN(Address)>500 OR Email='' OR (Email IS NOT NULL AND Email NOT LIKE '%@example.com')",
            "SELECT COUNT(*) AS [Value] FROM dbo.TAI_KHOAN WHERE Username COLLATE Latin1_General_100_BIN2<>LOWER(Username) COLLATE Latin1_General_100_BIN2 OR CreatedAt>SYSUTCDATETIME()",
            "SELECT COUNT(*) AS [Value] FROM dbo.SAN_PHAM p WHERE (SELECT COUNT(*) FROM dbo.SAN_PHAM_ANH a WHERE a.ProductId=p.Id)<>3 OR NOT EXISTS(SELECT 1 FROM dbo.SAN_PHAM_ANH a WHERE a.ProductId=p.Id AND a.SortOrder=0 AND a.Url=p.ImageUrl)",
            "SELECT COUNT(*) AS [Value] FROM dbo.SAN_PHAM_ANH WHERE SortOrder NOT BETWEEN 0 AND 2"
        ];
        foreach (var sql in queries)
            if (await db.Database.SqlQueryRaw<int>(sql).SingleAsync() != 0)
                throw new InvalidOperationException("Dữ liệu nền vi phạm kiểm chứng SQL độc lập.");
        if (full)
        {
            var expected = new Dictionary<string, int> { ["Giá rẻ"] = 14, ["Tầm trung"] = 16, ["Cao cấp"] = 12, ["Gaming"] = 4, ["Màn hình gập"] = 4 };
            foreach (var (name, count) in expected)
                if (await (from p in db.Products join c in db.Categories on p.CategoryId equals c.Id where c.Name == name select p).CountAsync() != count)
                    throw new InvalidOperationException("Sai phân bổ phân khúc.");
        }
        var urls = await db.ProductImages.Select(x => x.Url).ToListAsync();
        foreach (var url in urls)
        {
            if (!url.StartsWith("/images/demo/", StringComparison.Ordinal) || Path.GetFileName(url) != url[13..] ||
                !File.Exists(Path.Combine(imagesDirectory, Path.GetFileName(url))))
                throw new InvalidOperationException("Ảnh dữ liệu nền thiếu tệp hoặc sai đường dẫn.");
        }
        var audit = await new DataAudit(db).Run();
        if (!audit.Valid) throw new InvalidOperationException("DataAudit từ chối dữ liệu nền.");
    }
}
