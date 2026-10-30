using System.Net;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class OrderDecisionChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"decisions");await c.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),3);var input=await CheckoutChecks.Cart(c,data);
        await using var db=c.Open();var k=c.Kernel(db);var order=await new CheckoutService(k).Place(c.Customer,input);var queries=new OrderQueryService(k);var s=new OrderWorkflow(k);
        await Check.Run("K15/A19 confirmation leaves COD unpaid; admin cancellation releases only reservations",async()=>
        {
            await CommerceContext.Reject("INVALID_INPUT",async()=>await s.Cancel(c.Customer,order.Id,new(order.Version,Guid.NewGuid())));
            await s.Confirm(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));var confirmed=await queries.Read(c.Admin,order.Id,true);
            Check.True(confirmed.Payment=="CHUA_THU"&&confirmed.Status=="DA_XAC_NHAN","Confirmation collected COD.");
            await CommerceContext.Reject("INVALID_STATE",async()=>await s.Cancel(c.Customer,order.Id,new(confirmed.Version,Guid.NewGuid(),"No longer pending")));
            await s.Cancel(c.Admin,order.Id,new(confirmed.Version,Guid.NewGuid(),"Hủy trước xuất giao"),true);
            var final=await queries.Read(c.Customer,order.Id);
            Check.True(final.Status=="DA_HUY"&&await db.Inventory.Where(x=>data.Product.Variants.Select(v=>v.Id).Contains(x.VariantId)).AllAsync(x=>x.OnHand==3&&x.Reserved==0),"Cancellation changed physical stock or failed to release holds.");
            await CommerceContext.Reject("INVALID_STATE",async()=>await s.Cancel(c.Customer,order.Id,new(final.Version,Guid.NewGuid(),"Repeat")));
            using var denied=await c.CustomerClient.Send($"/api/admin/orders/{order.Id}/confirm",new VersionedCommand(final.Version,Guid.NewGuid()));
            Check.True(denied.StatusCode==HttpStatusCode.Forbidden,"Customer confirmed an admin order.");
        });
        await Check.Run("K15/A19 independent cancellation-versus-confirmation race preserves one valid outcome",async()=>
        {
            var request=await CheckoutChecks.Cart(c,data);var created=await new CheckoutService(k).Place(c.Customer,request);
            async Task<bool> Attempt(bool admin){await using var d=c.Open();var flow=new OrderWorkflow(c.Kernel(d));try{if(admin)await flow.Confirm(c.Admin,created.Id,new(created.Version,Guid.NewGuid()));else await flow.Cancel(c.Customer,created.Id,new(created.Version,Guid.NewGuid(),"Đổi nhu cầu"));return true;}catch(BusinessException e)when(e.Code is "CONCURRENT_UPDATE" or "INVALID_STATE"){return false;}}
            var results=await Task.WhenAll(Attempt(false),Attempt(true));var current=await queries.Read(c.Admin,created.Id,true);
            Check.True(results.Count(x=>x)==1&&current.Status is "DA_HUY" or "DA_XAC_NHAN","Order race overwrote the winning state.");
        });
    }
}
