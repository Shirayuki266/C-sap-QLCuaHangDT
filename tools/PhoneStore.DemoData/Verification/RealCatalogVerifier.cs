using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.DemoData.Verification;

// Independent joins against frozen manufacturer facts, without calling plan generation.
public static class RealCatalogVerifier
{
    public static async Task<Dictionary<string, long>> Run(StoreDbContext db, string scale, string directory)
    {
        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        using var stream = typeof(RealCatalogVerifier).Assembly.GetManifestResourceStream("PhoneStore.DemoData.RealCatalog.json")
            ?? throw new InvalidOperationException("Thiếu thông số nguồn.");
        using var reader = new StreamReader(stream);
        var source = await reader.ReadToEndAsync();
        var count = scale == "full" ? 50 : 6;
        foreach (var (label, sql) in Checks)
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = Facts + "\n" + sql;
            command.Parameters.Add(new SqlParameter("@catalog", source));
            command.Parameters.Add(new SqlParameter("@count", count));
            result[label] = Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        long invalidFiles = 0;
        using var facts = JsonDocument.Parse(source);
        foreach (var product in facts.RootElement.EnumerateArray().Take(count))
        foreach (var photo in product.GetProperty("photos").EnumerateArray())
        {
            var file = photo.GetProperty("file").GetString()!;
            var physical = Path.Combine(directory, file);
            if (Path.GetFileName(file) != file || !File.Exists(physical) ||
                !Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(physical)))
                    .Equals(photo.GetProperty("wrapperSha256").GetString(), StringComparison.OrdinalIgnoreCase)) invalidFiles++;
        }
        result["RealPhotoDigestMismatches"] = invalidFiles;
        return result;
    }

    private const string Facts = """
        WITH facts AS (
          SELECT f.* FROM OPENJSON(@catalog) j CROSS APPLY OPENJSON(j.value) WITH(
            Code nvarchar(30) '$.code',Name nvarchar(200) '$.name',Brand nvarchar(200) '$.brand',
            Display nvarchar(500) '$.display',Chip nvarchar(500) '$.chip',Ram nvarchar(500) '$.ram',
            Camera nvarchar(500) '$.camera',Battery nvarchar(500) '$.battery',Charging nvarchar(500) '$.charging',
            Os nvarchar(500) '$.os',Network nvarchar(500) '$.network',
            Storage nvarchar(max) '$.storage' AS JSON,Colors nvarchar(max) '$.colors' AS JSON,Photos nvarchar(max) '$.photos' AS JSON
          ) f WHERE CONVERT(int,j.[key])<@count
        )
        """;
    private static readonly (string Label, string Sql)[] Checks =
    [
        ("RealProductMismatches", """
          SELECT COUNT_BIG(*) FROM dbo.SAN_PHAM p FULL JOIN facts f ON f.Code=p.Code
          LEFT JOIN dbo.HANG b ON b.Id=p.BrandId
          WHERE p.Code IS NULL OR f.Code IS NULL OR p.Name<>f.Name OR b.Name<>f.Brand
            OR CHARINDEX(f.Name,p.Description)=0
            OR CHARINDEX(f.Display,p.Specifications)=0 OR CHARINDEX(f.Chip,p.Specifications)=0
            OR CHARINDEX(f.Ram,p.Specifications)=0 OR CHARINDEX(f.Camera,p.Specifications)=0
            OR CHARINDEX(f.Battery,p.Specifications)=0 OR CHARINDEX(f.Charging,p.Specifications)=0
            OR CHARINDEX(f.Os,p.Specifications)=0 OR CHARINDEX(f.Network,p.Specifications)=0
          """),
        ("RealVariantMismatches", """
          SELECT COUNT_BIG(*) FROM dbo.BIEN_THE v JOIN dbo.SAN_PHAM p ON p.Id=v.ProductId
          LEFT JOIN facts f ON f.Code=p.Code WHERE f.Code IS NULL
            OR NOT EXISTS(SELECT 1 FROM OPENJSON(f.Storage) s WHERE TRY_CONVERT(int,s.value)=v.StorageGb)
            OR NOT EXISTS(SELECT 1 FROM OPENJSON(f.Colors) c WHERE c.value=v.Color)
          """),
        ("RealPhotoMismatches", """
          SELECT (SELECT COUNT_BIG(*) FROM dbo.SAN_PHAM_ANH a JOIN dbo.SAN_PHAM p ON p.Id=a.ProductId LEFT JOIN facts f ON f.Code=p.Code
            WHERE f.Code IS NULL OR NOT EXISTS(SELECT 1 FROM OPENJSON(f.Photos) j
              WHERE CONVERT(int,j.[key])=a.SortOrder AND a.Url=N'/images/demo/'+JSON_VALUE(j.value,'$.file')))
            +(SELECT COUNT_BIG(*) FROM dbo.SAN_PHAM p JOIN facts f ON f.Code=p.Code WHERE p.ImageUrl<>N'/images/demo/'+JSON_VALUE(f.Photos,'$[0].file'))
          """),
        ("RealSnapshotMismatches", """
          SELECT COUNT_BIG(*) FROM dbo.DON_HANG_CT l JOIN dbo.BIEN_THE v ON v.Id=l.VariantId
          JOIN dbo.SAN_PHAM p ON p.Id=v.ProductId LEFT JOIN facts f ON f.Code=p.Code
          WHERE f.Code IS NULL OR l.Name<>f.Name OR l.Sku<>v.Sku
          """)
    ];
}
