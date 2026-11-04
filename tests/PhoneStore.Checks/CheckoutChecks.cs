using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class CheckoutChecks
{
    public static async Task<CheckoutInput> Cart(CommerceContext c,CommerceData data,int quantity=1)
    {
        await using var db=c.Open();var s=new CartService(c.Kernel(db));
        var old=await s.Read(c.Customer);foreach(var line in old.Lines)await s.Set(c.Customer,new(line.VariantId,0));
        foreach(var v in data.Product.Variants)await s.Set(c.Customer,new(v.Id,quantity));
        var cart=await s.Read(c.Customer);
        return new(Guid.NewGuid(),"Người nhận kiểm thử","0909999999","Địa chỉ nhận hàng kiểm thử",cart.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price!.Value)).ToList());
    }
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"checkout");await c.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),3);var request=await Cart(c,data);
        await using var db=c.Open();var kernel=c.Kernel(db);var checkout=new CheckoutService(kernel);
        await Check.Run("K12 invalid COD/recipient and changed prices commit no order or reservation",async()=>
        {
            await CommerceContext.Reject("INVALID_INPUT",async()=>await checkout.Place(c.Customer,request with {PaymentMethod="QR"}));
            await CommerceContext.Reject("INVALID_INPUT",async()=>await checkout.Place(c.Customer,request with {Recipient=""}));
            var changed=false;try{await checkout.Place(c.Customer,request with {Lines=request.Lines!.Select(x=>x with{Price=1}).ToList()});}catch(PriceChangedException e){changed=e.Cart.Lines.Count==2;}
            Check.True(changed&&!await db.Orders.AnyAsync(x=>x.RequestKey==request.RequestKey)&&await db.Inventory.Where(x=>data.Product.Variants.Select(v=>v.Id).Contains(x.VariantId)).AllAsync(x=>x.Reserved==0),"Invalid checkout left state or omitted reconfirmation data.");
        });
        await Check.Run("K12 pending/unpaid snapshots, request-key retry and own-cart clearing are atomic",async()=>
        {
            var order=await checkout.Place(c.Customer,request);var retry=await checkout.Place(c.Customer,request);
            Check.True(order.Id==retry.Id&&order.Status=="CHO_XAC_NHAN"&&order.Payment=="CHUA_THU"&&order.Total==request.Lines!.Sum(x=>x.Quantity*x.Price),"Checkout snapshot/retry failed.");
            Check.True((await new CartService(kernel).Read(c.Customer)).Lines.Count==0&&await db.Movements.CountAsync(x=>x.OrderId==order.Id&&x.Kind=="GIU")==2,"Checkout cart/journal failed.");
            await CommerceContext.Reject("INVALID_STATE",async()=>await checkout.Place(c.Customer,request with {Address="Khác nội dung"}));
            using var resolved=await c.CustomerClient.Http.GetAsync("/api/orders/by-request/"+request.RequestKey);
            Check.True(resolved.IsSuccessStatusCode&&(await resolved.Content.ReadFromJsonAsync<OrderView>())!.Id==order.Id,"Request-key reconciliation failed.");
        });
        await Check.Run("K12 independent buyers racing for the last unit cannot oversell",async()=>
        {
            using var other=new CommerceContext(connection,address,"checkout-other");await other.Initialize();
            var last=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c,1),1);
            var a=await Cart(c,last);var b=await Cart(other,last);
            async Task<bool> Place(CommerceContext who,CheckoutInput input){await using var d=who.Open();try{await new CheckoutService(who.Kernel(d)).Place(who.Customer,input);return true;}catch(BusinessException e)when(e.Code=="OUT_OF_STOCK"){return false;}}
            var results=await Task.WhenAll(Place(c,a),Place(other,b));
            Check.True(results.Count(x=>x)==1&&(await db.Inventory.AsNoTracking().SingleAsync(x=>x.VariantId==last.Product.Variants[0].Id)).Reserved==1,"Last-unit race oversold.");
            var winner=results[0]?a:b;var loser=results[0]?other:c;
            await CommerceContext.Reject("FORBIDDEN",async()=>{await using var d=loser.Open();await new CheckoutService(loser.Kernel(d)).Place(loser.Customer,winner);});
        });
        await Check.Run("K12 late SQL payment failure restores all reservations, cart and document rows",async()=>
        {
            var fresh=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),2);var input=await Cart(c,fresh);
            var max=await db.Payments.Select(x=>(long?)x.OrderId).MaxAsync()??0;
            var sql="ALTER TABLE dbo.THANH_TOAN ADD CONSTRAINT CK_CheckoutLateFailure CHECK (OrderId<="+max.ToString(System.Globalization.CultureInfo.InvariantCulture)+")";
            await db.Database.ExecuteSqlRawAsync(sql);
            try{await CommerceContext.Reject("INVALID_INPUT",async()=>await checkout.Place(c.Customer,input));}
            finally{await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.THANH_TOAN DROP CONSTRAINT CK_CheckoutLateFailure");}
            Check.True(!await db.Orders.AnyAsync(x=>x.RequestKey==input.RequestKey)&&(await new CartService(kernel).Read(c.Customer)).Lines.Count==2&&
                await db.Inventory.Where(x=>fresh.Product.Variants.Select(v=>v.Id).Contains(x.VariantId)).AllAsync(x=>x.Reserved==0),"Late failure left partial checkout effects.");
        });
    }
}
