using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Checks;

internal sealed partial class Prompt2AFixture
{
    public async Task Group1()
    {
        await Check.Run("A05.05 image URLs allowed fields uniqueness and CUSTOMER guards reject without partial writes",async()=>
        {
            var d=await Data();await using var db=C.Open();var service=new ProductService(C.Kernel(db));var p=await service.Read(C.Admin,d.Product.Id);var valid=ProductChecks.Input(p);
            foreach(var url in new[]{"javascript:alert(1)","data:image/png;base64,AAAA","//example.test/a.png","http://user:pass@example.test/a.png","http://example.test/a.png","https://u:p@example.test/a.png","/images/../private.png","/images/x\\a.png","/images/a\n.png","https://example.test/"+new string('x',1001)})
            {
                var before=await Snapshot();using var r=await C.AdminClient.Send($"/api/admin/products/{p.Id}",valid with{ImageUrl=url},HttpMethod.Put);await Error(r,HttpStatusCode.BadRequest,"INVALID_INPUT");await Unchanged(before);
            }
            foreach(var invalid in new[]{valid with{Images=[new("/images/a.png"),new("/images/a.png")]},valid with{Images=[new("/images/a.png",-1)]},valid with{Images=Enumerable.Range(0,13).Select(n=>new ImageInput($"/images/{n}.png")).ToList()},valid with{Variants=valid.Variants!.Select((v,i)=>v with{Sku=valid.Variants![0].Sku}).ToList()}})
            {
                var before=await Snapshot();using var r=await C.AdminClient.Send($"/api/admin/products/{p.Id}",invalid,HttpMethod.Put);await Error(r,HttpStatusCode.BadRequest,"INVALID_INPUT");await Unchanged(before);
            }
            var forbiddenBefore=await Snapshot();using(var forbidden=await C.CustomerClient.Send("/api/admin/products",valid))await Error(forbidden,HttpStatusCode.Forbidden,"FORBIDDEN");await Unchanged(forbiddenBefore);
            var overpost=JsonSerializer.SerializeToNode(valid)!.AsObject();overpost["onHand"]=9999;overpost["reserved"]=999;overpost["role"]="ADMIN";
            using(var r=await C.AdminClient.Send($"/api/admin/products/{p.Id}",overpost,HttpMethod.Put))await Error(r,HttpStatusCode.BadRequest,"INVALID_INPUT");await Unchanged(forbiddenBefore);
            await Save(d,x=>x with{ImageUrl="https://example.test/phone.png",Images=[new("/images/a.png")]});
        });
        await Check.Run("A17.05 late SQL journal failure rolls back the stock update and same-key retry applies once",async()=>
        {
            var d=await Data();var f=new StateMatrixFixture(C,d);await f.CreateOrder("CHO_XAC_NHAN");var v=d.Product.Variants[0].Id;var before=await Snapshot();var stock=(await Core.Snapshot()).Stock.Single(x=>x.VariantId==v);
            var input=new StockCountInput(stock.OnHand+5,"Prompt2A journal failure",Convert.ToBase64String(stock.Version),Guid.NewGuid());await using var db=C.Open();
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @id bigint={v}; DECLARE @sql nvarchar(max)=N'ALTER TABLE dbo.NHAT_KY_KHO ADD CONSTRAINT CK_P2A_COUNT_FAIL CHECK (VariantId <> '+CONVERT(nvarchar(20),@id)+N' OR Kind <> ''DIEU_CHINH'')'; EXEC(@sql);");
            // SQL CHECK error547 is mapped to INVALID_INPUT by the existing business kernel.
            try{using var r=await C.AdminClient.Send($"/api/admin/stock/{v}/adjust",input);await Error(r,HttpStatusCode.BadRequest,"INVALID_INPUT");await Unchanged(before);}
            finally{await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.NHAT_KY_KHO DROP CONSTRAINT CK_P2A_COUNT_FAIL");}
            using(var retry=await C.AdminClient.Send($"/api/admin/stock/{v}/adjust",input))Check.True(retry.StatusCode==HttpStatusCode.NoContent,"Failed journal consumed the count key/version.");
            var after=await Core.Snapshot();var now=after.Stock.Single(x=>x.VariantId==v);Check.True(now.OnHand==input.Counted&&now.Reserved==stock.Reserved&&after.Movements.Count(x=>x.OperationKey==input.OperationKey)==1,"Count retry had duplicate/partial effects.");
            var committed=await Snapshot();using(var stale=await C.AdminClient.Send($"/api/admin/stock/{v}/adjust",input))await Error(stale,HttpStatusCode.Conflict,"CONCURRENT_UPDATE");await Unchanged(committed);
        });
        await Check.Run("K09.02 missing and discontinued products reject; detail variant membership excludes foreign variants",async()=>
        {
            var d=await Data();var foreign=await Data();var before=await Snapshot();
            using(var missing=await C.CustomerClient.Http.GetAsync("/api/catalog/9223372036854775807"))await Error(missing,HttpStatusCode.NotFound,"NOT_FOUND");
            using(var r=await C.CustomerClient.Http.GetAsync($"/api/catalog/{d.Product.Id}?variantId={foreign.Product.Variants[0].Id}"))
            {
                var detail=await r.Content.ReadFromJsonAsync<PublicProduct>();Check.True(r.StatusCode==HttpStatusCode.OK&&detail!.Variants.Select(x=>x.Id).Order().SequenceEqual(d.Product.Variants.Select(x=>x.Id).Order())&&!detail.Variants.Any(x=>foreign.Product.Variants.Any(v=>v.Id==x.Id)),"Public detail injected a foreign variant.");
            }
            await Unchanged(before);await Save(d,x=>x with{Active=false});before=await Snapshot();
            using(var disabled=await C.CustomerClient.Http.GetAsync($"/api/catalog/{d.Product.Id}"))await Error(disabled,HttpStatusCode.NotFound,"NOT_FOUND");await Unchanged(before);
        });
        await Check.Run("K09.03 physical stock fully reserved remains public with available zero and internal balances redacted",async()=>
        {
            var d=await Data(5);var input=await CheckoutChecks.Cart(C,d,5);await using(var db=C.Open())await new CheckoutService(C.Kernel(db)).Place(C.Customer,input);
            var before=await Snapshot();var balances=(await Core.Snapshot()).Stock.Where(x=>d.Product.Variants.Any(v=>v.Id==x.VariantId)).ToList();Check.True(balances.All(x=>x.OnHand==5&&x.Reserved==5),"Full-reservation fixture failed.");
            using var r=await C.CustomerClient.Http.GetAsync($"/api/catalog/{d.Product.Id}");var raw=await r.Content.ReadAsStringAsync();var p=await r.Content.ReadFromJsonAsync<PublicProduct>();
            Check.True(r.StatusCode==HttpStatusCode.OK&&p!.Variants.All(x=>x.Available==0)&&!raw.Contains("\"onHand\"")&&!raw.Contains("\"reserved\"")&&!raw.Contains("\"cost\""),"Public full-reservation availability/redaction wrong.");await Unchanged(before);
        });
        await Check.Run("A10.02 ADMIN profile edits reject duplicate phone/email atomically and retain normalized own values",async()=>
        {
            await using var db=C.Open();var s=new CustomerAdminService(C.Kernel(db));var own=await s.Read(C.Admin,C.Customer.AccountId);var other=await s.Read(C.Admin,Other.Customer.AccountId);
            await s.Update(C.Admin,own.Id,new(own.FullName,own.Phone,$"p2a-own-{own.Id}@example.test",own.Address));await s.Update(C.Admin,other.Id,new(other.FullName,other.Phone,$"p2a-other-{other.Id}@example.test",other.Address));
            own=await s.Read(C.Admin,own.Id);other=await s.Read(C.Admin,other.Id);
            var recipientOrder=await new StateMatrixFixture(C,await Data()).CreateOrder("CHO_XAC_NHAN");
            foreach(var input in new[]{new ProfileChangeRequest("Partial phone edit",other.Phone,own.Email,"Partial address"),new ProfileChangeRequest("Partial email edit",own.Phone,other.Email,"Partial address")})
            {
                var before=await Snapshot();using var r=await C.AdminClient.Send($"/api/admin/customers/{own.Id}",input,HttpMethod.Put);await Error(r,HttpStatusCode.BadRequest,"INVALID_INPUT");await Unchanged(before);
                var stored=await new OrderQueryService(C.Kernel(db)).Read(C.Admin,recipientOrder.Id,true);
                Check.True(stored.Recipient==recipientOrder.Recipient&&stored.Phone==recipientOrder.Phone&&stored.Address==recipientOrder.Address,"Duplicate profile edit changed the old recipient in the same test.");
            }
            using var retained=await C.AdminClient.Send($"/api/admin/customers/{own.Id}",new ProfileChangeRequest(own.FullName," "+own.Phone+" "," "+own.Email!.ToUpperInvariant()+" ",own.Address),HttpMethod.Put);Check.True(retained.StatusCode==HttpStatusCode.NoContent,"Own normalized phone/email was treated as duplicate.");
            var current=await s.Read(C.Admin,own.Id);Check.True(current.Phone==own.Phone&&current.Email==own.Email,"Own identifiers failed normalization.");
            var retainedOrder=await new OrderQueryService(C.Kernel(db)).Read(C.Admin,recipientOrder.Id,true);
            Check.True(retainedOrder.Recipient==recipientOrder.Recipient&&retainedOrder.Phone==recipientOrder.Phone&&retainedOrder.Address==recipientOrder.Address,"Retaining own identifiers changed the old recipient.");
        });
        await Check.Run("A10.04 missing or invalid ADMIN customer edits preserve profiles and old recipient snapshots",async()=>
        {
            var d=await Data();var order=await new StateMatrixFixture(C,d).CreateOrder("CHO_XAC_NHAN");
            await using var db=C.Open();var own=await new CustomerAdminService(C.Kernel(db)).Read(C.Admin,C.Customer.AccountId);
            foreach(var sample in new[]{(long.MaxValue,new ProfileChangeRequest("Missing",own.Phone,own.Email,"New address"),HttpStatusCode.NotFound,"NOT_FOUND"),(own.Id,new ProfileChangeRequest("",own.Phone,own.Email,"Partial"),HttpStatusCode.BadRequest,"INVALID_INPUT"),(own.Id,new ProfileChangeRequest("Partial","invalid",own.Email,"Partial"),HttpStatusCode.BadRequest,"INVALID_INPUT"),(own.Id,new ProfileChangeRequest("Partial",own.Phone,"not-email","Partial"),HttpStatusCode.BadRequest,"INVALID_INPUT")})
            {var before=await Snapshot();using var r=await C.AdminClient.Send($"/api/admin/customers/{sample.Item1}",sample.Item2,HttpMethod.Put);await Error(r,sample.Item3,sample.Item4);await Unchanged(before);}
            var stored=await new OrderQueryService(C.Kernel(db)).Read(C.Admin,order.Id,true);Check.True(stored.Recipient==order.Recipient&&stored.Phone==order.Phone&&stored.Address==order.Address,"Rejected profile edit changed historical recipient.");
        });
        await Check.Run("A15.04 recheck empty draft cancellation and confirmation guards without stock or price side effects",async()=>
        {
            var d=await Data();await using var db=C.Open();var s=new PurchaseService(C.Kernel(db));
            foreach(var empty in new[]{false,true})
            {
                var id=await s.Save(C.Admin,0,new(d.SupplierId,empty?[]:d.Product.Variants.Select(v=>new PurchaseLineInput(v.Id,2,123m)).ToList(),RequestKey:Guid.NewGuid()));var row=await s.Read(C.Admin,id);
                if(empty){var unchanged=await Snapshot();using var rejected=await C.AdminClient.Send($"/api/admin/purchases/{id}/confirm",new VersionedCommand(row.Version,Guid.NewGuid()));await Error(rejected,HttpStatusCode.BadRequest,"INVALID_INPUT");await Unchanged(unchanged);}
                var before=await Core.Snapshot();using var cancelled=await C.AdminClient.Send($"/api/admin/purchases/{id}/cancel",new VersionedCommand(row.Version,Guid.NewGuid()));Check.True(cancelled.StatusCode==HttpStatusCode.NoContent,"Draft cancellation failed.");
                var after=await Core.Snapshot();var old=before.Purchases.Single(x=>x.Id==id);var current=after.Purchases.Single(x=>x.Id==id);Check.True(current.Status=="DA_HUY"&&!old.Version.SequenceEqual(current.Version),"Draft state/version did not advance.");old.Status=current.Status;old.Version=current.Version;StateMatrixFixture.Same(before,after,"Draft cancellation changed more than status/version.");
            }
        });
        await Check.Run("A21.01 all six invoice eligibility states missing IDs and roles preserve all business data",async()=>
        {
            var d=await Data(100);var f=new StateMatrixFixture(C,d);
            foreach(var state in StateMatrixFixture.States)
            {
                var order=await f.CreateOrder(state);var before=await Snapshot();using var r=await C.AdminClient.Http.GetAsync($"/api/admin/orders/{order.Id}/invoice");
                if(state is "DA_XAC_NHAN" or "DANG_GIAO" or "HOAN_TAT"){var invoice=await r.Content.ReadFromJsonAsync<SalesInvoice>();Check.True(r.StatusCode==HttpStatusCode.OK&&invoice!.Order.Id==order.Id&&invoice.Order.Total==order.Total,"Eligible invoice incorrect.");}
                else await Error(r,HttpStatusCode.Conflict,"INVALID_STATE");await Unchanged(before);
            }
            var unchanged=await Snapshot();using(var missing=await C.AdminClient.Http.GetAsync("/api/admin/orders/9223372036854775807/invoice"))await Error(missing,HttpStatusCode.NotFound,"NOT_FOUND");
            using(var forbidden=await C.CustomerClient.Http.GetAsync("/api/admin/orders/9223372036854775807/invoice"))await Error(forbidden,HttpStatusCode.Forbidden,"FORBIDDEN");await Unchanged(unchanged);
        });
        await Check.Run("A16.03 stock no-match is empty but real SQL read failure is SERVER_ERROR not zero stock",async()=>
        {
            var before=await Snapshot();using(var empty=await C.AdminClient.Http.GetAsync("/api/admin/stock?query=P2A-NO-MATCH-9223372036854775807")){var page=await empty.Content.ReadFromJsonAsync<Page<StockView>>();Check.True(empty.StatusCode==HttpStatusCode.OK&&page!.Total==0&&page.Items.Count==0,"No-match stock was not a valid empty page.");}await Unchanged(before);
            await using var db=C.Open();await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_rename N'dbo.TON_KHO',N'TON_KHO_PROMPT2A_OFFLINE'");
            try{using var failure=await C.AdminClient.Http.GetAsync("/api/admin/stock");await Error(failure,HttpStatusCode.InternalServerError,"SERVER_ERROR");}
            finally{await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_rename N'dbo.TON_KHO_PROMPT2A_OFFLINE',N'TON_KHO'");}await Unchanged(before);
        });
        await Check.Run("K08.01 combined brand price storage color and sort return exact matching pages and totals",async()=>
        {
            await FilterFixtures();var before=await Snapshot();await using var db=C.Open();var catalog=new CatalogService(C.Kernel(db));var filter=Combined();var first=await catalog.List(filter);var next=await catalog.List(filter with{Page=2});
            Check.True(first.Total==2&&next.Total==2&&first.Items.Single().Id==Filters[3].Product.Id&&next.Items.Single().Id==Filters[2].Product.Id,"Combined filter sorting/paging/count wrong.");
            Check.True(first.Items.Concat(next.Items).SelectMany(x=>x.Variants).All(v=>v.Color=="Black"&&v.StorageGb==128&&v.Price>=2300m&&v.Price<=2500m),"Response variants violate combined predicates.");await Unchanged(before);
        });
        await Check.Run("K08.02 color storage and price must match the same variant not separate variants",async()=>
        {
            await FilterFixtures();var before=await Snapshot();await using var db=C.Open();var page=await new CatalogService(C.Kernel(db)).List(Combined() with{PageSize=100});
            Check.True(page.Total==2&&page.Items.Select(x=>x.Id).Order().SequenceEqual(new[]{Filters[2].Product.Id,Filters[3].Product.Id}.Order())&&!page.Items.Any(x=>x.Id==Filters[0].Product.Id||x.Id==Filters[1].Product.Id),"Filter combined values from different variants.");await Unchanged(before);
        });
    }
}
