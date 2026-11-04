using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Checks;
internal static class ChecklistP0Checks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"source-p0");await c.Initialize();
        async Task<CommerceData> Fixture(int variants=1)
        {
            var data=await CommerceData.Create(c,variants);await using var db=c.Open();var k=c.Kernel(db);var s=new PurchaseService(k);
            var id=await s.Save(c.Admin,0,new(data.SupplierId,data.Product.Variants.Select(v=>new PurchaseLineInput(v.Id,10,1000000m)).ToList(),RequestKey:Guid.NewGuid()));
            var draft=await s.Read(c.Admin,id);await s.Confirm(c.Admin,id,new(draft.Version,Guid.NewGuid()));
            return data with{Product=await new ProductService(k).Read(c.Admin,data.Product.Id)};
        }
        async Task Balance(CommerceData data,int h,int r)
        {
            await using var db=c.Open();var ids=data.Product.Variants.Select(x=>x.Id).ToArray();var rows=await db.Inventory.AsNoTracking().Where(x=>ids.Contains(x.VariantId)).ToListAsync();
            Check.True(rows.Count==ids.Length&&rows.All(x=>x.OnHand==h&&x.Reserved==r&&x.OnHand>=x.Reserved&&x.Reserved>=0),$"Expected each SKU H/R/A={h}/{r}/{h-r}.");
        }
        async Task<OrderView> Create(CommerceData data,int quantity=2)
        {
            var input=await CheckoutChecks.Cart(c,data,quantity);await using var db=c.Open();return await new CheckoutService(c.Kernel(db)).Place(c.Customer,input);
        }
        async Task<OrderView> Read(long id)
        {await using var db=c.Open();return await new OrderQueryService(c.Kernel(db)).Read(c.Admin,id,true);}
        async Task<OrderView> Act(OrderView o,string action)
        {
            await using var db=c.Open();var w=new OrderWorkflow(c.Kernel(db));var cmd=new VersionedCommand(o.Version,Guid.NewGuid(),"Kiểm chứng checklist");
            switch(action){case "confirm":await w.Confirm(c.Admin,o.Id,cmd);break;case "dispatch":await w.Dispatch(c.Admin,o.Id,cmd);break;case "complete":await w.Complete(c.Admin,o.Id,cmd);break;case "fail":await w.Fail(c.Admin,o.Id,cmd);break;case "return":await w.ReceiveReturn(c.Admin,o.Id,cmd);break;case "customer-cancel":await w.Cancel(c.Customer,o.Id,cmd);break;case "admin-cancel":await w.Cancel(c.Admin,o.Id,cmd,true);break;}
            return await Read(o.Id);
        }
        await Check.Run("FLOW.01 DEC.05 INV.01-04 exact COD flow 10/0/10 to 8/0/8 and 2400000 collected",async()=>
        {
            var data=await Fixture();await Balance(data,10,0);var input=await CheckoutChecks.Cart(c,data,2);await Balance(data,10,0);
            await using var db=c.Open();var k=c.Kernel(db);var reports=new ReportService(k);var before=(await reports.Read(c.Admin,"revenue")).Revenue;
            var o=await new CheckoutService(k).Place(c.Customer,input);await Balance(data,10,2);
            Check.True(o.Status=="CHO_XAC_NHAN"&&o.Payment=="CHUA_THU"&&o.Total==2400000m&&o.Lines.Single().Price==1200000m,"Checkout snapshots/payment differ.");
            o=await Act(o,"confirm");await Balance(data,10,2);Check.True(o.Payment=="CHUA_THU","Confirmation collected COD.");
            o=await Act(o,"dispatch");await Balance(data,8,0);Check.True((await reports.Read(c.Admin,"revenue")).Revenue==before,"Shipping was included in revenue.");
            o=await Act(o,"complete");await Balance(data,8,0);var pay=await db.Payments.AsNoTracking().SingleAsync(x=>x.OrderId==o.Id);
            Check.True(o.Status=="HOAN_TAT"&&pay.Status=="DA_THU"&&pay.Amount==2400000m&&pay.CollectedAt==o.CompletedAt!.Value.UtcDateTime&&(await reports.Read(c.Admin,"revenue")).Revenue==before+2400000m,"Completion/revenue did not reconcile.");
        });
        await Check.Run("FLOW.02 K15.01-04 customer cancellation releases only reservations once",async()=>
        {
            var data=await Fixture();var o=await Create(data);await Balance(data,10,2);o=await Act(o,"customer-cancel");await Balance(data,10,0);
            await CommerceContext.Reject("INVALID_STATE",async()=>await Act(o,"customer-cancel"));await Balance(data,10,0);Check.True(o.Payment=="CHUA_THU","Cancel collected COD.");
        });
        await Check.Run("FLOW.03 A19.02-03 confirmed administrative cancellation cannot later dispatch",async()=>
        {
            var data=await Fixture();var o=await Act(await Create(data),"confirm");o=await Act(o,"admin-cancel");await Balance(data,10,0);
            await CommerceContext.Reject("INVALID_STATE",async()=>await Act(o,"dispatch"));await Balance(data,10,0);
        });
        await Check.Run("FLOW.04 DEC.05 A20.03-05 failed delivery stays out until once-only physical return",async()=>
        {
            var data=await Fixture();var o=await Act(await Act(await Create(data),"confirm"),"dispatch");await Balance(data,8,0);
            o=await Act(o,"fail");await Balance(data,8,0);Check.True(o.Payment=="CHUA_THU"&&!o.ReturnReceived,"Failed delivery collected/restocked.");
            o=await Act(o,"return");await Balance(data,10,0);Check.True(o.Status=="GIAO_THAT_BAI"&&o.ReturnReceived,"Return invented a new order state.");
            await CommerceContext.Reject("INVALID_STATE",async()=>await Act(o,"return"));await Balance(data,10,0);
        });
        await Check.Run("FLOW.05 A14.04 A15.01/03 draft keeps cost and confirmation reprices once",async()=>
        {
            var data=await Fixture();await using var db=c.Open();var k=c.Kernel(db);var s=new PurchaseService(k);var v=data.Product.Variants[0];
            var id=await s.Save(c.Admin,0,new(data.SupplierId,[new(v.Id,3,1100000m)],RequestKey:Guid.NewGuid()));await Balance(data,10,0);
            var current=(await new ProductService(k).Read(c.Admin,data.Product.Id)).Variants.Single();Check.True(current.Cost==1000000m&&current.Price==1200000m,"Draft repriced catalog.");
            var draft=await s.Read(c.Admin,id);await s.Confirm(c.Admin,id,new(draft.Version,Guid.NewGuid()));await Balance(data,13,0);
            current=(await new ProductService(k).Read(c.Admin,data.Product.Id)).Variants.Single();Check.True(current.Cost==1100000m&&current.Price==1320000m,"Confirmed cost/price differ.");
            draft=await s.Read(c.Admin,id);await CommerceContext.Reject("INVALID_STATE",async()=>await s.Confirm(c.Admin,id,new(draft.Version,Guid.NewGuid())));await Balance(data,13,0);
        });
        await Check.Run("FLOW.06 K12.02 K14.03 snapshot price remains 1200000 after live price becomes 1320000",async()=>
        {
            var data=await Fixture();var old=await Create(data);var input=await CheckoutChecks.Cart(c,data,1);await using var db=c.Open();var k=c.Kernel(db);var s=new PurchaseService(k);
            var id=await s.Save(c.Admin,0,new(data.SupplierId,[new(data.Product.Variants[0].Id,3,1100000m)],RequestKey:Guid.NewGuid()));var draft=await s.Read(c.Admin,id);await s.Confirm(c.Admin,id,new(draft.Version,Guid.NewGuid()));
            var changed=false;try{await new CheckoutService(k).Place(c.Customer,input);}catch(PriceChangedException e){changed=e.Cart.Lines.Single().Price==1320000m;}
            Check.True(changed&&!await db.Orders.AnyAsync(x=>x.RequestKey==input.RequestKey)&&(await Read(old.Id)).Lines.Single().Price==1200000m,"Repricing changed snapshots or accepted stale confirmation.");await Balance(data,13,2);
        });
        await Check.Run("FLOW.07 A17.01-03 physical counts below/equal/above reservations use absolute values",async()=>
        {
            var data=await Fixture();await Create(data,3);await using var db=c.Open();var k=c.Kernel(db);var s=new InventoryService(k);var vid=data.Product.Variants[0].Id;
            async Task<string> Version()=>Convert.ToBase64String((await db.Inventory.AsNoTracking().SingleAsync(x=>x.VariantId==vid)).Version);
            await CommerceContext.Reject("INVALID_INPUT",async()=>await s.Adjust(c.Admin,vid,new(2,"Count",await Version(),Guid.NewGuid())));await Balance(data,10,3);
            await s.Adjust(c.Admin,vid,new(3,"Count",await Version(),Guid.NewGuid()));await Balance(data,3,3);
            await s.Adjust(c.Admin,vid,new(8,"Count",await Version(),Guid.NewGuid()));await Balance(data,8,3);
        });
        await Check.Run("FLOW.08 RACE.10 A20.06 late two-SKU dispatch journal failure rolls back and retries once",async()=>
        {
            var data=await Fixture(2);var o=await Act(await Create(data),"confirm");await using var db=c.Open();var k=c.Kernel(db);var w=new OrderWorkflow(k);var cmd=new VersionedCommand(o.Version,Guid.NewGuid());
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @id bigint={o.Id}; DECLARE @sql nvarchar(max)=N'ALTER TABLE dbo.NHAT_KY_KHO ADD CONSTRAINT CK_QA_DispatchLate CHECK (OrderId<>'+CONVERT(nvarchar(20),@id)+N' OR Kind<>''XUAT_GIAO'')'; EXEC(@sql);");
            try{await CommerceContext.Reject("INVALID_INPUT",async()=>await w.Dispatch(c.Admin,o.Id,cmd));await Balance(data,10,2);var unchanged=await Read(o.Id);Check.True(unchanged.Status=="DA_XAC_NHAN"&&unchanged.Version==o.Version&&unchanged.History.Count==2&&!await db.Movements.AnyAsync(x=>x.OrderId==o.Id&&x.Kind=="XUAT_GIAO"),"Late dispatch error left partial effects.");}
            finally{await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.NHAT_KY_KHO DROP CONSTRAINT CK_QA_DispatchLate");}
            await w.Dispatch(c.Admin,o.Id,cmd);await Balance(data,8,0);Check.True(await db.Movements.CountAsync(x=>x.OrderId==o.Id&&x.Kind=="XUAT_GIAO")==2,"Retry did not journal both SKUs exactly once.");
        });
        foreach(var returning in new[]{false,true})await Check.Run(returning?"RACE.05 DEC.05 two physical returns restore each SKU once":"RACE.04 DEC.05 two dispatches reduce each SKU once",async()=>
        {
            var data=await Fixture();var o=await Act(await Create(data),"confirm");if(returning)o=await Act(await Act(o,"dispatch"),"fail");
            async Task<bool> Attempt(){try{await Act(o,returning?"return":"dispatch");return true;}catch(BusinessException e)when(e.Code is "CONCURRENT_UPDATE" or "INVALID_STATE"){return false;}}
            var results=await Task.WhenAll(Attempt(),Attempt());Check.True(results.Count(x=>x)==1,"Duplicate operation had two winners.");await Balance(data,returning?10:8,0);
        });
        await Check.Run("RACE.07 A17.04 count versus checkout cannot commit Reserved above OnHand",async()=>
        {
            var data=await Fixture();await using(var d=c.Open()){var k=c.Kernel(d);var openingRow=await d.Inventory.AsNoTracking().SingleAsync(x=>x.VariantId==data.Product.Variants[0].Id);await new InventoryService(k).Adjust(c.Admin,openingRow.VariantId,new(1,"Opening count",Convert.ToBase64String(openingRow.Version),Guid.NewGuid()));}
            var input=await CheckoutChecks.Cart(c,data,1);string version;await using(var d=c.Open())version=Convert.ToBase64String((await d.Inventory.AsNoTracking().SingleAsync(x=>x.VariantId==data.Product.Variants[0].Id)).Version);
            async Task<bool> Attempt(bool count){await using var d=c.Open();var k=c.Kernel(d);try{if(count)await new InventoryService(k).Adjust(c.Admin,data.Product.Variants[0].Id,new(0,"Concurrent count",version,Guid.NewGuid()));else await new CheckoutService(k).Place(c.Customer,input);return true;}catch(BusinessException e)when(e.Code is "INVALID_INPUT" or "CONCURRENT_UPDATE" or "OUT_OF_STOCK"){return false;}}
            var results=await Task.WhenAll(Attempt(true),Attempt(false));Check.True(results.Count(x=>x)==1,"Count and last-unit checkout both won.");await using var db=c.Open();var row=await db.Inventory.AsNoTracking().SingleAsync(x=>x.VariantId==data.Product.Variants[0].Id);Check.True((row.OnHand==0&&row.Reserved==0)||(row.OnHand==1&&row.Reserved==1),"Invalid count/reservation outcome.");
        });
        await Check.Run("RACE.08 A15.05 receipt versus percentage edit follows a valid serialized cost/price outcome",async()=>
        {
            var data=await Fixture();long id;string version;
            await using(var db=c.Open()){var k=c.Kernel(db);var s=new PurchaseService(k);id=await s.Save(c.Admin,0,new(data.SupplierId,[new(data.Product.Variants[0].Id,1,1100000m)],RequestKey:Guid.NewGuid()));version=(await s.Read(c.Admin,id)).Version;}
            async Task<bool> Attempt(bool receipt){await using var db=c.Open();var k=c.Kernel(db);try{if(receipt)await new PurchaseService(k).Confirm(c.Admin,id,new(version,Guid.NewGuid()));else{var input=ProductChecks.Input(data.Product);await new ProductService(k).Save(c.Admin,data.Product.Id,input with{Variants=input.Variants!.Select(x=>x with{ProfitPercent=30}).ToList()});}return true;}catch(BusinessException e)when(e.Code=="CONCURRENT_UPDATE"){return false;}}
            var results=await Task.WhenAll(Attempt(true),Attempt(false));Check.True(results[0],"Receipt did not commit.");await using var d=c.Open();var product=await new ProductService(c.Kernel(d)).Read(c.Admin,data.Product.Id);var v=product.Variants.Single();Check.True(v.Cost==1100000m&&v.ProfitPercent is 20m or 30m&&v.Price==Rules.Price(v.Cost,v.ProfitPercent),"Lost price/cost update.");await Balance(data,11,0);
        });
    }
}
