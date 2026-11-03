using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.DemoData.Cli;
using PhoneStore.DemoData.Generation;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.DemoData.Persistence;

public sealed class SimulationClock(DateTime current) : TimeProvider
{
    public DateTime Current { get; set; } = current;
    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Current, DateTimeKind.Utc));
}

public sealed class SeedStore(string connection)
{
    public StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>()
        .UseSqlServer(Options.GuardConnection(connection)).Options);

    public async Task Seed(DemoPlan plan, Secrets secrets, bool reset)
    {
        // Serialize competing tool instances, including reset, using a dedicated session lock.
        await using var lease = new SqlConnection(Options.GuardConnection(connection));
        await lease.OpenAsync();
        await using var acquire = lease.CreateCommand();
        acquire.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'PhoneStore:demo-data',@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=1000; IF @r<0 THROW 51000,N'Demo tool busy',1;";
        await acquire.ExecuteNonQueryAsync();
        try
        {
            await using var db = Open();
            if (!await new DatabaseMigrator(db).IsCurrentAsync())
                throw new InvalidOperationException("Cần migrate riêng CSDL mới bằng migration hiện có trước khi seed.");
            var audit = await new DataAudit(db).Run();
            if (audit.Errors.Any(x => x is "Unmanaged schema tables" or "Disabled/untrusted constraints"))
                throw new InvalidOperationException("Không reset/seed schema lạ hoặc constraint không đáng tin cậy.");
            var occupied = audit.Counts.Any(x => x.Key != "__PhoneStoreMigrations" && x.Value != 0);
            if (occupied && !reset) throw new InvalidOperationException("CSDL đã có dữ liệu; cần --reset chủ động trên CSDL demo/QA hợp lệ.");
            var clock = new SimulationClock(plan.Start.AddDays(-1));
            var kernel = new BusinessKernel(db, clock);
            if (occupied)
            {
                // Static identifiers, FK order, and one transaction. No DROP, migration, or constraint disabling.
                await kernel.Write(async () =>
                {
                    await db.Database.ExecuteSqlRawAsync("""
                        DELETE FROM dbo.NHAT_KY_KHO; DELETE FROM dbo.LICH_SU_DON;
                        DELETE FROM dbo.THANH_TOAN; DELETE FROM dbo.DON_HANG_CT; DELETE FROM dbo.DON_HANG;
                        DELETE FROM dbo.PHIEU_NHAP_CT; DELETE FROM dbo.PHIEU_NHAP;
                        DELETE FROM dbo.GIO_HANG_CT; DELETE FROM dbo.TON_KHO;
                        DELETE FROM dbo.SAN_PHAM_ANH; DELETE FROM dbo.BIEN_THE; DELETE FROM dbo.SAN_PHAM;
                        DELETE FROM dbo.NHA_CUNG_CAP; DELETE FROM dbo.HANG; DELETE FROM dbo.DANH_MUC;
                        DELETE FROM dbo.PHIEN; DELETE FROM dbo.KHACH_HANG; DELETE FROM dbo.TAI_KHOAN;
                        """);
                    return true;
                });
            }
            var hasher = new PasswordHasher<Account>();
            var adminId = await new AdminProvisioner(db, hasher).Create("demo.admin.large", secrets.Admin);
            await SetCreatedAt(db, adminId, clock.Current);
            // AccountRegistration owns its own transactions; it must not be nested in kernel.Write.
            foreach (var customer in plan.Customers)
            {
                var registered = await new AccountRegistration(db, hasher).RegisterAsync(new(customer.Username,
                    secrets.Customer, customer.Name, customer.Phone, customer.Email, customer.Address));
                await SetCreatedAt(db, registered.Id, customer.CreatedAt);
            }
            var sessions = new AccountSessions(db, hasher, clock);
            var session = await sessions.LoginAsync(new("demo.admin.large", secrets.Admin, "admin"));
            var actor = new Actor(adminId, session.SessionId);
            try
            {
                await kernel.Write(async () =>
                {
                    var lookups = new LookupService(kernel);
                    var brands = new List<int>();
                    var categories = new List<int>();
                    foreach (var name in plan.Brands) { brands.Add(await lookups.Save(actor, "brands", 0, new(name))); db.ChangeTracker.Clear(); }
                    foreach (var name in plan.Categories) { categories.Add(await lookups.Save(actor, "categories", 0, new(name))); db.ChangeTracker.Clear(); }
                    foreach (var supplier in plan.Suppliers)
                    {
                        await new SupplierService(kernel).Save(actor, 0, new(supplier.Code, supplier.Name, supplier.Phone, supplier.Address));
                        db.ChangeTracker.Clear();
                    }
                    foreach (var product in plan.Products)
                    {
                        await new ProductService(kernel).Save(actor, 0, new(product.Code, product.Name,
                            brands[product.Brand], categories[product.Category], product.Specifications, product.Description,
                            SvgAssets.Url(product.Code, 1), true,
                            product.Variants.Select(v => new VariantInput(0, v.Sku, v.Color, v.StorageGb, v.Cost, v.ProfitPercent)).ToList(),
                            Enumerable.Range(1, 3).Select(v => new ImageInput(SvgAssets.Url(product.Code, v), v - 1)).ToList()));
                        db.ChangeTracker.Clear();
                    }
                    foreach (var customer in plan.Customers.Where(x => x.Locked))
                    {
                        var id = await db.Accounts.Where(x => x.Username == customer.Username).Select(x => x.Id).SingleAsync();
                        await new CustomerAdminService(kernel).SetActive(actor, id, new(false, true));
                        db.ChangeTracker.Clear();
                    }
                    return true;
                });
            }
            finally
            {
                // The demo must contain no simulation sessions, even if catalog generation fails.
                db.ChangeTracker.Clear();
                await kernel.Write(async () => { await db.Sessions.ExecuteDeleteAsync(); return true; });
            }
            var finalAudit = await new DataAudit(db).Run();
            if (!finalAudit.Valid) throw new InvalidOperationException("Audit dữ liệu nền thất bại.");
        }
        finally
        {
            await using var release = lease.CreateCommand();
            release.CommandText = "EXEC sys.sp_releaseapplock @Resource=N'PhoneStore:demo-data',@LockOwner='Session';";
            await release.ExecuteNonQueryAsync();
        }
    }

    private static async Task SetCreatedAt(StoreDbContext db, long id, DateTime createdAt)
    {
        // Only approved direct business-data update: historical account timestamps.
        await db.Accounts.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAt, createdAt));
        db.ChangeTracker.Clear();
    }
}
