using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Checks;
internal static class DemoSeedChecks
{
    public const string AdminPassword="DemoCheckOnly@Strong12345";
    public const string CustomerPassword="DemoCustomerOnly@Strong12345";
    public static async Task Run(string connection)
    {
        var target=new SqlConnectionStringBuilder(connection);var database="PhoneStore_QA_Demo_"+Guid.NewGuid().ToString("N");
        var masterTarget=new SqlConnectionStringBuilder(connection){InitialCatalog="master"};await using var master=new SqlConnection(masterTarget.ConnectionString);await master.OpenAsync();
        target.InitialCatalog=database;
        StoreDbContext Open()=>new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(target.ConnectionString).Options);
        try
        {
            await using(var db=Open())await new DatabaseMigrator(db).MigrateAsync();
            await Check.Run("Demo seed late failure rolls back accounts, stock, purchases and orders together",async()=>
            {
                await using var db=Open();await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.THANH_TOAN ADD CONSTRAINT CK_QA_SeedLate CHECK (OrderId<0)");
                try
                {
                    var failed=false;try{await new DemoSeeder(db,new PasswordHasher<Account>()).Seed(AdminPassword,CustomerPassword);}catch(BusinessException e)when(e.Code=="INVALID_INPUT"){failed=true;}
                    var audit=await new DataAudit(db).Run();Check.True(failed&&audit.Valid&&audit.Counts.Where(x=>x.Key!="__PhoneStoreMigrations").All(x=>x.Value==0),"Late seed failure left partial data.");
                }
                finally{await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.THANH_TOAN DROP CONSTRAINT CK_QA_SeedLate");}
            });
            await Check.Run("Demo dataset contains 20 products/customers with fully reconciled stock/COD/history",async()=>
            {
                await using var db=Open();var seeder=new DemoSeeder(db,new PasswordHasher<Account>());var result=await seeder.Seed(AdminPassword,CustomerPassword);
                Check.True(result.Valid&&DemoSeeder.ExpectedCounts.All(x=>result.Counts[x.Key]==x.Value),"Sample manifest/audit mismatch: "+string.Join(",",result.Errors));
                Check.True(await db.Inventory.SumAsync(x=>x.OnHand)==884&&await db.Inventory.SumAsync(x=>x.Reserved)==12&&await db.Payments.CountAsync(x=>x.Status=="DA_THU")==3,"Sample balances or paid scenarios differ.");
                var hash=await db.Accounts.Where(x=>x.Username==DemoSeeder.AdminUsername).Select(x=>x.PasswordHash).SingleAsync();
                var second=await seeder.Seed(AdminPassword+"different",CustomerPassword+"different");Check.True(second.Valid&&await db.Accounts.Where(x=>x.Username==DemoSeeder.AdminUsername).Select(x=>x.PasswordHash).SingleAsync()==hash,"Rerun duplicated/reset data or password.");
                await db.Inventory.Where(x=>x.VariantId==db.Inventory.Min(v=>v.VariantId)).ExecuteUpdateAsync(x=>x.SetProperty(v=>v.Reserved,v=>v.Reserved+1));
                Check.True(!(await new DataAudit(db).Run()).Valid,"Audit missed a valid-SQL but inconsistent reservation.");
                await db.Inventory.Where(x=>x.VariantId==db.Inventory.Min(v=>v.VariantId)).ExecuteUpdateAsync(x=>x.SetProperty(v=>v.Reserved,v=>v.Reserved-1));
            });
            await HttpChecks.Run(target.ConnectionString,demoOnly:true);
            await using(var db=Open())Check.True((await new DataAudit(db).Run()).Valid,"Seeded HTTP writes broke business integrity.");
            await Check.Run("Explicit admin provisioning hashes secrets and refuses overwrites; seed refuses unrelated data",async()=>
            {
                await using var db=Open();var service=new AdminProvisioner(db,new PasswordHasher<Account>());var id=await service.Create("provision.check",AdminPassword);
                var account=await db.Accounts.AsNoTracking().SingleAsync(x=>x.Id==id);Check.True(account.Role=="ADMIN"&&!await db.Customers.AnyAsync(x=>x.Id==id)&&new PasswordHasher<Account>().VerifyHashedPassword(account,account.PasswordHash,AdminPassword)!=PasswordVerificationResult.Failed,"Provisioning did not hash/limit role.");
                var refused=false;try{await service.Create("provision.check",CustomerPassword);}catch(BusinessException e)when(e.Code=="INVALID_INPUT"){refused=true;}Check.True(refused,"Provisioning overwrote an account.");
                var count=await db.Accounts.CountAsync();refused=false;try{await new DemoSeeder(db,new PasswordHasher<Account>()).Seed(AdminPassword,CustomerPassword);}catch(InvalidOperationException){refused=true;}
                Check.True(refused&&await db.Accounts.CountAsync()==count,"Seed modified unrelated existing data.");
            });
        }
        finally
        {
            SqlConnection.ClearAllPools();await using var drop=master.CreateCommand();drop.CommandText=$"IF DB_ID('{database}') IS NOT NULL DROP DATABASE [{database}]";await drop.ExecuteNonQueryAsync();
        }
    }
}
