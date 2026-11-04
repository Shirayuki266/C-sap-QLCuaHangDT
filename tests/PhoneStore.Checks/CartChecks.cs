using System.Net;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class CartChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"carts");await c.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),5);
        await using var db=c.Open();var service=new CartService(c.Kernel(db));var first=data.Product.Variants[0];var second=data.Product.Variants[1];
        await Check.Run("K10/K11 own cart prices, updates, duplicate merge and deletion do not reserve stock",async()=>
        {
            await service.Set(c.Customer,new(first.Id,1),true);await service.Merge(c.Customer,[new(first.Id,1),new(first.Id,1),new(second.Id,1)]);
            var cart=await service.Read(c.Customer);Check.True(cart.Lines.Count==2&&cart.Lines.Single(x=>x.VariantId==first.Id).Quantity==1&&cart.Total==first.Price+second.Price,"Cart max merge/price incorrect.");
            await service.Set(c.Customer,new(second.Id,0));cart=await service.Read(c.Customer);Check.True(cart.Lines.Count==1,"Cart deletion failed.");
            Check.True(await db.Inventory.Where(x=>x.VariantId==first.Id||x.VariantId==second.Id).AllAsync(x=>x.Reserved==0),"Cart reserved stock.");
        });
        await Check.Run("K10/K11 approved merge caps shortages and guest stale preview remains explicit",async()=>
        {
            var merged=await service.Merge(c.Customer,[new(second.Id,1),new(first.Id,99)]);
            Check.True(merged.Cart.Lines.Count==2&&merged.Cart.Lines.Single(x=>x.VariantId==first.Id).Quantity==5&&merged.Adjusted.Single().Quantity==5,"Merge did not report an availability cap.");
            var guest=await service.Preview([new(long.MaxValue,1)]);Check.True(guest.HasIssues&&guest.Lines.Single().Issue=="NOT_FOUND","Guest stale line was silently dropped.");
            using var missing=await c.CustomerClient.Send("/api/cart",new{variantId=first.Id},HttpMethod.Put);
            using var malformed=await c.CustomerClient.Send("/api/cart",new{variantId=first.Id,quantity=1.5m},HttpMethod.Put);
            using var forbidden=await c.AdminClient.Http.GetAsync("/api/cart");
            Check.True(missing.StatusCode==HttpStatusCode.BadRequest&&malformed.StatusCode==HttpStatusCode.BadRequest&&forbidden.StatusCode==HttpStatusCode.Forbidden,"Cart JSON/role validation failed.");
        });
    }
}
