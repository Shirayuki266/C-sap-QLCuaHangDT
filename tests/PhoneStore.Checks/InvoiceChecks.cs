using System.Net;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class InvoiceChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"invoice");await c.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),2);var input=await CheckoutChecks.Cart(c,data);
        await using var db=c.Open();var k=c.Kernel(db);var order=await new CheckoutService(k).Place(c.Customer,input);var s=new InvoiceService(k);
        await Check.Run("A21 invoice eligibility and immutable snapshots are read-only and ADMIN-scoped",async()=>
        {
            await CommerceContext.Reject("INVALID_STATE",async()=>await s.Read(c.Admin,order.Id));
            await new OrderWorkflow(k).Confirm(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));
            var stock=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).Select(x=>new{x.VariantId,x.OnHand,x.Reserved}).ToListAsync();var movements=await db.Movements.CountAsync();var histories=await db.OrderHistory.CountAsync();
            var a=await s.Read(c.Admin,order.Id);var b=await s.Read(c.Admin,order.Id);
            Check.True(a.Order.Total==order.Total&&a.Order.Lines.SequenceEqual(order.Lines)&&b.Order.Lines.SequenceEqual(order.Lines)&&a.Notice.Contains("không phải"),"Invoice did not preserve snapshots or internal document notice.");
            Check.True((await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).Select(x=>new{x.VariantId,x.OnHand,x.Reserved}).ToListAsync()).SequenceEqual(stock)&&await db.Movements.CountAsync()==movements&&await db.OrderHistory.CountAsync()==histories&&(await db.Payments.AsNoTracking().SingleAsync(x=>x.OrderId==order.Id)).Amount==0,"Repeated invoice queries changed stock/payment/history.");
            using var denied=await c.CustomerClient.Http.GetAsync($"/api/admin/orders/{order.Id}/invoice");using var ok=await c.AdminClient.Http.GetAsync($"/api/admin/orders/{order.Id}/invoice");
            Check.True(denied.StatusCode==HttpStatusCode.Forbidden&&ok.IsSuccessStatusCode,"Invoice HTTP authorization failed.");
        });
    }
}
