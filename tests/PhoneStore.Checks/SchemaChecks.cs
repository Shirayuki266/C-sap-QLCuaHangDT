using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class SchemaChecks
{
    public static async Task Run(string connection)
    {
        StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
        async Task Migrate() { await using var db = Open(); await new DatabaseMigrator(db).MigrateAsync(); }
        await Check.Run("Schema migration serializes independent callers and replays once", async () =>
        {
            await Task.WhenAll(Migrate(), Migrate());
            await using var db = Open();
            Check.True(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE is_ms_shipped=0").SingleAsync() == 19, "Expected 18 application tables and one migration ledger.");
            Check.True(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM dbo.__PhoneStoreMigrations").SingleAsync() == 1, "Duplicate migration ledger.");
            Check.True(await db.Accounts.CountAsync() == 0, "Migration unexpectedly seeded accounts.");
            Check.True(await new DatabaseMigrator(db).IsCurrentAsync(), "Current schema was not recognized.");
        });

        long variantId = 0, productId = 0, orderId = 0, purchaseId = 0;
        await Check.Run("EF mappings persist all 18 application entity types", async () =>
        {
            await using var db = Open();
            Check.True(db.Model.GetEntityTypes().Count() == 18, "Unexpected EF entity count.");
            Check.True(db.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()).All(x => x.DeleteBehavior == DeleteBehavior.NoAction), "A relationship would cascade delete history.");
            var admin = new Account { Username = "schema-admin", PasswordHash = "test-fixture-not-a-login-hash", Role = "ADMIN" };
            var customerAccount = new Account { Username = "schema-customer", PasswordHash = "test-fixture-not-a-login-hash" };
            var brand = new Brand { Name = "Schema Brand" };
            var category = new Category { Name = "Schema Category" };
            var supplier = new Supplier { Code = "SCHEMA-SUPPLIER", Name = "Schema Supplier", Phone = "0900000001", Address = "Test fixture" };
            db.AddRange(admin, customerAccount, brand, category, supplier);
            await db.SaveChangesAsync();
            var customer = new Customer { Id = customerAccount.Id, FullName = "Schema Customer", Phone = "0900000002" };
            var product = new Product { Code = "SCHEMA-PRODUCT", Name = "Schema Product", BrandId = brand.Id, CategoryId = category.Id };
            db.AddRange(customer, product);
            await db.SaveChangesAsync();
            productId = product.Id;
            var variant = new Variant { ProductId = product.Id, Sku = "SCHEMA-SKU", Color = "Black", StorageGb = 128, Cost = 100m, ProfitPercent = 20m, Price = 120m };
            var order = new Order { Code = "SCHEMA-ORDER", CustomerId = customer.Id, Recipient = "Test Recipient", Phone = "0900000002", Address = "Test address", Total = 120m, RequestKey = Guid.NewGuid() };
            var purchase = new Purchase { Code = "SCHEMA-PURCHASE", CreatorId = admin.Id, SupplierId = supplier.Id, Total = 100m };
            db.AddRange(variant, order, purchase);
            await db.SaveChangesAsync();
            variantId = variant.Id; orderId = order.Id; purchaseId = purchase.Id;
            var inventory = new Inventory { VariantId = variant.Id, OnHand = 5, Reserved = 1 };
            db.AddRange(inventory,
                new ProductImage { ProductId = product.Id, Url = "https://example.test/" + new string('a', 900), SortOrder = 0 },
                new LoginSession { Id = Guid.NewGuid(), AccountId = customer.Id, ExpiresAt = DateTime.UtcNow.AddHours(8) },
                new CartLine { CustomerId = customer.Id, VariantId = variant.Id, Quantity = 1 },
                new OrderLine { OrderId = order.Id, VariantId = variant.Id, Name = product.Name, Sku = variant.Sku, Description = "Black/128GB", Quantity = 1, Price = 120m },
                new Payment { OrderId = order.Id },
                new OrderHistory { OrderId = order.Id, ActorId = customer.Id, To = "CHO_XAC_NHAN" },
                new PurchaseLine { PurchaseId = purchase.Id, VariantId = variant.Id, Quantity = 1, Cost = 100m },
                new StockMovement { VariantId = variant.Id, ActorId = admin.Id, Kind = "DIEU_CHINH", DeltaOnHand = 5, DeltaReserved = 1, Reason = "Schema fixture", OperationKey = Guid.NewGuid() });
            await db.SaveChangesAsync();
            Check.True(new[] { variant.Version, inventory.Version, order.Version, purchase.Version }.All(version => version.Length == 8), "SQL rowversion was not generated for every concurrency entity.");
            await new DatabaseMigrator(db).MigrateAsync();
            Check.True(await db.Variants.CountAsync() == 1 && (await db.Variants.SingleAsync()).Price == 120m, "Migration replay changed persisted fixture data.");
            Check.True(await db.ProductImages.CountAsync() == 1 && await db.Payments.CountAsync() == 1 && await db.Movements.CountAsync() == 1, "A mapped entity was not saved.");
        });

        async Task RejectSql(string statement, params int[] numbers)
        {
            await using var db = Open();
            try { await db.Database.ExecuteSqlRawAsync(statement); }
            catch (SqlException exception) when (numbers.Contains(exception.Number)) { return; }
            throw new InvalidOperationException("Expected SQL Server to reject the invalid fixture change.");
        }
        await Check.Run("SQL constraints reject invalid stock, costs, percentages and COD", async () =>
        {
            foreach (var statement in new[] {
                "UPDATE dbo.TON_KHO SET OnHand=-1", "UPDATE dbo.TON_KHO SET Reserved=6", "UPDATE dbo.TON_KHO SET Reserved=-1",
                "UPDATE dbo.BIEN_THE SET Cost=0", "UPDATE dbo.BIEN_THE SET ProfitPercent=-0.01", "UPDATE dbo.BIEN_THE SET ProfitPercent=10000.01",
                "UPDATE dbo.BIEN_THE SET Price=0", "UPDATE dbo.BIEN_THE SET StorageGb=0", "UPDATE dbo.THANH_TOAN SET Method='QR'",
                "UPDATE dbo.THANH_TOAN SET Status='DA_THU'", "UPDATE dbo.DON_HANG SET Status='HOAN_TAT'", "UPDATE dbo.TAI_KHOAN SET Role='STAFF'",
                "UPDATE dbo.PHIEU_NHAP SET Status='DA_XAC_NHAN'", "UPDATE dbo.PHIEU_NHAP_CT SET Quantity=0",
                "UPDATE dbo.SAN_PHAM SET Specifications=REPLICATE(CAST(N'x' AS nvarchar(max)),20001)" })
                await RejectSql(statement, 547);
        });
        await Check.Run("SQL uniqueness and NO ACTION foreign keys preserve identities and history", async () =>
        {
            foreach (var statement in new[] {
                "INSERT dbo.TAI_KHOAN(Username,PasswordHash,Role) VALUES(N'schema-admin',N'fixture','CUSTOMER')",
                $"INSERT dbo.BIEN_THE(ProductId,Sku,Color,StorageGb,Cost,ProfitPercent,Price) VALUES({productId},N'SCHEMA-SKU',N'Blue',256,100,20,120)",
                $"INSERT dbo.BIEN_THE(ProductId,Sku,Color,StorageGb,Cost,ProfitPercent,Price) VALUES({productId},N'SCHEMA-OTHER',N'Black',128,100,20,120)",
                "INSERT dbo.SAN_PHAM_ANH(ProductId,Url,SortOrder) SELECT ProductId,Url,SortOrder FROM dbo.SAN_PHAM_ANH",
                "INSERT dbo.PHIEU_NHAP_CT(PurchaseId,VariantId,Quantity,Cost) SELECT PurchaseId,VariantId,Quantity,Cost FROM dbo.PHIEU_NHAP_CT",
                "INSERT dbo.NHAT_KY_KHO(VariantId,ActorId,Kind,DeltaOnHand,DeltaReserved,OperationKey) SELECT VariantId,ActorId,Kind,DeltaOnHand,DeltaReserved,OperationKey FROM dbo.NHAT_KY_KHO" })
                await RejectSql(statement, 2601, 2627);
            await RejectSql($"DELETE dbo.SAN_PHAM WHERE Id={productId}", 547);
            await RejectSql($"UPDATE dbo.NHAT_KY_KHO SET OrderId={orderId},PurchaseId={purchaseId}", 547);
            await RejectSql("UPDATE dbo.PHIEU_NHAP SET SupplierId=-1", 547);
            await RejectSql("UPDATE dbo.NHAT_KY_KHO SET DeltaOnHand=0,DeltaReserved=0", 547);
        });
        await Check.Run("Optional email accepts multiple nulls but rejects duplicate values", async () =>
        {
            await using var db = Open();
            var account = new Account { Username = "schema-second-customer", PasswordHash = "fixture" };
            db.Add(account); await db.SaveChangesAsync();
            db.Add(new Customer { Id = account.Id, FullName = "Second", Phone = "0900000003" });
            await db.SaveChangesAsync();
            Check.True(await db.Customers.CountAsync(x => x.Email == null) == 2, "Null emails were treated as duplicates.");
            await db.Database.ExecuteSqlRawAsync("UPDATE dbo.KHACH_HANG SET Email=N'unique@example.test' WHERE Phone='0900000002'");
            await RejectSql("UPDATE dbo.KHACH_HANG SET Email=N'unique@example.test' WHERE Phone='0900000003'", 2601, 2627);
        });
        await Check.Run("SQL rowversion prevents a stale EF writer from overwriting inventory", async () =>
        {
            await using var first = Open(); await using var second = Open();
            var current = await first.Inventory.SingleAsync(x => x.VariantId == variantId);
            var stale = await second.Inventory.SingleAsync(x => x.VariantId == variantId);
            current.OnHand = 6; await first.SaveChangesAsync();
            stale.OnHand = 7;
            try { await second.SaveChangesAsync(); throw new InvalidOperationException("Stale writer was accepted."); }
            catch (DbUpdateConcurrencyException) { }
            await using var verify = Open();
            Check.True((await verify.Inventory.SingleAsync()).OnHand == 6, "Stale writer changed stock.");
        });
        await Check.Run("Migration rejects altered checksums and legacy lineage without rewriting rows", async () =>
        {
            await using var db = Open();
            var hash = await db.Database.SqlQueryRaw<string>("SELECT Sha256 AS [Value] FROM dbo.__PhoneStoreMigrations").SingleAsync();
            await db.Database.ExecuteSqlRawAsync("UPDATE dbo.__PhoneStoreMigrations SET Sha256=REPLICATE('0',64)");
            Check.True(!await new DatabaseMigrator(db).IsCurrentAsync(), "Tampered schema was reported current.");
            var rejected = false;
            try { await new DatabaseMigrator(db).MigrateAsync(); } catch (InvalidOperationException) { rejected = true; }
            Check.True(rejected, "Altered checksum was accepted.");
            var foreignId = "legacy/001";
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.__PhoneStoreMigrations SET Sha256={hash},Id={foreignId}");
            rejected = false;
            try { await new DatabaseMigrator(db).MigrateAsync(); } catch (InvalidOperationException) { rejected = true; }
            Check.True(rejected && await db.Accounts.CountAsync() == 3, "Foreign lineage was rewritten or fixture accounts lost.");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.__PhoneStoreMigrations SET Id={DatabaseMigrator.MigrationId}");
            Check.True(await new DatabaseMigrator(db).IsCurrentAsync(), "Restored test ledger was not recognized.");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.__PhoneStoreMigrations(Id,Sha256) VALUES({foreignId},{hash})");
            Check.True(!await new DatabaseMigrator(db).IsCurrentAsync(), "An extra foreign migration row was reported current.");
            rejected = false;
            try { await new DatabaseMigrator(db).MigrateAsync(); } catch (InvalidOperationException) { rejected = true; }
            Check.True(rejected, "Mixed migration lineage was accepted.");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.__PhoneStoreMigrations WHERE Id={foreignId}");
        });
        await Check.Run("Migration refuses an unmanaged nonempty database and preserves its data", async () =>
        {
            await using var unmanaged = await TestDatabase.Create();
            await using var db = new StoreDbContext(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(unmanaged.ConnectionString).Options);
            await db.Database.ExecuteSqlRawAsync("CREATE TABLE dbo.KeepMe(Id int NOT NULL PRIMARY KEY); INSERT dbo.KeepMe VALUES(42)");
            var rejected = false;
            try { await new DatabaseMigrator(db).MigrateAsync(); } catch (InvalidOperationException) { rejected = true; }
            Check.True(rejected, "Unmanaged schema was accepted.");
            Check.True(await db.Database.SqlQueryRaw<int>("SELECT Id AS [Value] FROM dbo.KeepMe").SingleAsync() == 42, "Unmanaged data was altered.");
            Check.True(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE is_ms_shipped=0").SingleAsync() == 1, "Migration partially created tables in an unmanaged database.");
        });
        await HttpChecks.Run(connection);
    }
}
