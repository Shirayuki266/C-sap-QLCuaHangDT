using System.Text.Json;

namespace PhoneStore.Checks;

internal static class MvcOrdersAssertions
{
    internal static void Cancel(MvcCheckoutSql before,MvcCheckoutSql after,long id,long customer,string reason)
    {
        string J(object value)=>JsonSerializer.Serialize(value);
        var original=before.Orders.Single(x=>x.Id==id);var order=after.Orders.Single(x=>x.Id==id);var lines=before.Lines.Where(x=>x.OrderId==id).ToList();
        Check.True(order.Status=="DA_HUY"&&!order.Version.SequenceEqual(original.Version)&&order.CustomerId==customer&&order.Code==original.Code&&order.RequestKey==original.RequestKey&&order.Total==original.Total&&order.CreatedAt==original.CreatedAt&&order.CompletedAt==original.CompletedAt&&order.Recipient==original.Recipient&&order.Phone==original.Phone&&order.Address==original.Address,"Cancel changed snapshot/money or failed state/version.");
        Check.True(J(before.Orders.Where(x=>x.Id!=id))==J(after.Orders.Where(x=>x.Id!=id))&&J(before.Lines)==J(after.Lines)&&J(before.Payments)==J(after.Payments)&&J(before.Cart)==J(after.Cart),"Cancel changed unrelated order/line/payment/cart data.");
        var payment=after.Payments.Single(x=>x.OrderId==id);Check.True(payment.Status=="CHUA_THU"&&payment.Amount==0&&payment.Method=="COD","Cancel changed unpaid COD.");
        foreach(var stock in before.Inventory)
        {
            var current=after.Inventory.Single(x=>x.VariantId==stock.VariantId);var delta=lines.Where(x=>x.VariantId==stock.VariantId).Sum(x=>x.Quantity);
            Check.True(current.OnHand==stock.OnHand&&current.Reserved==stock.Reserved-delta&&(delta>0||stock.Version.SequenceEqual(current.Version)),"Cancel H/R delta incorrect.");
        }
        var history=after.History.Where(x=>!before.History.Any(old=>old.Id==x.Id)).ToList();var movements=after.Journal.Where(x=>!before.Journal.Any(old=>old.Id==x.Id)).ToList();
        Check.True(history.Count==1&&history[0].OrderId==id&&history[0].From=="CHO_XAC_NHAN"&&history[0].To=="DA_HUY"&&history[0].Reason==reason.Trim()&&history[0].ActorId==customer&&
            after.History.Count==before.History.Count+1&&J(before.History)==J(after.History.Where(x=>before.History.Any(old=>old.Id==x.Id))),"Cancel history delta incorrect.");
        Check.True(movements.Count==lines.Count&&after.Journal.Count==before.Journal.Count+lines.Count&&movements.All(x=>x.OrderId==id&&x.Kind=="HUY_GIU"&&x.DeltaOnHand==0&&x.DeltaReserved==-lines.Single(line=>line.VariantId==x.VariantId).Quantity&&x.Reason==reason.Trim())&&
            movements.Select(x=>x.OperationKey).Distinct().Count()==1&&J(before.Journal)==J(after.Journal.Where(x=>before.Journal.Any(old=>old.Id==x.Id))),"Cancel journal delta incorrect.");
    }
}
