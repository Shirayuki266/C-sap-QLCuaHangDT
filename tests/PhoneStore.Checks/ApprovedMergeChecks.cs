using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class ApprovedMergeChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"approved-merge");await c.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),5);
        var a=data.Product.Variants[0].Id;var b=data.Product.Variants[1].Id;await using var db=c.Open();var k=c.Kernel(db);var s=new CartService(k);
        await Check.Run("K10.04 DEC.02 max merge repeats and parallel retries do not accumulate guest quantities",async()=>
        {
            await s.Set(c.Customer,new(a,2));var input=new List<CartInput>{new(a,3),new(a,1),new(b,2)};
            var first=await s.Merge(c.Customer,input);var second=await s.Merge(c.Customer,input);
            Check.True(first.Cart.Lines.Single(x=>x.VariantId==a).Quantity==3&&second.Cart.Lines.SequenceEqual(first.Cart.Lines)&&second.Adjusted.Count==0,"Identical merge accumulated quantities.");
            async Task<CartMergeResult> Merge(){await using var d=c.Open();return await new CartService(c.Kernel(d)).Merge(c.Customer,input);}
            var results=await Task.WhenAll(Merge(),Merge());Check.True(results.All(x=>x.Cart.Lines.Single(v=>v.VariantId==a).Quantity==3),"Parallel merge accumulated quantities.");
            Check.True(await db.Inventory.Where(x=>x.VariantId==a||x.VariantId==b).AllAsync(x=>x.OnHand==5&&x.Reserved==0),"Merge changed inventory.");
        });
        await Check.Run("DEC.02 merge reports live caps and removes discontinued missing and zero-stock lines",async()=>
        {
            var zero=await CommerceData.Create(c,1);var inactive=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c,1),2);
            await s.Set(c.Customer,new(inactive.Product.Variants[0].Id,1));
            await new ProductService(k).Save(c.Admin,inactive.Product.Id,ProductChecks.Input(inactive.Product) with{Active=false});
            var result=await s.Merge(c.Customer,[new(a,int.MaxValue),new(zero.Product.Variants[0].Id,1),new(long.MaxValue,1)]);
            Check.True(result.Cart.Lines.Single(x=>x.VariantId==a).Quantity==5&&result.Cart.Lines.All(x=>x.VariantId!=inactive.Product.Variants[0].Id&&x.VariantId!=zero.Product.Variants[0].Id&&x.VariantId!=long.MaxValue),"Unusable merge lines were retained.");
            Check.True(result.Adjusted.Count==4&&result.Adjusted.Any(x=>x.Reason=="DISCONTINUED")&&result.Adjusted.Any(x=>x.Reason=="NOT_FOUND")&&result.Adjusted.Any(x=>x.Reason=="OUT_OF_STOCK")&&result.Adjusted.Any(x=>x.Reason=="QUANTITY_ADJUSTED"),"Adjustment reasons missing.");
            using var response=await c.CustomerClient.Send("/api/cart/merge",new[]{new CartInput(a,99)});var body=(await response.Content.ReadFromJsonAsync<CartMergeResult>())!;
            Check.True(response.StatusCode==HttpStatusCode.OK&&body.Cart.Lines.Single(x=>x.VariantId==a).Quantity==5&&body.Adjusted.Count==1,"HTTP merge response is not cart/adjusted.");
            using var denied=await c.AdminClient.Send("/api/cart/merge",new[]{new CartInput(a,1)});using var csrf=await c.CustomerClient.Send("/api/cart/merge",new[]{new CartInput(a,1)},csrf:false);
            using var overpost=await c.CustomerClient.Send("/api/cart/merge",new[]{new{variantId=a,quantity=1,price=1,customerId=c.Admin.AccountId}});
            Check.True(denied.StatusCode==HttpStatusCode.Forbidden&&csrf.StatusCode==HttpStatusCode.BadRequest&&overpost.StatusCode==HttpStatusCode.BadRequest,"Merge trust boundary failed.");
        });
        await Check.Run("DEC.02 late merge write failure rolls back all cart adjustments without stock effects",async()=>
        {
            await s.Set(c.Customer,new(a,2));await s.Set(c.Customer,new(b,1));var before=await s.Read(c.Customer);var movements=await db.Movements.CountAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @customer bigint={c.Customer.AccountId}, @variant bigint={b}; DECLARE @sql nvarchar(max)=N'ALTER TABLE dbo.GIO_HANG_CT ADD CONSTRAINT CK_QA_MergeLate CHECK (CustomerId<>'+CONVERT(nvarchar(20),@customer)+N' OR VariantId<>'+CONVERT(nvarchar(20),@variant)+N' OR Quantity<=1)'; EXEC(@sql);");
            try{await CommerceContext.Reject("INVALID_INPUT",async()=>await s.Merge(c.Customer,[new(a,4),new(b,3)]));Check.True((await s.Read(c.Customer)).Lines.SequenceEqual(before.Lines),"Rejected merge left partial cart updates.");}
            finally{await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.GIO_HANG_CT DROP CONSTRAINT CK_QA_MergeLate");}
            Check.True(await db.Movements.CountAsync()==movements&&await db.Inventory.Where(x=>x.VariantId==a||x.VariantId==b).AllAsync(x=>x.OnHand==5&&x.Reserved==0),"Failed merge wrote stock or journal.");
        });
    }
}
