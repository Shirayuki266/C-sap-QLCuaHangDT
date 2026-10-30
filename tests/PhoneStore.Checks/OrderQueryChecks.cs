using System.Net;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class OrderQueryChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"order-queries");await c.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c));var input=await CheckoutChecks.Cart(c,data);
        await using var db=c.Open();var k=c.Kernel(db);var order=await new CheckoutService(k).Place(c.Customer,input);var s=new OrderQueryService(k);
        await Check.Run("K13/K14/A12/A18 own snapshots, admin customer filters and nonowner 404 are accurate",async()=>
        {
            Check.True((await s.Read(c.Customer,order.Id)).Lines.Count==2&&(await s.List(c.Admin,new(CustomerId:c.Customer.AccountId),true)).Items.Single().Id==order.Id,"Order queries/snapshots incorrect.");
            using var other=new CommerceContext(connection,address,"order-query-other");await other.Initialize();
            await CommerceContext.Reject("NOT_FOUND",async()=>await s.Read(other.Customer,order.Id));
            using var forbidden=await other.CustomerClient.Http.GetAsync($"/api/orders/{order.Id}");Check.True(forbidden.StatusCode==HttpStatusCode.NotFound,"Order IDOR leaked data.");
            Check.True((await s.List(other.Customer,new())).Total==0,"Another customer saw private orders.");
        });
        await Check.Run("Order date filters use UTC+7 boundaries and reject malformed/reversed extremes",async()=>
        {
            var boundary=new DateTime(2020,1,1,17,0,0,DateTimeKind.Utc);await db.Orders.Where(x=>x.Id==order.Id).ExecuteUpdateAsync(x=>x.SetProperty(o=>o.CreatedAt,boundary));
            Check.True((await s.List(c.Customer,new(From:"2020-01-02",To:"2020-01-02"))).Total==1&&(await s.List(c.Customer,new(To:"2020-01-01"))).Total==0,"UTC+7 boundary failed.");
            await CommerceContext.Reject("INVALID_INPUT",async()=>await s.List(c.Admin,new(From:"2020-02-01",To:"2020-01-01"),true));
            await CommerceContext.Reject("INVALID_INPUT",async()=>await s.List(c.Admin,new(From:"0001-01-01"),true));
            await CommerceContext.Reject("INVALID_INPUT",async()=>await s.List(c.Admin,new(To:"9999-12-31"),true));
        });
    }
}
