using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal sealed record MvcCheckoutSql(List<Inventory> Inventory, List<CartLine> Cart, List<Order> Orders,
    List<OrderLine> Lines, List<Payment> Payments, List<OrderHistory> History, List<StockMovement> Journal)
{
    internal static StoreDbContext Open(string connection) => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
    internal static async Task<MvcCheckoutSql> Read(string connection)
    {
        await using var db = Open(connection);
        return new(await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync(), await db.Cart.AsNoTracking().OrderBy(x=>x.CustomerId).ThenBy(x=>x.VariantId).ToListAsync(),
            await db.Orders.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(), await db.OrderLines.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            await db.Payments.AsNoTracking().OrderBy(x=>x.OrderId).ToListAsync(), await db.OrderHistory.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(), await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync());
    }
    public string Snapshot() => JsonSerializer.Serialize(this);
    internal static long AssertOne(MvcCheckoutSql before, MvcCheckoutSql after, Guid key, long customer, long variant, int quantity, decimal price)
    {
        var order = after.Orders.Single(x=>x.RequestKey==key);
        Check.True(after.Orders.Count==before.Orders.Count+1 && order.CustomerId==customer && order.Status=="CHO_XAC_NHAN" && order.Total==quantity*price, "Checkout order delta/owner/total/state incorrect.");
        var line=after.Lines.Single(x=>x.OrderId==order.Id); var payment=after.Payments.Single(x=>x.OrderId==order.Id); var history=after.History.Single(x=>x.OrderId==order.Id); var movement=after.Journal.Single(x=>x.OrderId==order.Id);
        Check.True(after.Lines.Count==before.Lines.Count+1 && line.VariantId==variant && line.Quantity==quantity && line.Price==price &&
            after.Payments.Count==before.Payments.Count+1 && payment.Status=="CHUA_THU" && payment.Method=="COD" && payment.Amount==0 &&
            after.History.Count==before.History.Count+1 && history.From is null && history.To=="CHO_XAC_NHAN" &&
            after.Journal.Count==before.Journal.Count+1 && movement.Kind=="GIU" && movement.DeltaOnHand==0 && movement.DeltaReserved==quantity && movement.OperationKey==key, "Checkout line/payment/history/journal delta incorrect.");
        foreach(var old in before.Inventory)
        {
            var current=after.Inventory.Single(x=>x.VariantId==old.VariantId);
            Check.True(current.OnHand==old.OnHand && current.Reserved==old.Reserved+(old.VariantId==variant?quantity:0) &&
                (old.VariantId==variant || current.Version.SequenceEqual(old.Version)), "Checkout inventory H/R/version delta incorrect.");
        }
        Check.True(!after.Cart.Any(x=>x.CustomerId==customer) && JsonSerializer.Serialize(before.Cart.Where(x=>x.CustomerId!=customer))==JsonSerializer.Serialize(after.Cart.Where(x=>x.CustomerId!=customer)), "Checkout failed to clear own cart or changed another cart.");
        Check.True(JsonSerializer.Serialize(before.Orders)==JsonSerializer.Serialize(after.Orders.Where(x=>x.Id!=order.Id)) &&
            JsonSerializer.Serialize(before.Payments)==JsonSerializer.Serialize(after.Payments.Where(x=>x.OrderId!=order.Id)) &&
            JsonSerializer.Serialize(before.Lines)==JsonSerializer.Serialize(after.Lines.Where(x=>x.OrderId!=order.Id)) &&
            JsonSerializer.Serialize(before.History)==JsonSerializer.Serialize(after.History.Where(x=>x.OrderId!=order.Id)) &&
            JsonSerializer.Serialize(before.Journal)==JsonSerializer.Serialize(after.Journal.Where(x=>x.OrderId!=order.Id)), "Checkout altered unrelated order/payment/history/journal snapshots.");
        return order.Id;
    }
}
