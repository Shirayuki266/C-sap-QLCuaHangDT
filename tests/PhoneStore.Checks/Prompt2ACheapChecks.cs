using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Checks;

internal sealed partial class Prompt2AFixture
{
    public async Task Group2()
    {
        await Check.Run("A08.03 inactive lookups cannot be newly selected but retained references can be edited",async()=>
        {
            var retained=await Data();var another=await Data();
            foreach(var kind in new[]{"brands","categories"})
            {
                await using(var db=C.Open())
                {
                    var id=kind=="brands"?retained.Product.BrandId:retained.Product.CategoryId;
                    var name=kind=="brands"?await db.Brands.Where(x=>x.Id==id).Select(x=>x.Name).SingleAsync():await db.Categories.Where(x=>x.Id==id).Select(x=>x.Name).SingleAsync();
                    await new LookupService(C.Kernel(db)).Save(C.Admin,kind,id,new(name,false));
                }
                await using(var db=C.Open())
                {
                    var service=new ProductService(C.Kernel(db));var other=await service.Read(C.Admin,another.Product.Id);var input=ProductChecks.Input(other);
                    input=kind=="brands"?input with{BrandId=retained.Product.BrandId}:input with{CategoryId=retained.Product.CategoryId};
                    var before=await Snapshot();await CommerceContext.Reject("INVALID_INPUT",async()=>await service.Save(C.Admin,other.Id,input));await Unchanged(before);
                    var creation=input with{Code="P2A-"+Guid.NewGuid().ToString("N")[..20],Variants=input.Variants!.Select((v,i)=>v with{Id=0,Version=null,Sku="P2A-"+Guid.NewGuid().ToString("N")+i}).ToList()};
                    await CommerceContext.Reject("INVALID_INPUT",async()=>await service.Save(C.Admin,0,creation));await Unchanged(before);
                }
                var balances=await Core.Snapshot();await Save(retained,x=>x with{Name="Retained inactive "+kind});await using var current=C.Open();var product=await new ProductService(C.Kernel(current)).Read(C.Admin,retained.Product.Id);
                Check.True(product.BrandId==retained.Product.BrandId&&product.CategoryId==retained.Product.CategoryId&&product.Active,"Existing inactive reference was not retained.");
                await CommerceContext.Reject("NOT_FOUND",async()=>await new CatalogService(C.Kernel(current)).Read(product.Id));var after=await Core.Snapshot();
                StateMatrixFixture.Same(balances.Stock,after.Stock,"Retained lookup edit changed stock.");StateMatrixFixture.Same(balances.History,after.History,"Retained lookup edit changed history.");
            }
        });
        await using(var db=C.Open())
        {
            var service=new CustomerAdminService(C.Kernel(db));
            foreach(var item in new[]{(C.Customer.AccountId,"P2A Search Alpha"),(Other.Customer.AccountId,"P2A Search Beta")})
            {
                var p=await service.Read(C.Admin,item.Item1);await service.Update(C.Admin,p.Id,new(item.Item2,p.Phone,$"p2a-search-{p.Id}@example.test",p.Address));
            }
        }
        await Check.Run("A09.01 ADMIN name phone email searches and paged totals identify the exact customers",async()=>
        {
            var before=await Snapshot();await using var db=C.Open();var service=new CustomerAdminService(C.Kernel(db));var own=await service.Read(C.Admin,C.Customer.AccountId);
            foreach(var query in new[]{own.FullName,own.Phone,own.Email!}){var page=await service.List(C.Admin,query);Check.True(page.Total==1&&page.Items.Single().Id==own.Id,"Customer name/phone/email search mismatch.");}
            var a=await service.List(C.Admin,"P2A Search",page:1,pageSize:1);var b=await service.List(C.Admin,"P2A Search",page:2,pageSize:1);
            Check.True(a.Total==2&&b.Total==2&&a.Items.Single().Id==C.Customer.AccountId&&b.Items.Single().Id==Other.Customer.AccountId,"Customer totals or stable paging wrong.");await Unchanged(before);
        });
        await Check.Run("A12.04 selected-customer order lists keep price total history snapshots and are read-only",async()=>
        {
            var d=await Data();var order=await new StateMatrixFixture(C,d).CreateOrder("DA_XAC_NHAN");await Save(d,x=>x with{Name="P2A live repriced",Variants=x.Variants!.Select(v=>v with{ProfitPercent=50m}).ToList()});
            var before=await Snapshot();await using var db=C.Open();var page=await new OrderQueryService(C.Kernel(db)).List(C.Admin,new(Query:order.Code,CustomerId:C.Customer.AccountId),true);var result=page.Items.Single();
            Check.True(page.Total==1&&result.Total==order.Total&&result.CustomerId==C.Customer.AccountId,"Selected-customer order total/source wrong.");StateMatrixFixture.Same(order.Lines,result.Lines,"List read repriced order snapshots.");StateMatrixFixture.Same(order.History,result.History,"List changed order history.");
            using var response=await C.AdminClient.Http.GetAsync($"/api/admin/customers/{C.Customer.AccountId}/orders?pageSize=100");var actual=await response.Content.ReadFromJsonAsync<Page<OrderView>>();
            Check.True(response.StatusCode==HttpStatusCode.OK&&actual!.Items.All(x=>x.CustomerId==C.Customer.AccountId)&&actual.Items.Single(x=>x.Id==order.Id).Total==order.Total,"Selected customer HTTP list leaked or repriced an order.");await Unchanged(before);
        });
        await Check.Run("A09.04 customer searches preserve all profile account state session and business rows",async()=>
        {
            var before=await Snapshot();foreach(var query in new[]{"P2A Search","P2A-no-customer-match","   "})
            {using var response=await C.AdminClient.Http.GetAsync("/api/admin/customers?query="+Uri.EscapeDataString(query)+"&pageSize=1");Check.True(response.StatusCode==HttpStatusCode.OK,"Customer readonly search failed.");}await Unchanged(before);
        });
        await Check.Run("K07.02 public blank whitespace and no-match search return correct metadata without writes",async()=>
        {
            var before=await Snapshot();await using var db=C.Open();var service=new CatalogService(C.Kernel(db));var normal=await service.List(new());
            StateMatrixFixture.Same(normal,await service.List(new(Query:"")),"Blank search differs from unfiltered page.");StateMatrixFixture.Same(normal,await service.List(new(Query:" \t\r\n ")),"Whitespace search differs from unfiltered page.");
            using var response=await C.CustomerClient.Http.GetAsync("/api/catalog?query=P2A-NO-PUBLIC-MATCH-9223372036854775807");var empty=await response.Content.ReadFromJsonAsync<Page<PublicProduct>>();
            Check.True(response.StatusCode==HttpStatusCode.OK&&empty!.Total==0&&empty.Items.Count==0&&empty.PageNumber==1&&empty.PageSize==20,"No-match search metadata/error distinction wrong.");await Unchanged(before);
        });
        await Check.Run("K08.04 clearing individual or all filters recomputes exact totals and stable pages",async()=>
        {
            await FilterFixtures();var before=await Snapshot();await using var db=C.Open();var service=new CatalogService(C.Kernel(db));var filter=Combined() with{PageSize=100};
            var noColor=await service.List(filter with{Color=null});var noPrices=await service.List(filter with{MinPrice=null,MaxPrice=null});
            Check.True(noColor.Total==3&&noColor.Items.Select(x=>x.Id).Order().SequenceEqual(new[]{Filters[1].Product.Id,Filters[2].Product.Id,Filters[3].Product.Id}.Order()),"Clearing color discarded remaining predicates.");
            Check.True(noPrices.Total==3&&noPrices.Items.Select(x=>x.Id).Order().SequenceEqual(new[]{Filters[0].Product.Id,Filters[2].Product.Id,Filters[3].Product.Id}.Order()),"Clearing price discarded remaining predicates.");
            var page1=await service.List(new(Query:"P2A Filter",PageSize:2));var page2=await service.List(new(Query:"P2A Filter",PageSize:2,Page:2));
            Check.True(page1.Total==4&&page2.Total==4&&page1.Items.Concat(page2.Items).Select(x=>x.Id).SequenceEqual(Filters.Select(x=>x.Product.Id)),"Clearing filters left stale totals/pages.");await Unchanged(before);
        });
        await Check.Run("K13.04 own order history lists do not change status completion payment stock or journals",async()=>
        {
            var before=await Snapshot();await using var db=C.Open();var list=await new OrderQueryService(C.Kernel(db)).List(C.Customer,new(PageSize:100));
            Check.True(list.Total>0&&list.Items.All(x=>x.CustomerId==C.Customer.AccountId),"Own order list scope/fixture wrong.");
            using var response=await C.CustomerClient.Http.GetAsync("/api/orders?pageSize=100");var actual=await response.Content.ReadFromJsonAsync<Page<OrderView>>();
            Check.True(response.StatusCode==HttpStatusCode.OK&&actual!.Total==list.Total&&actual.Items.All(x=>x.CustomerId==C.Customer.AccountId),"Own HTTP order list mismatch.");await Unchanged(before);
        });
    }
}
