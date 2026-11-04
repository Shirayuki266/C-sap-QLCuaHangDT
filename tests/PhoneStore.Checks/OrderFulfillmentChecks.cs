using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class OrderFulfillmentChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"fulfillment");await c.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),5);
        async Task<OrderView> Shipping()
        {
            var input=await CheckoutChecks.Cart(c,data);await using var db=c.Open();var k=c.Kernel(db);var s=new OrderWorkflow(k);var q=new OrderQueryService(k);
            var order=await new CheckoutService(k).Place(c.Customer,input);await s.Confirm(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));order=await q.Read(c.Admin,order.Id,true);
            await s.Dispatch(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));return await q.Read(c.Admin,order.Id,true);
        }
        await Check.Run("A20 dispatch decrements physical/reserved once; completion atomically collects COD",async()=>
        {
            var order=await Shipping();await using var db=c.Open();var k=c.Kernel(db);var s=new OrderWorkflow(k);var q=new OrderQueryService(k);
            Check.True(await db.Inventory.Where(x=>data.Product.Variants.Select(v=>v.Id).Contains(x.VariantId)).AllAsync(x=>x.OnHand==4&&x.Reserved==0),"Dispatch inventory effects are incorrect.");
            await CommerceContext.Reject("INVALID_STATE",async()=>await s.Dispatch(c.Admin,order.Id,new(order.Version,Guid.NewGuid())));
            await s.Complete(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));var done=await q.Read(c.Admin,order.Id,true);var pay=await db.Payments.AsNoTracking().SingleAsync(x=>x.OrderId==order.Id);
            Check.True(done.Status=="HOAN_TAT"&&done.CompletedAt!=null&&pay.Amount==done.Total&&pay.Status=="DA_THU"&&pay.ActorId==c.Admin.AccountId&&pay.CollectedAt==done.CompletedAt.Value.UtcDateTime,"Completion and COD did not commit together.");
            await CommerceContext.Reject("INVALID_STATE",async()=>await s.Complete(c.Admin,order.Id,new(done.Version,Guid.NewGuid())));
        });
        await Check.Run("A20 failed delivery keeps stock out until explicit once-only physical return",async()=>
        {
            var order=await Shipping();await using var db=c.Open();var k=c.Kernel(db);var s=new OrderWorkflow(k);var q=new OrderQueryService(k);
            await s.Fail(c.Admin,order.Id,new(order.Version,Guid.NewGuid(),"Không liên lạc được"));var failed=await q.Read(c.Admin,order.Id,true);
            Check.True(await db.Inventory.Where(x=>data.Product.Variants.Select(v=>v.Id).Contains(x.VariantId)).AllAsync(x=>x.OnHand==3&&x.Reserved==0)&&failed.Payment=="CHUA_THU"&&!failed.ReturnReceived,"Failure silently restocked/collected COD.");
            await s.ReceiveReturn(c.Admin,order.Id,new(failed.Version,Guid.NewGuid(),"Kho đã nhận đủ máy"));var returned=await q.Read(c.Admin,order.Id,true);
            Check.True(returned.ReturnReceived&&returned.Status=="GIAO_THAT_BAI"&&await db.Inventory.Where(x=>data.Product.Variants.Select(v=>v.Id).Contains(x.VariantId)).AllAsync(x=>x.OnHand==4&&x.Reserved==0),"Physical return changed state or wrong inventory.");
            await CommerceContext.Reject("INVALID_STATE",async()=>await s.ReceiveReturn(c.Admin,order.Id,new(returned.Version,Guid.NewGuid(),"Repeat")));
        });
        await Check.Run("A20 late payment write failure rolls back completion, timestamps and history",async()=>
        {
            var order=await Shipping();await using var db=c.Open();var k=c.Kernel(db);var count=await db.OrderHistory.CountAsync(x=>x.OrderId==order.Id);
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @id bigint={order.Id}; DECLARE @ddl nvarchar(max)=N'ALTER TABLE dbo.THANH_TOAN ADD CONSTRAINT CK_QA_Completion CHECK (OrderId <> '+CONVERT(nvarchar(20),@id)+N' OR Status=''CHUA_THU'')'; EXEC(@ddl);");
            try
            {
                await CommerceContext.Reject("INVALID_INPUT",async()=>await new OrderWorkflow(k).Complete(c.Admin,order.Id,new(order.Version,Guid.NewGuid())));
                var current=await new OrderQueryService(k).Read(c.Admin,order.Id,true);
                Check.True(current.Status=="DANG_GIAO"&&current.CompletedAt==null&&current.Payment=="CHUA_THU"&&await db.OrderHistory.CountAsync(x=>x.OrderId==order.Id)==count,"Late COD error left partial completion.");
            }
            finally{await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.THANH_TOAN DROP CONSTRAINT CK_QA_Completion");}
        });
    }
}
