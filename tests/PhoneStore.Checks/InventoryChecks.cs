using System.Net;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class InventoryChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"inventory");await c.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c));
        await using var db=c.Open();var s=new InventoryService(c.Kernel(db));var v=data.Product.Variants[0];
        await Check.Run("A16/A17 counts preserve reservations and write the exact physical delta once",async()=>
        {
            await db.Inventory.Where(x=>x.VariantId==v.Id).ExecuteUpdateAsync(x=>x.SetProperty(t=>t.Reserved,2));
            var stock=(await s.List(c.Admin,data.Product.Code)).Items.Single(x=>x.VariantId==v.Id);
            await CommerceContext.Reject("INVALID_INPUT",async()=>await s.Adjust(c.Admin,v.Id,new(1,"Kiểm kê",stock.Version,Guid.NewGuid())));
            await CommerceContext.Reject("INVALID_INPUT",async()=>await s.Adjust(c.Admin,v.Id,new(12,"",stock.Version,Guid.NewGuid())));
            var key=Guid.NewGuid();await s.Adjust(c.Admin,v.Id,new(12,"Kiểm kê thực tế",stock.Version,key));
            var current=(await s.List(c.Admin,data.Product.Code)).Items.Single(x=>x.VariantId==v.Id);
            Check.True(current.OnHand==12&&current.Reserved==2&&current.Available==10,"Count corrupted balance.");
            var movement=(await s.Movements(c.Admin,operationKey:key)).Items.Single();Check.True(movement.DeltaOnHand==2&&movement.DeltaReserved==0,"Journal delta incorrect.");
            await CommerceContext.Reject("CONCURRENT_UPDATE",async()=>await s.Adjust(c.Admin,v.Id,new(13,"Stale",stock.Version,Guid.NewGuid())));
            await CommerceContext.Reject("INVALID_STATE",async()=>await s.Adjust(c.Admin,v.Id,new(13,"Retry",current.Version,key)));
        });
        await Check.Run("A16 inventory is ADMIN-only with real filters and bounded pages",async()=>
        {
            using var denied=await c.CustomerClient.Http.GetAsync("/api/admin/stock");
            using var page=await c.AdminClient.Http.GetAsync("/api/admin/stock?pageSize=1&query="+Uri.EscapeDataString(data.Product.Code));
            Check.True(denied.StatusCode==HttpStatusCode.Forbidden&&page.IsSuccessStatusCode,"Inventory role/filter query failed.");
        });
    }
}
