using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal sealed class MvcFlowTrace(string connection,long customer,long variant)
{
    private MvcCheckoutSql? previous;
    private readonly List<object> records=[];
    public int Steps=>records.Count;
    public async Task Start()=>previous=await MvcCheckoutSql.Read(connection);
    public async Task Record(string flow,string step,string effect,long orderId=0)
    {
        var before=previous!;var after=await MvcCheckoutSql.Read(connection);var old=before.Inventory.Single(x=>x.VariantId==variant);var stock=after.Inventory.Single(x=>x.VariantId==variant);
        var dh=effect=="dispatch"?-1:0;var dr=effect=="place"?1:effect is "cancel" or "dispatch"?-1:0;
        Check.True(stock.OnHand==old.OnHand+dh&&stock.Reserved==old.Reserved+dr,"Unexpected H/R transition at "+flow+"/"+step);
        foreach(var row in before.Inventory.Where(x=>x.VariantId!=variant))Check.True(JsonSerializer.Serialize(row)==JsonSerializer.Serialize(after.Inventory.Single(x=>x.VariantId==row.VariantId)),"Unrelated SKU changed in flow.");
        if(effect=="place")
        {
            await using var db=MvcCheckoutSql.Open(connection);var price=await db.Variants.Where(x=>x.Id==variant).Select(x=>x.Price).SingleAsync();var order=after.Orders.Single(x=>x.Id==orderId);
            MvcCheckoutSql.AssertOne(before,after,order.RequestKey,customer,variant,1,price);
        }
        else if(effect=="cancel")MvcOrdersAssertions.Cancel(before,after,orderId,customer,"Hủy luồng QA");
        else if(effect is "confirm" or "dispatch" or "complete")
        {
            var order=after.Orders.Single(x=>x.Id==orderId);var payment=after.Payments.Single(x=>x.OrderId==orderId);
            var expected=effect=="confirm"?"DA_XAC_NHAN":effect=="dispatch"?"DANG_GIAO":"HOAN_TAT";
            Check.True(order.Status==expected&&after.Orders.Count==before.Orders.Count&&after.History.Count==before.History.Count+1&&order.Total==before.Orders.Single(x=>x.Id==orderId).Total,"Admin lifecycle mismatch.");
            Check.True(effect=="complete"?payment.Status=="DA_THU"&&payment.Amount==order.Total&&order.CompletedAt.HasValue:payment.Status=="CHUA_THU"&&payment.Amount==0,"Admin COD mismatch.");
            Check.True(after.Journal.Count==before.Journal.Count+(effect=="dispatch"?1:0),"Admin journal delta mismatch.");
        }
        else
        {
            Check.True(JsonSerializer.Serialize(before.Orders)==JsonSerializer.Serialize(after.Orders)&&JsonSerializer.Serialize(before.Lines)==JsonSerializer.Serialize(after.Lines)&&JsonSerializer.Serialize(before.Payments)==JsonSerializer.Serialize(after.Payments)&&JsonSerializer.Serialize(before.History)==JsonSerializer.Serialize(after.History)&&JsonSerializer.Serialize(before.Journal)==JsonSerializer.Serialize(after.Journal),"Read/cart/reprice/expiry unexpectedly changed documents or stock history.");
        }
        object Summary(MvcCheckoutSql state)=>new { stock=state.Inventory.Select(x=>new {variantId=x.VariantId,H=x.OnHand,R=x.Reserved,available=x.OnHand-x.Reserved}),ownOrders=state.Orders.Count(x=>x.CustomerId==customer),cartQuantity=state.Cart.Where(x=>x.CustomerId==customer).Sum(x=>x.Quantity),historyRows=state.History.Count,journalRows=state.Journal.Count,orders=state.Orders.Where(x=>x.CustomerId==customer).Select(x=>new{x.Id,x.Status,x.Total,cod=state.Payments.Single(p=>p.OrderId==x.Id).Status}) };
        records.Add(new{flow,step,effect,before=Summary(before),after=Summary(after)});previous=after;
    }
    public Task Write(string path)=>File.WriteAllTextAsync(path,JsonSerializer.Serialize(new{qaOnly=true,steps=records.Count,perStepAssertionsPassed=true,records},new JsonSerializerOptions{WriteIndented=true}));
}
