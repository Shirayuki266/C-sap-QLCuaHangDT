using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PhoneStore.DemoData.Contracts;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.DemoData.Verification;

public sealed record QuarterSummary(int Year, int Quarter, int Target, int Orders, decimal Revenue);
public sealed record VerificationReport(bool Valid, Dictionary<string, int> Counts, List<QuarterSummary> Quarters,
    Dictionary<string, int> Statuses, Dictionary<string, long> Metrics, List<string> Errors);

// This verifier never calls Generation, reproduces its in-memory balances, or writes SQL data.
public static class DatasetVerifier
{
    public static Task<VerificationReport> Run(StoreDbContext db, DatasetContext context, string imagesDirectory) =>
        new BusinessKernel(db, TimeProvider.System).Read(async () =>
        {
            var errors = new List<string>();
            var audit = await new DataAudit(db).Run();
            errors.AddRange(audit.Errors);
            var full = context.Scale == "full";
            if (context.Scale is not ("full" or "small") || context.Cutoff > DateTime.UtcNow ||
                context.Cutoff != context.AsOf.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7) ||
                context.Products.Length != (full ? 50 : 6) || context.Products.Select(x => x.Code).Distinct().Count() != context.Products.Length)
                errors.Add("Invalid verification metadata");
            try { await FoundationVerifier.Verify(db, context.Scale, imagesDirectory); }
            catch (InvalidOperationException) { errors.Add("Catalog/accounts/images foundation"); }

            async Task<T> Query<T>(string sql, Func<System.Data.Common.DbDataReader, T> read)
            {
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
                command.CommandText = sql;
                command.Parameters.Add(new SqlParameter("@cutoff", context.Cutoff));
                command.Parameters.Add(new SqlParameter("@start", context.Start));
                command.Parameters.Add(new SqlParameter("@products", JsonSerializer.Serialize(context.Products)));
                command.Parameters.Add(new SqlParameter("@full", full));
                await using var reader = await command.ExecuteReaderAsync();
                return read(reader);
            }
            foreach (var (label, sql) in Checks)
                if (await Query(sql, reader => { reader.Read(); return Convert.ToInt64(reader.GetValue(0)); }) != 0) errors.Add(label);

            var quarters = await Query(QuarterSql, reader =>
            {
                var rows = new List<QuarterSummary>();
                while (reader.Read()) rows.Add(new(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetDecimal(4)));
                return rows;
            });
            if (quarters.Count != 9 || quarters.Any(x => x.Orders < Math.Ceiling(x.Target * .85m) || x.Orders > Math.Floor(x.Target * 1.15m) || x.Revenue <= 0))
                errors.Add("Quarter counts/revenue");
            if (full)
            {
                var ranges = new Dictionary<string, (int Min, int Max)>
                {
                    ["TAI_KHOAN"]=(60,62),["KHACH_HANG"]=(50,70),["PHIEN"]=(0,0),["HANG"]=(8,12),["DANH_MUC"]=(5,8),
                    ["SAN_PHAM"]=(45,55),["SAN_PHAM_ANH"]=(100,180),["BIEN_THE"]=(110,140),["TON_KHO"]=(125,125),
                    ["GIO_HANG_CT"]=(20,30),["NHA_CUNG_CAP"]=(10,15),["PHIEU_NHAP"]=(56,62),["PHIEU_NHAP_CT"]=(200,320),
                    ["DON_HANG"]=(540,660),["DON_HANG_CT"]=(700,950),["THANH_TOAN"]=(540,660),["LICH_SU_DON"]=(1600,2400),
                    ["NHAT_KY_KHO"]=(1500,2600),["__PhoneStoreMigrations"]=(1,1)
                };
                foreach (var (table, range) in ranges)
                    if (!audit.Counts.TryGetValue(table, out var count) || count < range.Min || count > range.Max) errors.Add("Scale: " + table);
            }
            var metrics = await Query(MetricsSql, reader =>
            {
                var result = new Dictionary<string, long>(StringComparer.Ordinal);
                while (reader.Read()) result[reader.GetString(0)] = reader.GetInt64(1);
                return result;
            });
            if (metrics["RecentOrders"] < (full ? 20 : 6)) errors.Add("Recent 30-day orders");
            if (metrics["CurrentMonthEligibleProducts"] < (full ? 8 : 5)) errors.Add("Current-month eligible completed products");
            if (metrics["LowStockVariants"] < (full ? 8 : 3) || metrics["OutOfStockVariants"] < (full ? 3 : 1)) errors.Add("Low/out-of-stock examples");
            if (full && metrics["TypicalStockVariants"] < 63) errors.Add("Most variants retain normal stock");
            if (metrics["FailedWithReturn"] * 2 <= metrics["FailedOrders"]) errors.Add("Most failed deliveries physically returned");
            if (metrics["Adjustments"] is < 6 or > 10) errors.Add("Physical counts/reasons");
            if (metrics["NoOrderCustomers"] != 2 || (full && (metrics["Repeat5To10Customers"] == 0 || metrics["OneOrderCustomers"] < 10))) errors.Add("Customer purchase distribution");
            if (metrics["EmptyDrafts"] == 0 || metrics["Drafts"] == 0 || metrics["CancelledPurchases"] == 0 || metrics["InactiveSuppliers"] == 0) errors.Add("Purchase scenarios");
            if (metrics["InactiveProductsWithHistory"] != (full ? 4 : 1)) errors.Add("Retired products retain history");
            if (metrics["CartCustomers"] != (full ? 12 : 3)) errors.Add("Active cart customers");
            var statuses = await Query("SELECT Status,COUNT(*) FROM dbo.DON_HANG GROUP BY Status ORDER BY Status", reader =>
            {
                var result = new Dictionary<string, int>(StringComparer.Ordinal);
                while (reader.Read()) result[reader.GetString(0)] = reader.GetInt32(1);
                return result;
            });
            if (full && statuses.GetValueOrDefault("HOAN_TAT") < audit.Counts["DON_HANG"] * .80m) errors.Add("Completed-order proportion");
            return new VerificationReport(errors.Count == 0, audit.Counts, quarters, statuses, metrics, errors.Distinct().ToList());
        });

    private const string QuarterSql = """
        WITH q AS (SELECT n,DATEADD(hour,-7,DATEADD(quarter,n,DATEADD(hour,7,@start))) s,DATEADD(hour,-7,DATEADD(quarter,n+1,DATEADD(hour,7,@start))) e FROM (VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8)) v(n))
        SELECT YEAR(DATEADD(hour,7,q.s)),DATEPART(quarter,DATEADD(hour,7,q.s)),
          CASE WHEN @full=0 THEN CASE WHEN n=8 THEN 12 ELSE 6 END WHEN n=8 THEN 35
            WHEN n<4 THEN CASE DATEPART(quarter,DATEADD(hour,7,q.s)) WHEN 1 THEN 62 WHEN 2 THEN 58 WHEN 3 THEN 66 ELSE 70 END
            ELSE CASE DATEPART(quarter,DATEADD(hour,7,q.s)) WHEN 1 THEN 76 WHEN 2 THEN 70 WHEN 3 THEN 80 ELSE 84 END END,
          (SELECT COUNT(*) FROM dbo.DON_HANG o WHERE o.CreatedAt>=q.s AND o.CreatedAt<q.e AND o.CreatedAt<=@cutoff),
          COALESCE((SELECT SUM(o.Total) FROM dbo.DON_HANG o JOIN dbo.THANH_TOAN p ON p.OrderId=o.Id
            WHERE o.Status='HOAN_TAT' AND p.Status='DA_THU' AND o.CompletedAt>=q.s AND o.CompletedAt<q.e AND o.CompletedAt<=@cutoff),CONVERT(decimal(18,2),0))
        FROM q ORDER BY n
        """;

    private const string MetricsSql = """
        SELECT 'RecentOrders',COUNT_BIG(*) FROM dbo.DON_HANG WHERE CreatedAt>=DATEADD(day,-30,@cutoff) AND CreatedAt<=@cutoff
        UNION ALL SELECT 'CurrentMonthEligibleProducts',COUNT_BIG(DISTINCT s.Id) FROM dbo.DON_HANG o JOIN dbo.THANH_TOAN t ON t.OrderId=o.Id JOIN dbo.DON_HANG_CT l ON l.OrderId=o.Id JOIN dbo.BIEN_THE v ON v.Id=l.VariantId JOIN dbo.SAN_PHAM s ON s.Id=v.ProductId JOIN dbo.HANG b ON b.Id=s.BrandId JOIN dbo.DANH_MUC c ON c.Id=s.CategoryId
          WHERE o.Status='HOAN_TAT' AND t.Status='DA_THU' AND s.Active=1 AND v.Active=1 AND b.Active=1 AND c.Active=1 AND o.CompletedAt>=DATEADD(hour,-7,CONVERT(datetime2,DATEFROMPARTS(YEAR(DATEADD(hour,7,@cutoff)),MONTH(DATEADD(hour,7,@cutoff)),1))) AND o.CompletedAt<=@cutoff
        UNION ALL SELECT 'LowStockVariants',COUNT_BIG(*) FROM dbo.TON_KHO WHERE OnHand<=5
        UNION ALL SELECT 'OutOfStockVariants',COUNT_BIG(*) FROM dbo.TON_KHO WHERE OnHand=0
        UNION ALL SELECT 'TypicalStockVariants',COUNT_BIG(*) FROM dbo.TON_KHO WHERE OnHand BETWEEN 5 AND 40
        UNION ALL SELECT 'FailedOrders',COUNT_BIG(*) FROM dbo.DON_HANG WHERE Status='GIAO_THAT_BAI'
        UNION ALL SELECT 'FailedWithReturn',COUNT_BIG(*) FROM dbo.DON_HANG o WHERE Status='GIAO_THAT_BAI' AND EXISTS(SELECT 1 FROM dbo.NHAT_KY_KHO m WHERE m.OrderId=o.Id AND m.Kind='NHAN_LAI')
        UNION ALL SELECT 'Adjustments',COUNT_BIG(*) FROM dbo.NHAT_KY_KHO WHERE Kind='DIEU_CHINH'
        UNION ALL SELECT 'NoOrderCustomers',COUNT_BIG(*) FROM dbo.KHACH_HANG c WHERE NOT EXISTS(SELECT 1 FROM dbo.DON_HANG o WHERE o.CustomerId=c.Id)
        UNION ALL SELECT 'Repeat5To10Customers',COUNT_BIG(*) FROM (SELECT CustomerId FROM dbo.DON_HANG GROUP BY CustomerId HAVING COUNT(*) BETWEEN 5 AND 10) d
        UNION ALL SELECT 'OneOrderCustomers',COUNT_BIG(*) FROM (SELECT CustomerId FROM dbo.DON_HANG GROUP BY CustomerId HAVING COUNT(*)=1) d
        UNION ALL SELECT 'Drafts',COUNT_BIG(*) FROM dbo.PHIEU_NHAP WHERE Status='NHAP'
        UNION ALL SELECT 'EmptyDrafts',COUNT_BIG(*) FROM dbo.PHIEU_NHAP p WHERE Status='NHAP' AND NOT EXISTS(SELECT 1 FROM dbo.PHIEU_NHAP_CT l WHERE l.PurchaseId=p.Id)
        UNION ALL SELECT 'CancelledPurchases',COUNT_BIG(*) FROM dbo.PHIEU_NHAP WHERE Status='DA_HUY'
        UNION ALL SELECT 'InactiveSuppliers',COUNT_BIG(*) FROM dbo.NHA_CUNG_CAP WHERE Active=0
        UNION ALL SELECT 'InactiveProductsWithHistory',COUNT_BIG(*) FROM dbo.SAN_PHAM p WHERE Active=0 AND EXISTS(SELECT 1 FROM dbo.BIEN_THE v JOIN dbo.DON_HANG_CT l ON l.VariantId=v.Id WHERE v.ProductId=p.Id)
        UNION ALL SELECT 'CartCustomers',COUNT_BIG(DISTINCT CustomerId) FROM dbo.GIO_HANG_CT
        """;

    private static readonly (string Label, string Sql)[] Checks =
    [
        ("Reference horizon", "SELECT CASE WHEN @start<>DATEADD(hour,-7,DATEADD(quarter,DATEDIFF(quarter,0,DATEADD(hour,7,@cutoff))-8,0)) THEN 1 ELSE 0 END"),
        ("Initial product launch distribution", "SELECT CASE WHEN (SELECT COUNT(*) FROM OPENJSON(@products) WITH(LaunchAt datetime2 '$.LaunchAt') WHERE LaunchAt=@start)<>CASE WHEN @full=1 THEN 24 ELSE 3 END THEN 1 ELSE 0 END"),
        ("Future or out-of-horizon timestamps", """
          SELECT COUNT(*) FROM (
            SELECT CreatedAt t FROM dbo.TAI_KHOAN UNION ALL SELECT UpdatedAt FROM dbo.GIO_HANG_CT UNION ALL SELECT CreatedAt FROM dbo.DON_HANG
            UNION ALL SELECT CompletedAt FROM dbo.DON_HANG WHERE CompletedAt IS NOT NULL UNION ALL SELECT CollectedAt FROM dbo.THANH_TOAN WHERE CollectedAt IS NOT NULL
            UNION ALL SELECT At FROM dbo.LICH_SU_DON UNION ALL SELECT CreatedAt FROM dbo.PHIEU_NHAP UNION ALL SELECT ConfirmedAt FROM dbo.PHIEU_NHAP WHERE ConfirmedAt IS NOT NULL UNION ALL SELECT At FROM dbo.NHAT_KY_KHO
          ) d WHERE t>@cutoff OR t<DATEADD(day,-1,@start)
          """),
        ("Account and purchase chronology", """
          SELECT (SELECT COUNT(*) FROM dbo.DON_HANG o JOIN dbo.TAI_KHOAN a ON a.Id=o.CustomerId WHERE a.CreatedAt>=o.CreatedAt)
            +(SELECT COUNT(*) FROM dbo.PHIEU_NHAP p JOIN dbo.TAI_KHOAN a ON a.Id=p.CreatorId WHERE a.CreatedAt>=p.CreatedAt OR (p.ConfirmedAt IS NOT NULL AND p.ConfirmedAt<p.CreatedAt))
          """),
        ("Product launch/retirement chronology", """
          WITH facts AS (SELECT Code,LaunchAt,RetiredAt FROM OPENJSON(@products) WITH(Code nvarchar(30) '$.Code',LaunchAt datetime2 '$.LaunchAt',RetiredAt datetime2 '$.RetiredAt'))
          SELECT (SELECT COUNT(*) FROM dbo.SAN_PHAM p LEFT JOIN facts f ON f.Code=p.Code WHERE f.Code IS NULL OR f.LaunchAt>@cutoff OR f.LaunchAt<@start OR (f.RetiredAt IS NULL AND p.Active=0) OR (f.RetiredAt IS NOT NULL AND (p.Active=1 OR f.RetiredAt<f.LaunchAt OR f.RetiredAt>@cutoff)))
            +(SELECT COUNT(*) FROM dbo.PHIEU_NHAP_CT l JOIN dbo.PHIEU_NHAP r ON r.Id=l.PurchaseId JOIN dbo.BIEN_THE v ON v.Id=l.VariantId JOIN dbo.SAN_PHAM p ON p.Id=v.ProductId JOIN facts f ON f.Code=p.Code WHERE r.CreatedAt<=f.LaunchAt OR (f.RetiredAt IS NOT NULL AND r.CreatedAt>=f.RetiredAt))
            +(SELECT COUNT(*) FROM dbo.DON_HANG_CT l JOIN dbo.DON_HANG o ON o.Id=l.OrderId JOIN dbo.BIEN_THE v ON v.Id=l.VariantId JOIN dbo.SAN_PHAM p ON p.Id=v.ProductId JOIN facts f ON f.Code=p.Code WHERE o.CreatedAt<=f.LaunchAt OR (f.RetiredAt IS NOT NULL AND o.CreatedAt>=f.RetiredAt))
          """),
        ("Historical stock never negative or over-reserved", """
          WITH balances AS (SELECT SUM(CONVERT(bigint,DeltaOnHand)) OVER(PARTITION BY VariantId ORDER BY At,Id ROWS UNBOUNDED PRECEDING) h,SUM(CONVERT(bigint,DeltaReserved)) OVER(PARTITION BY VariantId ORDER BY At,Id ROWS UNBOUNDED PRECEDING) r FROM dbo.NHAT_KY_KHO)
          SELECT COUNT(*) FROM balances WHERE r<0 OR h<r
          """),
        ("Strict history timestamps and order creation", """
          WITH h AS (SELECT h.*,LAG(At) OVER(PARTITION BY OrderId ORDER BY Id) prev FROM dbo.LICH_SU_DON h)
          SELECT COUNT(*) FROM h JOIN dbo.DON_HANG o ON o.Id=h.OrderId WHERE h.At<o.CreatedAt OR (h.[From] IS NULL AND h.At<>o.CreatedAt) OR h.prev>=h.At
          """),
        ("History actors and required reasons", """
          SELECT COUNT(*) FROM dbo.LICH_SU_DON h JOIN dbo.DON_HANG o ON o.Id=h.OrderId JOIN dbo.TAI_KHOAN a ON a.Id=h.ActorId
          WHERE (h.[From] IS NULL AND h.ActorId<>o.CustomerId) OR (h.[From] IS NOT NULL AND NOT(h.[From]='CHO_XAC_NHAN' AND h.[To]='DA_HUY') AND a.Role<>'ADMIN')
          OR (h.[To] IN ('DA_HUY','GIAO_THAT_BAI') AND LEN(LTRIM(RTRIM(COALESCE(h.Reason,''))))=0)
          OR (h.Reason IS NOT NULL AND (LEN(h.Reason)=0 OR DATALENGTH(h.Reason)<>DATALENGTH(LTRIM(RTRIM(h.Reason)))))
          """),
        ("Completion history and COD timestamps", """
          SELECT COUNT(*) FROM dbo.DON_HANG o LEFT JOIN dbo.THANH_TOAN p ON p.OrderId=o.Id
          WHERE (o.Status='HOAN_TAT' AND NOT EXISTS(SELECT 1 FROM dbo.LICH_SU_DON h WHERE h.OrderId=o.Id AND h.[From]='DANG_GIAO' AND h.[To]='HOAN_TAT' AND h.At=o.CompletedAt AND h.ActorId=p.ActorId))
          OR (o.CompletedAt IS NOT NULL AND o.CompletedAt<=o.CreatedAt)
          """),
        ("Workflow confirmation/dispatch/completion delays", """
          SELECT COUNT(*) FROM dbo.LICH_SU_DON h JOIN dbo.DON_HANG o ON o.Id=h.OrderId
          OUTER APPLY(SELECT MAX(At) At FROM dbo.LICH_SU_DON c WHERE c.OrderId=h.OrderId AND c.[To]='DA_XAC_NHAN') confirm
          OUTER APPLY(SELECT MAX(At) At FROM dbo.LICH_SU_DON d WHERE d.OrderId=h.OrderId AND d.[To]='DANG_GIAO') dispatch
          WHERE (h.[To]='DA_XAC_NHAN' AND h.At>DATEADD(day,1,o.CreatedAt))
            OR (h.[To]='DANG_GIAO' AND (h.At<DATEADD(day,1,confirm.At) OR h.At>DATEADD(day,2,confirm.At)))
            OR (h.[To]='HOAN_TAT' AND (h.At<DATEADD(day,2,dispatch.At) OR h.At>DATEADD(day,4,dispatch.At)))
          """),
        ("Return history requires complete physical journal", """
          SELECT COUNT(*) FROM dbo.LICH_SU_DON h JOIN dbo.DON_HANG_CT l ON l.OrderId=h.OrderId
          WHERE h.[From]='GIAO_THAT_BAI' AND h.[To]='GIAO_THAT_BAI' AND NOT EXISTS(
            SELECT 1 FROM dbo.NHAT_KY_KHO m WHERE m.OrderId=h.OrderId AND m.VariantId=l.VariantId AND m.Kind='NHAN_LAI' AND m.At=h.At AND m.ActorId=h.ActorId AND m.DeltaOnHand=l.Quantity AND m.DeltaReserved=0)
          """),
        ("Latest confirmed cost by event time", """
          SELECT COUNT(*) FROM dbo.BIEN_THE v OUTER APPLY(SELECT TOP(1) l.Cost FROM dbo.NHAT_KY_KHO m JOIN dbo.PHIEU_NHAP_CT l ON l.PurchaseId=m.PurchaseId AND l.VariantId=m.VariantId WHERE m.VariantId=v.Id AND m.Kind='NHAP' ORDER BY m.At DESC,m.Id DESC) c WHERE c.Cost IS NULL OR v.Cost<>c.Cost
          """),
        ("Journal action timestamps, actors and physical returns", """
          SELECT COUNT(*) FROM dbo.NHAT_KY_KHO m JOIN dbo.TAI_KHOAN a ON a.Id=m.ActorId LEFT JOIN dbo.DON_HANG o ON o.Id=m.OrderId LEFT JOIN dbo.PHIEU_NHAP p ON p.Id=m.PurchaseId
          WHERE (m.Kind='GIU' AND (m.At<>o.CreatedAt OR m.ActorId<>o.CustomerId OR m.OperationKey<>o.RequestKey))
          OR (m.Kind='NHAP' AND (m.At<>p.ConfirmedAt OR m.ActorId<>p.ConfirmerId))
          OR (m.Kind IN ('NHAP','XUAT_GIAO','NHAN_LAI','DIEU_CHINH') AND a.Role<>'ADMIN')
          OR (m.Kind IN ('HUY_GIU','XUAT_GIAO','NHAN_LAI') AND NOT EXISTS(SELECT 1 FROM dbo.LICH_SU_DON h WHERE h.OrderId=m.OrderId AND h.At=m.At AND h.ActorId=m.ActorId AND ((m.Kind='HUY_GIU' AND h.[To]='DA_HUY') OR (m.Kind='XUAT_GIAO' AND h.[From]='DA_XAC_NHAN' AND h.[To]='DANG_GIAO') OR (m.Kind='NHAN_LAI' AND h.[From]='GIAO_THAT_BAI' AND h.[To]='GIAO_THAT_BAI'))))
          """),
        ("Historical order snapshot prices and immutable descriptions", """
          SELECT COUNT(*) FROM dbo.DON_HANG_CT l JOIN dbo.DON_HANG o ON o.Id=l.OrderId JOIN dbo.BIEN_THE v ON v.Id=l.VariantId JOIN dbo.SAN_PHAM p ON p.Id=v.ProductId
          OUTER APPLY(SELECT TOP(1) pl.Cost FROM dbo.NHAT_KY_KHO m JOIN dbo.PHIEU_NHAP_CT pl ON pl.PurchaseId=m.PurchaseId AND pl.VariantId=m.VariantId WHERE m.Kind='NHAP' AND m.VariantId=v.Id AND m.At<=o.CreatedAt ORDER BY m.At DESC,m.Id DESC) cost
          WHERE cost.Cost IS NULL OR l.Price<>ROUND(cost.Cost*(1+v.ProfitPercent/100),2) OR l.Name<>p.Name OR l.Sku<>v.Sku OR l.Description<>v.Color+N' / '+CONVERT(nvarchar(20),v.StorageGb)+N'GB'
          """),
        ("Declining historical purchase costs", """
          WITH costs AS (SELECT l.VariantId,l.Cost,m.At,LAG(l.Cost) OVER(PARTITION BY l.VariantId ORDER BY m.At,m.Id) prev,LAG(m.At) OVER(PARTITION BY l.VariantId ORDER BY m.At,m.Id) prevAt FROM dbo.PHIEU_NHAP_CT l JOIN dbo.NHAT_KY_KHO m ON m.PurchaseId=l.PurchaseId AND m.VariantId=l.VariantId AND m.Kind='NHAP')
          SELECT COUNT(*) FROM costs WHERE prev IS NOT NULL AND (Cost>prev OR Cost<prev*POWER(CONVERT(float,.94),DATEDIFF(quarter,DATEADD(hour,7,prevAt),DATEADD(hour,7,At)))-.03 OR Cost>prev*POWER(CONVERT(float,.97),DATEDIFF(quarter,DATEADD(hour,7,prevAt),DATEADD(hour,7,At)))+.03)
          """),
        ("Unique normalized optional identifiers", """
          SELECT (SELECT COUNT(*) FROM dbo.KHACH_HANG WHERE Email='' OR (Email IS NOT NULL AND (Email NOT LIKE '%@example.com' OR Email<>LOWER(LTRIM(RTRIM(Email))))))
            +(SELECT COUNT(*) FROM dbo.TAI_KHOAN WHERE Username COLLATE Latin1_General_100_BIN2<>LOWER(LTRIM(RTRIM(Username))) COLLATE Latin1_General_100_BIN2)
            +(SELECT COUNT(*) FROM(SELECT Phone FROM dbo.KHACH_HANG GROUP BY Phone HAVING COUNT(*)>1) d)
            +(SELECT COUNT(*) FROM(SELECT Email FROM dbo.KHACH_HANG WHERE Email IS NOT NULL GROUP BY Email HAVING COUNT(*)>1) d)
          """),
        ("Action key reuse across documents", """
          SELECT COUNT(*) FROM(SELECT OperationKey FROM dbo.NHAT_KY_KHO GROUP BY OperationKey HAVING COUNT(DISTINCT COALESCE(CONVERT(varchar(30),OrderId),'none'))>1 OR COUNT(DISTINCT COALESCE(CONVERT(varchar(30),PurchaseId),'none'))>1 OR COUNT(DISTINCT Kind)>1 OR COUNT(DISTINCT At)>1 OR COUNT(DISTINCT ActorId)>1) d
          """),
        ("Open orders only in recent days", "SELECT COUNT(*) FROM dbo.DON_HANG WHERE Status IN ('CHO_XAC_NHAN','DA_XAC_NHAN','DANG_GIAO') AND CreatedAt<DATEADD(day,-7,@cutoff)"),
        ("Cart availability and freshness", "SELECT COUNT(*) FROM dbo.GIO_HANG_CT c JOIN dbo.BIEN_THE v ON v.Id=c.VariantId JOIN dbo.SAN_PHAM p ON p.Id=v.ProductId JOIN dbo.TON_KHO i ON i.VariantId=v.Id JOIN dbo.TAI_KHOAN a ON a.Id=c.CustomerId WHERE a.Active=0 OR v.Active=0 OR p.Active=0 OR c.Quantity>i.OnHand-i.Reserved OR c.UpdatedAt<DATEADD(day,-7,@cutoff)"),
        ("No retained simulation sessions", "SELECT COUNT(*) FROM dbo.PHIEN")
    ];
}
