using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal sealed record MvcOrdersFixture(MvcCatalogFixture Catalog, Actor Other, long OtherOrder, List<long> Created,
    long CancelId, long RaceId)
{
    public static async Task<MvcOrdersFixture> Seed(IServiceProvider services)
    {
        var data=await MvcCatalogFixture.Seed(services);var store=services.GetRequiredService<IStoreService>();
        var registration=services.GetRequiredService<IAccountRegistration>();await registration.RegisterAsync(new("orders-other-qa",MvcCatalogFixture.Password,"Khách QA đơn khác","0938123597"));
        var login=await services.GetRequiredService<IAccountSessions>().LoginAsync(new("orders-other-qa",MvcCatalogFixture.Password));var other=new Actor(login.Account.Id,login.SessionId);
        async Task<long> Place(Actor actor,long variant,int quantity,string name)
        {
            await store.UpdateCartAsync(actor,variant,quantity);var cart=await store.ReadCartAsync(actor);
            return await store.PlaceOrderAsync(actor,new(Guid.NewGuid(),name,"0938123501","Địa chỉ giao QA",cart.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price!.Value)).ToList()));
        }
        var foreign=await Place(other,data.Black128,1,"Khách riêng QA");var own=new List<long>();
        for(var i=0;i<8;i++)own.Add(await Place(data.Customer,i%2==0?data.Black128:data.Black256,1,"Người nhận đơn QA "+i));
        var workflow=services.GetRequiredService<OrderWorkflow>();var query=services.GetRequiredService<OrderQueryService>();
        async Task<VersionedCommand> Command(long id,string? reason=null)=>new((await query.Read(data.Admin,id,true)).Version,Guid.NewGuid(),reason);
        foreach(var index in new[]{2,3,4,5})
        {
            var id=own[index];await workflow.Confirm(data.Admin,id,await Command(id));
            if(index!=2)await workflow.Dispatch(data.Admin,id,await Command(id));
            if(index==4)await workflow.Fail(data.Admin,id,await Command(id,"Giao thất bại QA <script>alert(1)</script>"));
            if(index==5)await workflow.Complete(data.Admin,id,await Command(id));
        }
        await workflow.Cancel(data.Customer,own[7],await Command(own[7],new string('A',1000)));
        // Deterministic UTC+7 date-window boundaries, assigned only in this QA fixture before any assertions.
        var dates=new[]{new DateTime(2025,1,11,16,59,59,DateTimeKind.Utc),new DateTime(2025,1,11,17,0,0,DateTimeKind.Utc),
            new DateTime(2025,1,12,16,59,59,DateTimeKind.Utc),new DateTime(2025,1,12,17,0,0,DateTimeKind.Utc)};
        var db=services.GetRequiredService<StoreDbContext>();
        for(var i=0;i<4;i++)await db.Orders.Where(x=>x.Id==own[i]).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CreatedAt,dates[i]));
        return new(data,other,foreign,own,own[0],own[1]);
    }
}
