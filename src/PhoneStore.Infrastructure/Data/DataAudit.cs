using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Infrastructure.Data;
public sealed record DatabaseAudit(Dictionary<string,int> Counts,List<string> Errors)
{
    public bool Valid=>Errors.Count==0;
}
public sealed class DataAudit(StoreDbContext db)
{
    public Task<DatabaseAudit> Run()=>new BusinessKernel(db,TimeProvider.System).Read<DatabaseAudit>(async()=>
    {
        var counts=new Dictionary<string,int>();
        foreach(var table in db.Model.GetEntityTypes().Select(x=>x.GetTableName()!).Append("__PhoneStoreMigrations").Order())
        {
            // Identifiers come only from the compiled EF model, never HTTP/CLI input.
            var countSql="SELECT COUNT(*) AS [Value] FROM dbo.["+table.Replace("]","]]")+ "]";
            counts[table]=await db.Database.SqlQueryRaw<int>(countSql).SingleAsync();
        }
        var errors=new List<string>();
        foreach(var (label,query) in Checks)
        {
            // Every fragment in Checks is a private, source-controlled SQL constant.
            var auditSql="SELECT COUNT(*) AS [Value] FROM ("+query+") audit";
            if(await db.Database.SqlQueryRaw<int>(auditSql).SingleAsync()>0)errors.Add(label);
        }
        return new DatabaseAudit(counts,errors);
    });
    private static readonly (string Label,string Sql)[] Checks=
    [
        ("Unmanaged schema tables", "SELECT object_id FROM sys.tables WHERE is_ms_shipped=0 AND (schema_id<>SCHEMA_ID('dbo') OR name NOT IN ('TAI_KHOAN','KHACH_HANG','PHIEN','HANG','DANH_MUC','SAN_PHAM','SAN_PHAM_ANH','BIEN_THE','TON_KHO','GIO_HANG_CT','DON_HANG','DON_HANG_CT','THANH_TOAN','LICH_SU_DON','NHA_CUNG_CAP','PHIEU_NHAP','PHIEU_NHAP_CT','NHAT_KY_KHO','__PhoneStoreMigrations'))"),
        ("Disabled/untrusted constraints", "SELECT object_id FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1 UNION ALL SELECT object_id FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1"),
        ("Account/customer role mapping", "SELECT a.Id FROM dbo.TAI_KHOAN a LEFT JOIN dbo.KHACH_HANG c ON a.Id=c.Id WHERE (a.Role='CUSTOMER' AND c.Id IS NULL) OR (a.Role='ADMIN' AND c.Id IS NOT NULL)"),
        ("Variant missing inventory", "SELECT v.Id FROM dbo.BIEN_THE v LEFT JOIN dbo.TON_KHO i ON i.VariantId=v.Id WHERE i.VariantId IS NULL"),
        ("Inventory/journal balance", "SELECT i.VariantId FROM dbo.TON_KHO i LEFT JOIN (SELECT VariantId,SUM(CONVERT(bigint,DeltaOnHand)) h,SUM(CONVERT(bigint,DeltaReserved)) r FROM dbo.NHAT_KY_KHO GROUP BY VariantId) j ON j.VariantId=i.VariantId WHERE i.OnHand<>COALESCE(j.h,0) OR i.Reserved<>COALESCE(j.r,0) OR i.Reserved<0 OR i.OnHand<i.Reserved"),
        ("Reserved stock versus open orders", "SELECT i.VariantId FROM dbo.TON_KHO i LEFT JOIN (SELECT l.VariantId,SUM(CONVERT(bigint,l.Quantity)) q FROM dbo.DON_HANG_CT l JOIN dbo.DON_HANG o ON l.OrderId=o.Id WHERE o.Status IN ('CHO_XAC_NHAN','DA_XAC_NHAN') GROUP BY l.VariantId) r ON i.VariantId=r.VariantId WHERE i.Reserved<>COALESCE(r.q,0)"),
        ("Order totals/required lines/payment", "SELECT o.Id FROM dbo.DON_HANG o LEFT JOIN (SELECT OrderId,SUM(Quantity*Price) t FROM dbo.DON_HANG_CT GROUP BY OrderId) l ON l.OrderId=o.Id LEFT JOIN dbo.THANH_TOAN p ON p.OrderId=o.Id WHERE l.OrderId IS NULL OR o.Total<>l.t OR p.OrderId IS NULL"),
        ("Order/payment completion invariant", "SELECT o.Id FROM dbo.DON_HANG o JOIN dbo.THANH_TOAN p ON o.Id=p.OrderId LEFT JOIN dbo.TAI_KHOAN a ON a.Id=p.ActorId WHERE p.Method<>'COD' OR (o.Status='HOAN_TAT' AND (p.Status<>'DA_THU' OR p.Amount<>o.Total OR p.CollectedAt<>o.CompletedAt OR a.Role<>'ADMIN')) OR (o.Status<>'HOAN_TAT' AND (p.Status<>'CHUA_THU' OR p.Amount<>0))"),
        ("Missing order snapshots", "SELECT Id FROM dbo.DON_HANG_CT WHERE LEN(Name)=0 OR LEN(Sku)=0 OR LEN(Description)=0 UNION ALL SELECT Id FROM dbo.DON_HANG WHERE LEN(Recipient)=0 OR LEN(Phone)=0 OR LEN(Address)=0"),
        ("Order history chain", "SELECT Id FROM (SELECT h.*,ROW_NUMBER() OVER(PARTITION BY OrderId ORDER BY Id) n,LAG([To]) OVER(PARTITION BY OrderId ORDER BY Id) prev FROM dbo.LICH_SU_DON h) h WHERE (n=1 AND ([From] IS NOT NULL OR [To]<>'CHO_XAC_NHAN')) OR (n>1 AND ([From] IS NULL OR [From]<>prev))"),
        ("Latest history state", "SELECT o.Id FROM dbo.DON_HANG o OUTER APPLY (SELECT TOP(1) [To] FROM dbo.LICH_SU_DON h WHERE h.OrderId=o.Id ORDER BY h.Id DESC) h WHERE h.[To] IS NULL OR h.[To]<>o.Status"),
        ("Illegal history transition/actor", "SELECT h.Id FROM dbo.LICH_SU_DON h JOIN dbo.DON_HANG o ON o.Id=h.OrderId JOIN dbo.TAI_KHOAN a ON a.Id=h.ActorId WHERE NOT ((h.[From] IS NULL AND h.[To]='CHO_XAC_NHAN') OR (h.[From]='CHO_XAC_NHAN' AND h.[To] IN ('DA_XAC_NHAN','DA_HUY')) OR (h.[From]='DA_XAC_NHAN' AND h.[To] IN ('DANG_GIAO','DA_HUY')) OR (h.[From]='DANG_GIAO' AND h.[To] IN ('HOAN_TAT','GIAO_THAT_BAI')) OR (h.[From]='GIAO_THAT_BAI' AND h.[To]='GIAO_THAT_BAI')) OR (a.Role='CUSTOMER' AND (a.Id<>o.CustomerId OR NOT (h.[From] IS NULL OR (h.[From]='CHO_XAC_NHAN' AND h.[To]='DA_HUY'))))"),
        ("Incomplete physical return", "SELECT l.Id FROM dbo.DON_HANG_CT l WHERE EXISTS(SELECT 1 FROM dbo.NHAT_KY_KHO m WHERE m.OrderId=l.OrderId AND m.Kind='NHAN_LAI') AND NOT EXISTS(SELECT 1 FROM dbo.NHAT_KY_KHO m WHERE m.OrderId=l.OrderId AND m.VariantId=l.VariantId AND m.Kind='NHAN_LAI' AND m.DeltaOnHand=l.Quantity AND m.DeltaReserved=0)"),
        ("Order journal quantity/effects", "SELECT l.Id FROM dbo.DON_HANG_CT l JOIN dbo.DON_HANG o ON o.Id=l.OrderId WHERE NOT EXISTS(SELECT 1 FROM dbo.NHAT_KY_KHO m WHERE m.OrderId=o.Id AND m.VariantId=l.VariantId AND m.Kind='GIU' AND m.DeltaReserved=l.Quantity AND m.DeltaOnHand=0) OR (o.Status='DA_HUY' AND NOT EXISTS(SELECT 1 FROM dbo.NHAT_KY_KHO m WHERE m.OrderId=o.Id AND m.VariantId=l.VariantId AND m.Kind='HUY_GIU' AND m.DeltaReserved=-l.Quantity AND m.DeltaOnHand=0)) OR (o.Status IN ('DANG_GIAO','HOAN_TAT','GIAO_THAT_BAI') AND NOT EXISTS(SELECT 1 FROM dbo.NHAT_KY_KHO m WHERE m.OrderId=o.Id AND m.VariantId=l.VariantId AND m.Kind='XUAT_GIAO' AND m.DeltaReserved=-l.Quantity AND m.DeltaOnHand=-l.Quantity))"),
        ("Unexpected order journal", "SELECT m.Id FROM dbo.NHAT_KY_KHO m JOIN dbo.DON_HANG o ON o.Id=m.OrderId LEFT JOIN dbo.DON_HANG_CT l ON l.OrderId=m.OrderId AND l.VariantId=m.VariantId WHERE l.Id IS NULL OR m.Kind NOT IN ('GIU','HUY_GIU','XUAT_GIAO','NHAN_LAI') OR (m.Kind='HUY_GIU' AND o.Status<>'DA_HUY') OR (m.Kind='XUAT_GIAO' AND o.Status NOT IN ('DANG_GIAO','HOAN_TAT','GIAO_THAT_BAI')) OR (m.Kind='NHAN_LAI' AND (o.Status<>'GIAO_THAT_BAI' OR m.DeltaOnHand<>l.Quantity OR m.DeltaReserved<>0 OR LEN(COALESCE(m.Reason,''))=0))"),
        ("Purchase totals/actors", "SELECT p.Id FROM dbo.PHIEU_NHAP p LEFT JOIN (SELECT PurchaseId,SUM(Quantity*Cost) t FROM dbo.PHIEU_NHAP_CT GROUP BY PurchaseId) l ON p.Id=l.PurchaseId JOIN dbo.TAI_KHOAN a ON a.Id=p.CreatorId LEFT JOIN dbo.TAI_KHOAN b ON b.Id=p.ConfirmerId WHERE p.Total<>COALESCE(l.t,0) OR a.Role<>'ADMIN' OR (p.Status='DA_XAC_NHAN' AND (l.PurchaseId IS NULL OR b.Role<>'ADMIN'))"),
        ("Confirmed purchase journal", "SELECT l.Id FROM dbo.PHIEU_NHAP_CT l JOIN dbo.PHIEU_NHAP p ON p.Id=l.PurchaseId WHERE p.Status='DA_XAC_NHAN' AND NOT EXISTS(SELECT 1 FROM dbo.NHAT_KY_KHO m WHERE m.PurchaseId=p.Id AND m.VariantId=l.VariantId AND m.Kind='NHAP' AND m.DeltaOnHand=l.Quantity AND m.DeltaReserved=0)"),
        ("Unexpected purchase journal", "SELECT m.Id FROM dbo.NHAT_KY_KHO m JOIN dbo.PHIEU_NHAP p ON p.Id=m.PurchaseId LEFT JOIN dbo.PHIEU_NHAP_CT l ON l.PurchaseId=m.PurchaseId AND l.VariantId=m.VariantId WHERE l.Id IS NULL OR p.Status<>'DA_XAC_NHAN' OR m.Kind<>'NHAP' OR m.DeltaOnHand<>l.Quantity OR m.DeltaReserved<>0"),
        ("Journal documents/reasons", "SELECT Id FROM dbo.NHAT_KY_KHO WHERE (Kind='NHAP' AND (PurchaseId IS NULL OR OrderId IS NOT NULL)) OR (Kind IN ('GIU','HUY_GIU','XUAT_GIAO','NHAN_LAI') AND (OrderId IS NULL OR PurchaseId IS NOT NULL)) OR (Kind='DIEU_CHINH' AND (OrderId IS NOT NULL OR PurchaseId IS NOT NULL OR DeltaReserved<>0 OR LEN(COALESCE(Reason,''))=0))"),
        ("Latest confirmed cost/calculated price", "SELECT v.Id FROM dbo.BIEN_THE v OUTER APPLY (SELECT TOP(1) l.Cost FROM dbo.NHAT_KY_KHO m JOIN dbo.PHIEU_NHAP_CT l ON l.PurchaseId=m.PurchaseId AND l.VariantId=m.VariantId WHERE m.VariantId=v.Id AND m.Kind='NHAP' ORDER BY m.Id DESC) last WHERE (last.Cost IS NOT NULL AND v.Cost<>last.Cost) OR v.Price<>ROUND(v.Cost*(1+v.ProfitPercent/100),2)")
    ];
}
