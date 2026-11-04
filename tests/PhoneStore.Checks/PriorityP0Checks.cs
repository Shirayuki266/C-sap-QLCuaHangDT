using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class PriorityP0Checks
{
    private sealed class Parameters:DbCommandInterceptor
    {
        public List<(string Sql,List<string> Values)> Commands{get;}=[];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default)
        {
            Commands.Add((command.CommandText,command.Parameters.Cast<DbParameter>().Select(x=>x.Value?.ToString()??"").ToList()));
            return ValueTask.FromResult(result);
        }
    }
    public static async Task RunIsolated()
    {
        await using var database=await TestDatabase.Create();
        await using(var db=new StoreDbContext(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(database.ConnectionString).Options))await new DatabaseMigrator(db).MigrateAsync();
        await HttpChecks.Run(database.ConnectionString,priorityOnly:true);
    }
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"priority-p0");await c.Initialize();
        async Task<CommerceData> Data()=>await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),30);
        async Task Active(CommerceData data,bool active,bool variantOnly=false)
        {
            await using var db=c.Open();var service=new ProductService(c.Kernel(db));var p=await service.Read(c.Admin,data.Product.Id);var input=ProductChecks.Input(p);
            await service.Save(c.Admin,p.Id,variantOnly?input with{Variants=input.Variants!.Select((v,i)=>i==0?v with{Active=active}:v).ToList()}:input with{Active=active});
        }
        await Check.Run("K07.04 public special-character search uses SQL parameters and leaves business data unchanged",async()=>
        {
            var data=await Data();await Data();var payloads=new[]{"' OR 1=1 --","'; DROP TABLE dbo.SAN_PHAM;--","%_[",@"\[\]","O'Reilly"};
            await using(var db=c.Open())
            {
                var service=new ProductService(c.Kernel(db));var p=await service.Read(c.Admin,data.Product.Id);
                await service.Save(c.Admin,p.Id,ProductChecks.Input(p) with{Specifications=string.Join(" | ",payloads)});
            }
            var f=new StateMatrixFixture(c,data);var before=await f.Snapshot();
            await using var products=c.Open();var productRows=await products.Products.AsNoTracking().OrderBy(x=>x.Id).ToListAsync();
            foreach(var term in payloads)
            {
                var capture=new Parameters();await using var db=new StoreDbContext(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).AddInterceptors(capture).Options);
                var page=await new CatalogService(c.Kernel(db)).List(new(Query:term));
                Check.True(page.Total==1&&page.Items.Single().Id==data.Product.Id&&capture.Commands.Any(x=>x.Values.Count>0),"Special search lost literal matching or used no SQL parameters.");
                if(term.Contains(" OR ",StringComparison.Ordinal)||term.Contains("DROP TABLE",StringComparison.Ordinal))
                {
                    // EF escapes LIKE metacharacters (including the underscore in SAN_PHAM) inside parameter values.
                    var marker=term.Contains("DROP TABLE",StringComparison.Ordinal)?"DROP TABLE":"OR 1=1";
                    Check.True(capture.Commands.Any(x=>x.Values.Any(v=>v.Contains(marker,StringComparison.Ordinal)))&&capture.Commands.All(x=>!x.Sql.Contains(marker,StringComparison.Ordinal)),"SQL-looking search text was concatenated into command text.");
                }
                using var response=await c.CustomerClient.Http.GetAsync("/api/catalog?query="+Uri.EscapeDataString(term));
                var body=await response.Content.ReadAsStringAsync();var actual=await response.Content.ReadFromJsonAsync<Page<PublicProduct>>();
                Check.True(response.StatusCode==HttpStatusCode.OK&&actual?.Total==1&&actual.Items.Single().Id==data.Product.Id&&!body.Contains("\"cost\"",StringComparison.OrdinalIgnoreCase),"HTTP literal search failed/redaction changed.");
            }
            await f.Unchanged(before);StateMatrixFixture.Same(productRows,await products.Products.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),"Search changed/deleted product metadata.");
        });
        await Check.Run("A07.01 disable and reopen product changes public visibility and revalidates checkout",async()=>
        {
            var data=await Data();var f=new StateMatrixFixture(c,data);var input=await CheckoutChecks.Cart(c,data);await Active(data,false);
            await using(var db=c.Open())
            {
                var catalog=new CatalogService(c.Kernel(db));await CommerceContext.Reject("NOT_FOUND",async()=>await catalog.Read(data.Product.Id));
                Check.True((await catalog.List(new(BrandId:data.Product.BrandId))).Total==0,"Disabled product remained public.");
            }
            var disabled=await f.Snapshot();using(var response=await c.CustomerClient.Send("/api/orders",input))Check.True(response.StatusCode==HttpStatusCode.BadRequest,"Disabled product checked out.");await f.Unchanged(disabled);
            await Active(data,true);
            await using var current=c.Open();var kernel=c.Kernel(current);Check.True((await new CatalogService(kernel).Read(data.Product.Id)).Id==data.Product.Id,"Reopened product stayed hidden.");
            var before=await f.Snapshot();var placed=await new CheckoutService(kernel).Place(c.Customer,input);var after=await f.Snapshot();
            Check.True(placed.Status=="CHO_XAC_NHAN"&&placed.Payment=="CHUA_THU"&&after.Orders.Count==before.Orders.Count+1,"Reopened product could not be checked out exactly once.");
            foreach(var line in placed.Lines){var old=before.Stock.Single(x=>x.VariantId==line.VariantId);var now=after.Stock.Single(x=>x.VariantId==line.VariantId);Check.True(now.OnHand==old.OnHand&&now.Reserved==old.Reserved+line.Quantity,"Reopen checkout H/R effect wrong.");}
        });
        await Check.Run("A07.04 checkout rechecks a variant discontinued after viewing the cart and rolls back",async()=>
        {
            var data=await Data();var f=new StateMatrixFixture(c,data);var input=await CheckoutChecks.Cart(c,data);await Active(data,false,variantOnly:true);
            await using(var db=c.Open())
            {
                var cart=await new CartService(c.Kernel(db)).Read(c.Customer);Check.True(cart.Lines.Any(x=>x.VariantId==data.Product.Variants[0].Id&&!x.Sellable&&x.Issue=="DISCONTINUED"),"Cart did not revalidate the discontinued variant.");
            }
            var before=await f.Snapshot();using var response=await c.CustomerClient.Send("/api/orders",input);
            Check.True(response.StatusCode==HttpStatusCode.BadRequest,"Previously viewed cart bypassed trading-state check.");await f.Unchanged(before);
            Check.True(!before.Orders.Any(x=>x.RequestKey==input.RequestKey),"Rejected checkout had an order.");
        });
        await Check.Run("A08.02 disabling used brand and category preserves products variants receipts orders and history",async()=>
        {
            var data=await Data();var f=new StateMatrixFixture(c,data);var order=await f.CreateOrder("HOAN_TAT");
            foreach(var kind in new[]{"brands","categories"})
            {
                var before=await f.Snapshot();await using var db=c.Open();var productRows=await db.Products.AsNoTracking().OrderBy(x=>x.Id).ToListAsync();var k=c.Kernel(db);var id=kind=="brands"?data.Product.BrandId:data.Product.CategoryId;
                var name=kind=="brands"?await db.Brands.Where(x=>x.Id==id).Select(x=>x.Name).SingleAsync():await db.Categories.Where(x=>x.Id==id).Select(x=>x.Name).SingleAsync();
                await new LookupService(k).Save(c.Admin,kind,id,new(name,false));await f.Unchanged(before);
                StateMatrixFixture.Same(productRows,await db.Products.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),"Lookup disable cascaded product metadata/state.");
                Check.True(!(await new LookupService(k).Public(kind)).Any(x=>x.Id==id),"Disabled lookup still public.");
                await CommerceContext.Reject("NOT_FOUND",async()=>await new CatalogService(k).Read(data.Product.Id));
                Check.True((await new OrderQueryService(k).Read(c.Admin,order.Id,true)).Status=="HOAN_TAT"&&(await new InvoiceService(k).Read(c.Admin,order.Id)).Order.Total==order.Total,"Linked historical order/invoice was lost.");
            }
        });
        await Check.Run("A18.04 all-order queries enforce ADMIN and are read-only for header COD stock and history",async()=>
        {
            var data=await Data();var f=new StateMatrixFixture(c,data);var order=await f.CreateOrder("DANG_GIAO");var before=await f.Snapshot();
            using var guest=new HttpClient(new HttpClientHandler{UseProxy=false}){BaseAddress=address};
            foreach(var route in new[]{"/api/admin/orders",$"/api/admin/orders?customerId={c.Customer.AccountId}",$"/api/admin/orders/{order.Id}"})
            {
                using var denied=await c.CustomerClient.Http.GetAsync(route);using var anonymous=await guest.GetAsync(route);using var allowed=await c.AdminClient.Http.GetAsync(route);
                Check.True(denied.StatusCode==HttpStatusCode.Forbidden&&anonymous.StatusCode==HttpStatusCode.Unauthorized&&allowed.StatusCode==HttpStatusCode.OK,"All-order HTTP authorization/read failed.");
            }
            await using var db=c.Open();var query=new OrderQueryService(c.Kernel(db));await CommerceContext.Reject("FORBIDDEN",async()=>await query.List(c.Customer,new(),true));
            var rows=await query.List(c.Admin,new(Query:order.Code,PageSize:1),true);Check.True(rows.Total==1&&rows.Items.Single().Id==order.Id,"Admin order query did not read exact stored order.");await f.Unchanged(before);
        });
        await Check.Run("A21.02 invoice recipient lines prices and total remain snapshots after catalog and profile edits",async()=>
        {
            var data=await Data();var f=new StateMatrixFixture(c,data);var order=await f.CreateOrder("DA_XAC_NHAN");SalesInvoice original;
            await using(var db=c.Open())original=await new InvoiceService(c.Kernel(db)).Read(c.Admin,order.Id);
            await using(var db=c.Open())
            {
                var k=c.Kernel(db);var products=new ProductService(k);var p=await products.Read(c.Admin,data.Product.Id);var input=ProductChecks.Input(p);
                await products.Save(c.Admin,p.Id,input with{Name="Edited catalog after invoice",Active=false,Variants=input.Variants!.Select(v=>v with{Sku=v.Sku+"-EDITED",ProfitPercent=50m}).ToList()});
                await new AccountProfiles(db,TimeProvider.System).UpdateAsync(c.Customer.AccountId,c.Customer.SessionId,new ProfileChangeRequest("Edited customer after invoice","0961234567","invoice-edit@example.test","Edited profile address"));
                Check.True((await products.Read(c.Admin,p.Id)).Variants[0].Price!=data.Product.Variants[0].Price&&(await new AccountProfiles(db,TimeProvider.System).ReadAsync(c.Customer.AccountId,c.Customer.SessionId)).FullName=="Edited customer after invoice","Invoice mutation fixture did not change live catalog/profile.");
            }
            var before=await f.Snapshot();await using var readDb=c.Open();var current=await new InvoiceService(c.Kernel(readDb)).Read(c.Admin,order.Id);StateMatrixFixture.Same(original,current,"Historical invoice changed with live catalog/profile.");
            using var http=await c.AdminClient.Http.GetAsync($"/api/admin/orders/{order.Id}/invoice");Check.True(http.StatusCode==HttpStatusCode.OK,"Historical invoice HTTP failed.");
            StateMatrixFixture.Same(original,await http.Content.ReadFromJsonAsync<SalesInvoice>(),"HTTP invoice lost immutable snapshot fields.");await f.Unchanged(before);
        });
    }
}
