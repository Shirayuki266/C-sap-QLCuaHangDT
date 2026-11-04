using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class FinalSchemaSmokeChecks
{
    private sealed record ColumnRow(string ColumnName, string DataType, int? Precision, int? Scale, string IsNullable);
    private sealed record VersionRow(string TableName, string ColumnName, short MaxLength, bool IsNullable);
    private sealed record IndexRow(string TableName, string IndexName, string Columns, bool IsPrimaryKey, string? FilterDefinition);
    private sealed record ForeignKeyRow(string FromTable, string FromColumn, string ToTable, string ToColumn, byte DeleteAction, byte UpdateAction, bool IsDisabled, bool IsNotTrusted);
    private sealed record CheckRow(string TableName, string ConstraintName, string Definition, bool IsDisabled, bool IsNotTrusted);

    public static async Task<object> ReadAndVerify(string connection)
    {
        await using var db = new StoreDbContext(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
        var tables = await db.Database.SqlQueryRaw<string>("SELECT TABLE_NAME AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME").ToListAsync();
        var columns = await db.Database.SqlQueryRaw<ColumnRow>("""
            SELECT COLUMN_NAME AS ColumnName,DATA_TYPE AS DataType,CONVERT(int,NUMERIC_PRECISION) AS Precision,
                CONVERT(int,NUMERIC_SCALE) AS Scale,IS_NULLABLE AS IsNullable
            FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='BIEN_THE' AND COLUMN_NAME IN ('Cost','ProfitPercent','Price') ORDER BY COLUMN_NAME
            """).ToListAsync();
        var versions = await db.Database.SqlQueryRaw<VersionRow>("""
            SELECT t.name AS TableName,c.name AS ColumnName,c.max_length AS MaxLength,c.is_nullable AS IsNullable
            FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id WHERE t.is_ms_shipped=0 AND c.system_type_id=189 ORDER BY t.name
            """).ToListAsync();
        var indexes = await db.Database.SqlQueryRaw<IndexRow>("""
            SELECT t.name AS TableName,i.name AS IndexName,
                STRING_AGG(CONVERT(nvarchar(max),c.name),N',') WITHIN GROUP(ORDER BY ic.key_ordinal) AS Columns,
                i.is_primary_key AS IsPrimaryKey,i.filter_definition AS FilterDefinition
            FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE t.is_ms_shipped=0 AND i.is_unique=1 AND i.is_disabled=0 AND ic.key_ordinal>0
            GROUP BY t.name,i.name,i.is_primary_key,i.filter_definition ORDER BY t.name,i.name
            """).ToListAsync();
        var foreignKeys = await db.Database.SqlQueryRaw<ForeignKeyRow>("""
            SELECT p.name AS FromTable,pc.name AS FromColumn,r.name AS ToTable,rc.name AS ToColumn,
                f.delete_referential_action AS DeleteAction,f.update_referential_action AS UpdateAction,
                f.is_disabled AS IsDisabled,f.is_not_trusted AS IsNotTrusted
            FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id
            JOIN sys.tables p ON p.object_id=f.parent_object_id JOIN sys.tables r ON r.object_id=f.referenced_object_id
            JOIN sys.columns pc ON pc.object_id=p.object_id AND pc.column_id=fc.parent_column_id
            JOIN sys.columns rc ON rc.object_id=r.object_id AND rc.column_id=fc.referenced_column_id
            ORDER BY p.name,pc.name,r.name
            """).ToListAsync();
        var checks = await db.Database.SqlQueryRaw<CheckRow>("""
            SELECT t.name AS TableName,c.name AS ConstraintName,c.definition AS Definition,c.is_disabled AS IsDisabled,c.is_not_trusted AS IsNotTrusted
            FROM sys.check_constraints c JOIN sys.tables t ON t.object_id=c.parent_object_id ORDER BY t.name,c.name
            """).ToListAsync();
        await Check.Run("Closeout INFORMATION_SCHEMA sys prove final nineteen-table schema unique checks NO ACTION FK and rowversions", async () =>
        {
            var expected = new[] { "TAI_KHOAN","KHACH_HANG","PHIEN","HANG","DANH_MUC","SAN_PHAM","SAN_PHAM_ANH","BIEN_THE","TON_KHO","GIO_HANG_CT","NHA_CUNG_CAP","PHIEU_NHAP","PHIEU_NHAP_CT","DON_HANG","DON_HANG_CT","THANH_TOAN","LICH_SU_DON","NHAT_KY_KHO","__PhoneStoreMigrations" };
            Check.True(tables.Count == 19 && tables.Order().SequenceEqual(expected.Order()) &&
                await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE is_ms_shipped=0").SingleAsync() == 19, "Final table set differs from the16+image+session+ledger requirement.");
            Check.True(columns.Count == 3 && columns.All(c => c.DataType == "decimal" && c.Scale == 2 && c.IsNullable == "NO") &&
                columns.Single(c => c.ColumnName == "Cost").Precision == 18 && columns.Single(c => c.ColumnName == "Price").Precision == 18 &&
                columns.Single(c => c.ColumnName == "ProfitPercent").Precision == 7, "Variant final price/cost columns differ.");
            Check.True(versions.Count == 4 && versions.All(v => v.ColumnName == "Version" && v.MaxLength == 8 && !v.IsNullable) &&
                versions.Select(v => v.TableName).Order().SequenceEqual(new[] { "BIEN_THE","TON_KHO","DON_HANG","PHIEU_NHAP" }.Order()), "SQL rowversion set differs.");
            Check.True(indexes.Count(i => i.IsPrimaryKey) == 19, "A final table lacks its primary key.");
            foreach (var (table, key) in new[] {
                ("TAI_KHOAN","Username"),("KHACH_HANG","Phone"),("KHACH_HANG","Email"),("HANG","Name"),("DANH_MUC","Name"),
                ("SAN_PHAM","Code"),("SAN_PHAM_ANH","ProductId,UrlHash"),("BIEN_THE","Sku"),("BIEN_THE","ProductId,Color,StorageGb"),
                ("GIO_HANG_CT","CustomerId,VariantId"),("NHA_CUNG_CAP","Code"),("DON_HANG","Code"),("DON_HANG","RequestKey"),
                ("DON_HANG_CT","OrderId,VariantId"),("PHIEU_NHAP","Code"),("PHIEU_NHAP_CT","PurchaseId,VariantId"),
                ("NHAT_KY_KHO","OperationKey,VariantId"),("NHAT_KY_KHO","OrderId,VariantId,Kind"),("NHAT_KY_KHO","PurchaseId,VariantId,Kind") })
                Check.True(indexes.Any(i => i.TableName == table && i.Columns == key), "Required uniqueness missing: " + table + "/" + key);
            static string Normalize(string value) => Regex.Replace(value.ToUpperInvariant(), @"[\s\[\]()]", "");
            foreach (var (table, key, filter) in new[] { ("KHACH_HANG","Email","EMAILISNOTNULL"),
                ("NHAT_KY_KHO","OrderId,VariantId,Kind","ORDERIDISNOTNULL"),("NHAT_KY_KHO","PurchaseId,VariantId,Kind","PURCHASEIDISNOTNULL") })
                Check.True(indexes.Any(i => i.TableName == table && i.Columns == key && Normalize(i.FilterDefinition ?? "") == filter), "Required filtered unique differs.");
            var edges = new[] {
                "KHACH_HANG.Id>TAI_KHOAN.Id","PHIEN.AccountId>TAI_KHOAN.Id","SAN_PHAM.BrandId>HANG.Id","SAN_PHAM.CategoryId>DANH_MUC.Id",
                "SAN_PHAM_ANH.ProductId>SAN_PHAM.Id","BIEN_THE.ProductId>SAN_PHAM.Id","TON_KHO.VariantId>BIEN_THE.Id",
                "GIO_HANG_CT.CustomerId>KHACH_HANG.Id","GIO_HANG_CT.VariantId>BIEN_THE.Id","DON_HANG.CustomerId>KHACH_HANG.Id",
                "DON_HANG_CT.OrderId>DON_HANG.Id","DON_HANG_CT.VariantId>BIEN_THE.Id","THANH_TOAN.OrderId>DON_HANG.Id","THANH_TOAN.ActorId>TAI_KHOAN.Id",
                "LICH_SU_DON.OrderId>DON_HANG.Id","LICH_SU_DON.ActorId>TAI_KHOAN.Id","PHIEU_NHAP.SupplierId>NHA_CUNG_CAP.Id",
                "PHIEU_NHAP.CreatorId>TAI_KHOAN.Id","PHIEU_NHAP.ConfirmerId>TAI_KHOAN.Id","PHIEU_NHAP_CT.PurchaseId>PHIEU_NHAP.Id",
                "PHIEU_NHAP_CT.VariantId>BIEN_THE.Id","NHAT_KY_KHO.VariantId>BIEN_THE.Id","NHAT_KY_KHO.ActorId>TAI_KHOAN.Id",
                "NHAT_KY_KHO.OrderId>DON_HANG.Id","NHAT_KY_KHO.PurchaseId>PHIEU_NHAP.Id" };
            Check.True(foreignKeys.Select(f => $"{f.FromTable}.{f.FromColumn}>{f.ToTable}.{f.ToColumn}").Order().SequenceEqual(edges.Order()) &&
                foreignKeys.All(f => f.DeleteAction == 0 && f.UpdateAction == 0 && !f.IsDisabled && !f.IsNotTrusted), "FK graph/NO ACTION/trust differs from the final schema.");
            Check.True(checks.Count > 0 && checks.All(c => !c.IsDisabled && !c.IsNotTrusted), "Disabled/untrusted CHECK found.");
            foreach (var (table, fragments) in new[] {
                ("TAI_KHOAN",new[]{"ROLE=","'CUSTOMER'","'ADMIN'"}), ("BIEN_THE",new[]{"COST>0","PRICE>0","STORAGEGB>0","PROFITPERCENT>=0","PROFITPERCENT<=10000"}),
                ("TON_KHO",new[]{"RESERVED>=0","ONHAND>=RESERVED"}), ("GIO_HANG_CT",new[]{"QUANTITY>0"}),
                ("DON_HANG",new[]{"TOTAL>0","COMPLETEDATISNOTNULL","REQUESTKEY<>"}), ("DON_HANG_CT",new[]{"QUANTITY>0","PRICE>0"}),
                ("THANH_TOAN",new[]{"METHOD='COD'","AMOUNT=0","AMOUNT>0","COLLECTEDATISNOTNULL"}),
                ("PHIEU_NHAP",new[]{"TOTAL>=0","CONFIRMERIDISNOTNULL","CONFIRMEDATISNOTNULL"}),
                ("PHIEU_NHAP_CT",new[]{"QUANTITY>0","COST>0"}), ("NHAT_KY_KHO",new[]{"ORDERIDISNULLORPURCHASEIDISNULL","DELTAONHAND<>0","DELTARESERVED<>0"}) })
            {
                var definitions = string.Join(";", checks.Where(c => c.TableName == table).Select(c => Normalize(c.Definition)));
                Check.True(fragments.All(definitions.Contains), "Required CHECK semantics absent: " + table);
            }
        });
        return new { Tables = tables, LedgerTable = "dbo.__PhoneStoreMigrations", VariantColumns = columns,
            Rowversions = versions, UniqueIndexes = indexes, ForeignKeys = foreignKeys, Checks = checks };
    }
}
