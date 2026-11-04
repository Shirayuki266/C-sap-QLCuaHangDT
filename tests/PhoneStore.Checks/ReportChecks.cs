using System.Net;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class ReportChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"reports");await c.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),10);
        async Task<long> Complete(int quantity,DateTime at,bool paid=true)
        {
            var input=await CheckoutChecks.Cart(c,data,quantity);await using var db=c.Open();var k=c.Kernel(db);var w=new OrderWorkflow(k);var q=new OrderQueryService(k);var o=await new CheckoutService(k).Place(c.Customer,input);
            await w.Confirm(c.Admin,o.Id,new(o.Version,Guid.NewGuid()));o=await q.Read(c.Admin,o.Id,true);await w.Dispatch(c.Admin,o.Id,new(o.Version,Guid.NewGuid()));o=await q.Read(c.Admin,o.Id,true);await w.Complete(c.Admin,o.Id,new(o.Version,Guid.NewGuid()));
            // Deliberate imported inconsistency must be excluded by reporting.
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.DON_HANG SET CompletedAt={at} WHERE Id={o.Id}");
            if(!paid)await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.THANH_TOAN SET Status='CHUA_THU', Amount=0, CollectedAt=NULL, ActorId=NULL WHERE OrderId={o.Id}");return o.Id;
        }
        await Check.Run("A22 revenue uses paid completion dates at exact UTC+7 boundaries without header multiplication",async()=>
        {
            var id=await Complete(2,new DateTime(2020,1,1,17,0,0,DateTimeKind.Utc));await Complete(1,new DateTime(2020,1,2,17,0,0,DateTimeKind.Utc));await Complete(1,new DateTime(2020,1,2,16,59,59,DateTimeKind.Utc),false);
            await using var db=c.Open();var s=new ReportService(c.Kernel(db));var r=await s.Read(c.Admin,"revenue","2020-01-02","2020-01-02");var total=await db.Orders.Where(x=>x.Id==id).Select(x=>x.Total).SingleAsync();
            Check.True(r.Orders==1&&r.Revenue==total&&r.Daily.Single().Day==new DateOnly(2020,1,2)&&r.Bestsellers.Sum(x=>x.Revenue)==total&&r.Quantity==4,"Revenue boundary/paid/header calculation failed.");
            var before=await s.Read(c.Admin,"revenue","2019-12-31","2020-01-01");Check.True(before.Orders==0&&before.Revenue==0,"Completion-day filter used creation time.");
            await CommerceContext.Reject("INVALID_INPUT",async()=>await s.Read(c.Admin,"revenue","2020-02-01","2020-01-01"));
        });
        await Check.Run("A22 bestsellers sort by quantity; current stock ignores period and ADMIN role is enforced",async()=>
        {
            await using var db=c.Open();var s=new ReportService(c.Kernel(db));var best=await s.Read(c.Admin,"bestsellers","2020-01-02","2020-01-02");var stock=await s.Read(c.Admin,"stock","bad","reversed");
            Check.True(best.Bestsellers.Count==2&&best.Bestsellers.All(x=>x.Quantity==2)&&stock.From==null&&stock.Stock.All(x=>x.Available==x.OnHand-x.Reserved),"Bestseller or stock contract failed.");
            using var denied=await c.CustomerClient.Http.GetAsync("/api/admin/reports?kind=revenue");using var ok=await c.AdminClient.Http.GetAsync("/api/admin/reports?kind=revenue&from=2020-01-02&to=2020-01-02");Check.True(denied.StatusCode==HttpStatusCode.Forbidden&&ok.IsSuccessStatusCode,"Report HTTP role boundary failed.");
        });
    }
}
