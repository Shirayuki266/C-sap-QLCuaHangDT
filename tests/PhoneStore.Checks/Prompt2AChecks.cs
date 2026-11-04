using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Checks;

internal static class Prompt2AChecks
{
    public static async Task RunIsolated()
    {
        await using var database=await TestDatabase.Create();
        await using(var db=new PhoneStore.Infrastructure.Data.StoreDbContext(new DbContextOptionsBuilder<PhoneStore.Infrastructure.Data.StoreDbContext>().UseSqlServer(database.ConnectionString).Options))await new PhoneStore.Infrastructure.Data.DatabaseMigrator(db).MigrateAsync();
        await HttpChecks.Run(database.ConnectionString,prompt2aOnly:true);
    }
    public static async Task Run(Uri address,string connection)
    {
        using var first=new CommerceContext(connection,address,"prompt2a-first");using var second=new CommerceContext(connection,address,"prompt2a-second");
        await first.Initialize();await second.Initialize();
        var anchor=await PurchaseConfirmChecks.Receive(first,await CommerceData.Create(first),20);
        var fixture=new Prompt2AFixture(first,second,anchor);await fixture.Group1();await fixture.Group2();await fixture.Supplement();
    }
}

internal sealed partial class Prompt2AFixture(CommerceContext first,CommerceContext second,CommerceData anchor)
{
    private CommerceContext C{get;}=first;
    private CommerceContext Other{get;}=second;
    private StateMatrixFixture Core{get;}=new(first,anchor);
    private List<CommerceData> Filters{get;}=[];
    private async Task<CommerceData> Data(int quantity=20)=>await PurchaseConfirmChecks.Receive(C,await CommerceData.Create(C),quantity);
    private async Task Save(CommerceData data,Func<ProductInput,ProductInput> change)
    {
        await using var db=C.Open();var service=new ProductService(C.Kernel(db));var p=await service.Read(C.Admin,data.Product.Id);
        await service.Save(C.Admin,p.Id,change(ProductChecks.Input(p)));
    }
    private async Task<string> Snapshot()
    {
        // All snapshots stay in memory. Never print hashes, session IDs or profile data on assertion failure.
        var business=await Core.Snapshot();await using var db=C.Open();
        return JsonSerializer.Serialize(new{business,Accounts=await db.Accounts.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            Sessions=await db.Sessions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Customers=await db.Customers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            Products=await db.Products.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Images=await db.ProductImages.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            Brands=await db.Brands.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Categories=await db.Categories.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            Suppliers=await db.Suppliers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
    }
    private async Task Unchanged(string before)=>Check.True(before==await Snapshot(),"Prompt2A request changed the captured database.");
    private static async Task Error(HttpResponseMessage response,HttpStatusCode status,string code)
    {
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Check.True(response.StatusCode==status&&json.RootElement.GetProperty("code").GetString()==code,$"Prompt2A expected {(int)status}/{code}, received {(int)response.StatusCode}/{json.RootElement.GetProperty("code").GetString()}.");
    }
    private async Task FilterFixtures()
    {
        if(Filters.Count>0)return;
        for(var i=0;i<4;i++)Filters.Add(await Data());
        for(var i=0;i<4;i++)
        {
            var index=i;
            await Save(Filters[i],p=>p with{Name="P2A Filter "+(char)('A'+index),BrandId=Filters[0].Product.BrandId,
                Variants=p.Variants!.Select((v,n)=>v with{Color=n==0?"Black":"White",StorageGb=index==1?(n==0?256:128):(n==0?128:256),ProfitPercent=n==0?(index==2?140m:index==3?145m:20m):20m}).ToList()});
        }
    }
    private CatalogFilter Combined()=>new(Query:"P2A Filter",BrandId:Filters[0].Product.BrandId,MinPrice:2300m,MaxPrice:2500m,StorageGb:128,Color:"Black",Sort:"price-desc",PageSize:1);
}
