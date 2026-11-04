using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class ChecklistCatalogFilterChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"catalog-state-checklist");await c.Initialize();
        var first=await CommerceData.Create(c);var second=await CommerceData.Create(c);var term="Statefilter "+Guid.NewGuid().ToString("N");
        await using var db=c.Open();var k=c.Kernel(db);var products=new ProductService(k);var catalog=new CatalogService(k);
        await products.Save(c.Admin,first.Product.Id,ProductChecks.Input(first.Product) with{Name=term+" A"});
        await products.Save(c.Admin,second.Product.Id,ProductChecks.Input(second.Product) with{Name=term+" B",Active=false});
        await Check.Run("A04.01-02 internal active/inactive filters compose with query brand and stable paging",async()=>
        {
            var active=await catalog.Internal(c.Admin,new(Query:term,Active:true));var inactive=await catalog.Internal(c.Admin,new(Query:term,Active:false));
            Check.True(active.Total==1&&active.Items.Single().Id==first.Product.Id&&inactive.Total==1&&inactive.Items.Single().Id==second.Product.Id,"Trading-state filter ignored.");
            Check.True((await catalog.Internal(c.Admin,new(Query:term,BrandId:first.Product.BrandId,Active:false))).Total==0,"State and brand did not compose.");
            var page1=await catalog.Internal(c.Admin,new(Query:term,PageSize:1));var page2=await catalog.Internal(c.Admin,new(Query:term,Page:2,PageSize:1));
            Check.True(page1.Total==2&&page2.Total==2&&page1.Items.Single().Id!=page2.Items.Single().Id,"Unfiltered state list/paging incorrect.");
            using var response=await c.AdminClient.Http.GetAsync("/api/admin/products?active=false&query="+Uri.EscapeDataString(term));
            Check.True(response.IsSuccessStatusCode&&(await response.Content.ReadFromJsonAsync<Page<AdminProduct>>())!.Items.Single().Id==second.Product.Id,"HTTP active binding failed.");
        });
        await Check.Run("A04.03-04 public visibility and role guards remain unchanged; state reads do not mutate",async()=>
        {
            var before=await db.Variants.AsNoTracking().Where(x=>x.ProductId==first.Product.Id||x.ProductId==second.Product.Id).OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Cost,x.Price,x.Active}).ToListAsync();
            var publicRows=await catalog.List(new(Query:term,Active:false));Check.True(publicRows.Total==1&&publicRows.Items.Single().Id==first.Product.Id,"Internal flag opened public inactive visibility.");
            await CommerceContext.Reject("FORBIDDEN",async()=>await catalog.Internal(c.Customer,new(Active:false)));
            using var denied=await c.CustomerClient.Http.GetAsync("/api/admin/products?active=false");using var malformed=await c.AdminClient.Http.GetAsync("/api/admin/products?active=invalid");
            Check.True(denied.StatusCode==HttpStatusCode.Forbidden&&malformed.StatusCode==HttpStatusCode.BadRequest,"State role/input validation failed.");
            Check.True((await db.Variants.AsNoTracking().Where(x=>x.ProductId==first.Product.Id||x.ProductId==second.Product.Id).OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Cost,x.Price,x.Active}).ToListAsync()).SequenceEqual(before),"Read changed variant prices/trading state.");
            Check.True(await db.Inventory.Where(x=>before.Select(v=>v.Id).Contains(x.VariantId)).AllAsync(x=>x.OnHand==0&&x.Reserved==0),"Read changed opening inventory.");
        });
    }
}
