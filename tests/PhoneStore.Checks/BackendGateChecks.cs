using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class BackendGateChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"backend-gate");await c.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),8);
        await Check.Run("A06 metadata-only edits advance every variant version and reject concurrent stale forms",async()=>
        {
            await using var db=c.Open();var s=new ProductService(c.Kernel(db));var before=await s.Read(c.Admin,data.Product.Id);
            await s.Save(c.Admin,before.Id,ProductChecks.Input(before) with{Name="Metadata only"});var after=await s.Read(c.Admin,before.Id);
            Check.True(after.Variants.Zip(before.Variants).All(x=>x.First.Version!=x.Second.Version&&x.First.Price==x.Second.Price&&x.First.Cost==x.Second.Cost),"Metadata change failed to advance versions or changed pricing.");
            await CommerceContext.Reject("CONCURRENT_UPDATE",async()=>await s.Save(c.Admin,before.Id,ProductChecks.Input(before)));
        });
        await Check.Run("K12 actual repricing between cart and checkout returns HTTP 409 with current cart and no hold",async()=>
        {
            var input=await CheckoutChecks.Cart(c,data);await using var db=c.Open();var k=c.Kernel(db);var purchases=new PurchaseService(k);
            var id=await purchases.Save(c.Admin,0,new(data.SupplierId,data.Product.Variants.Select(v=>new PurchaseLineInput(v.Id,1,v.Cost+200m)).ToList(),RequestKey:Guid.NewGuid()));var receipt=await purchases.Read(c.Admin,id);await purchases.Confirm(c.Admin,id,new(receipt.Version,Guid.NewGuid()));
            using var result=await c.CustomerClient.Send("/api/orders",input);using var json=JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            Check.True(result.StatusCode==HttpStatusCode.Conflict&&json.RootElement.GetProperty("code").GetString()=="PRICE_CHANGED"&&json.RootElement.GetProperty("details").GetProperty("lines").GetArrayLength()==2&&!await db.Orders.AnyAsync(x=>x.RequestKey==input.RequestKey)&&await db.Inventory.Where(x=>data.Product.Variants.Select(v=>v.Id).Contains(x.VariantId)).AllAsync(x=>x.Reserved==0),"Repricing failed structured reconfirmation or created partial order.");
        });
        await Check.Run("A13/A15 disabled supplier prevents confirmation without stock/cost effects",async()=>
        {
            var isolated=await CommerceData.Create(c);await using var db=c.Open();var k=c.Kernel(db);var s=new PurchaseService(k);var supplier=await db.Suppliers.AsNoTracking().SingleAsync(x=>x.Id==isolated.SupplierId);
            var id=await s.Save(c.Admin,0,new(supplier.Id,[new(isolated.Product.Variants[0].Id,2,2000)],RequestKey:Guid.NewGuid()));var draft=await s.Read(c.Admin,id);
            await new SupplierService(k).Save(c.Admin,supplier.Id,new(supplier.Code,supplier.Name,supplier.Phone,supplier.Address,false));
            await CommerceContext.Reject("INVALID_INPUT",async()=>await s.Confirm(c.Admin,id,new(draft.Version,Guid.NewGuid())));
            Check.True((await s.Read(c.Admin,id)).Status=="NHAP"&&!await db.Movements.AnyAsync(x=>x.PurchaseId==id)&&await db.Inventory.Where(x=>x.VariantId==isolated.Product.Variants[0].Id).AllAsync(x=>x.OnHand==0),"Disabled supplier confirmation partially committed.");
        });
        await Check.Run("ADMIN write API role and CSRF matrix rejects every customer request before effects",async()=>
        {
            var routes=new[]{("/api/admin/lookups/brands",HttpMethod.Post),("/api/admin/lookups/categories/1",HttpMethod.Put),("/api/admin/products",HttpMethod.Post),("/api/admin/products/1",HttpMethod.Put),("/api/admin/suppliers",HttpMethod.Post),("/api/admin/suppliers/1",HttpMethod.Put),("/api/admin/purchases",HttpMethod.Post),("/api/admin/purchases/1",HttpMethod.Put),("/api/admin/purchases/1/confirm",HttpMethod.Post),("/api/admin/purchases/1/cancel",HttpMethod.Post),("/api/admin/stock/1/adjust",HttpMethod.Post),("/api/admin/customers/1",HttpMethod.Put),("/api/admin/customers/1/state",HttpMethod.Post),("/api/admin/orders/1/confirm",HttpMethod.Post),("/api/admin/orders/1/cancel",HttpMethod.Post),("/api/admin/orders/1/dispatch",HttpMethod.Post),("/api/admin/orders/1/complete",HttpMethod.Post),("/api/admin/orders/1/fail",HttpMethod.Post),("/api/admin/orders/1/receive-return",HttpMethod.Post)};
            foreach(var (route,method) in routes)
            {
                using var denied=await c.CustomerClient.Send(route,new{},method);using var csrf=await c.AdminClient.Send(route,new{},method,csrf:false);
                Check.True(denied.StatusCode==HttpStatusCode.Forbidden&&csrf.StatusCode==HttpStatusCode.BadRequest,"Role/CSRF matrix failed: "+route);
            }
        });
        await Check.Run("Full-state commands require explicit active booleans instead of silently changing state",async()=>
        {
            using var missing=await c.AdminClient.Send("/api/admin/lookups/brands",new{name="Missing state"});
            Check.True(missing.StatusCode==HttpStatusCode.BadRequest,"Missing trading state silently applied a default.");
        });
        await Check.Run("A11 rejected late account lock restores all deleted sessions and active state",async()=>
        {
            await using var db=c.Open();var before=await db.Sessions.AsNoTracking().Where(x=>x.AccountId==c.Customer.AccountId).OrderBy(x=>x.Id).Select(x=>x.Id).ToListAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @id bigint={c.Customer.AccountId}; DECLARE @ddl nvarchar(max)=N'ALTER TABLE dbo.TAI_KHOAN ADD CONSTRAINT CK_QA_LockFailure CHECK (Id<>'+CONVERT(nvarchar(20),@id)+N' OR Active=1)'; EXEC(@ddl);");
            try
            {
                await CommerceContext.Reject("INVALID_INPUT",async()=>await new CustomerAdminService(c.Kernel(db)).SetActive(c.Admin,c.Customer.AccountId,new(false,true)));
                Check.True(await db.Accounts.AnyAsync(x=>x.Id==c.Customer.AccountId&&x.Active)&&(await db.Sessions.AsNoTracking().Where(x=>x.AccountId==c.Customer.AccountId).OrderBy(x=>x.Id).Select(x=>x.Id).ToListAsync()).SequenceEqual(before),"Rejected lock deleted sessions or disabled the account.");
            }
            finally{await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.TAI_KHOAN DROP CONSTRAINT CK_QA_LockFailure");}
        });
        await Check.Run("A19/A20 concurrent dispatch/cancel and complete/fail commit one coherent outcome",async()=>
        {
            async Task<OrderView> Create(bool ship)
            {
                var input=await CheckoutChecks.Cart(c,data);await using var db=c.Open();var k=c.Kernel(db);var q=new OrderQueryService(k);var w=new OrderWorkflow(k);var o=await new CheckoutService(k).Place(c.Customer,input);
                await w.Confirm(c.Admin,o.Id,new(o.Version,Guid.NewGuid()));o=await q.Read(c.Admin,o.Id,true);
                if(ship){await w.Dispatch(c.Admin,o.Id,new(o.Version,Guid.NewGuid()));o=await q.Read(c.Admin,o.Id,true);}return o;
            }
            async Task<bool> Attempt(OrderView o,string action)
            {
                await using var db=c.Open();var w=new OrderWorkflow(c.Kernel(db));var cmd=new VersionedCommand(o.Version,Guid.NewGuid(),"Kiểm tra tranh chấp");
                try{switch(action){case "dispatch":await w.Dispatch(c.Admin,o.Id,cmd);break;case "cancel":await w.Cancel(c.Admin,o.Id,cmd,true);break;case "complete":await w.Complete(c.Admin,o.Id,cmd);break;case "fail":await w.Fail(c.Admin,o.Id,cmd);break;}return true;}
                catch(PhoneStore.Core.BusinessException e)when(e.Code is "CONCURRENT_UPDATE" or "INVALID_STATE"){return false;}
            }
            var confirmed=await Create(false);var first=await Task.WhenAll(Attempt(confirmed,"dispatch"),Attempt(confirmed,"cancel"));
            var shipping=await Create(true);var second=await Task.WhenAll(Attempt(shipping,"complete"),Attempt(shipping,"fail"));
            await using var db=c.Open();var q=new OrderQueryService(c.Kernel(db));var a=await q.Read(c.Admin,confirmed.Id,true);var b=await q.Read(c.Admin,shipping.Id,true);
            Check.True(first.Count(x=>x)==1&&a.Status is "DA_HUY" or "DANG_GIAO"&&second.Count(x=>x)==1&&((b.Status=="HOAN_TAT"&&b.Payment=="DA_THU")||(b.Status=="GIAO_THAT_BAI"&&b.Payment=="CHUA_THU")),"Competing delivery commands left incoherent status/payment.");
        });
        await Check.Run("A22 bestseller rank follows quantity even when the lower-quantity item has greater revenue",async()=>
        {
            await using var db=c.Open();var k=c.Kernel(db);var purchases=new PurchaseService(k);
            var id=await purchases.Save(c.Admin,0,new(data.SupplierId,data.Product.Variants.Select((v,i)=>new PurchaseLineInput(v.Id,5,i==0?10m:10000m)).ToList(),RequestKey:Guid.NewGuid()));var receipt=await purchases.Read(c.Admin,id);await purchases.Confirm(c.Admin,id,new(receipt.Version,Guid.NewGuid()));db.ChangeTracker.Clear();
            var cart=new CartService(k);foreach(var line in (await cart.Read(c.Customer)).Lines)await cart.Set(c.Customer,new(line.VariantId,0));
            await cart.Set(c.Customer,new(data.Product.Variants[0].Id,3));await cart.Set(c.Customer,new(data.Product.Variants[1].Id,1));var current=await cart.Read(c.Customer);
            var order=await new CheckoutService(k).Place(c.Customer,new(Guid.NewGuid(),"Rank test","0909999999","Rank address",current.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price!.Value)).ToList()));var flow=new OrderWorkflow(k);var queries=new OrderQueryService(k);
            await flow.Confirm(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));order=await queries.Read(c.Admin,order.Id,true);await flow.Dispatch(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));order=await queries.Read(c.Admin,order.Id,true);await flow.Complete(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));
            var date=new DateTime(2021,6,1,0,0,0,DateTimeKind.Utc);await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.DON_HANG SET CompletedAt={date} WHERE Id={order.Id}");
            var report=await new ReportService(k).Read(c.Admin,"bestsellers","2021-06-01","2021-06-01");
            Check.True(report.Bestsellers[0].VariantId==data.Product.Variants[0].Id&&report.Bestsellers[0].Quantity==3&&report.Bestsellers[0].Revenue<report.Bestsellers[1].Revenue&&report.Bestsellers.Sum(x=>x.Revenue)==report.Revenue,"Bestseller sorted by revenue or lost snapshot totals.");
        });
    }
}
