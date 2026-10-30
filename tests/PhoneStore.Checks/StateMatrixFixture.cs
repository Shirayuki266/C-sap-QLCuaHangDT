using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal sealed record MatrixSnapshot(List<Order> Orders,List<OrderLine> Lines,List<Payment> Payments,
    List<Inventory> Stock,List<Variant> Variants,List<OrderHistory> History,List<StockMovement> Movements,
    List<Purchase> Purchases,List<PurchaseLine> PurchaseLines,List<CartLine> Cart)
{
    public string Json()=>JsonSerializer.Serialize(this);
}

internal sealed class StateMatrixFixture(CommerceContext context,CommerceData data)
{
    public CommerceContext Context=>context;
    public CommerceData Data=>data;
    public static readonly string[] States=["CHO_XAC_NHAN","DA_XAC_NHAN","DANG_GIAO","HOAN_TAT","DA_HUY","GIAO_THAT_BAI"];
    public async Task<MatrixSnapshot> Snapshot()
    {
        await using var db=context.Open();
        return new(await db.Orders.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            await db.OrderLines.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            await db.Payments.AsNoTracking().OrderBy(x=>x.OrderId).ToListAsync(),
            await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync(),
            await db.Variants.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            await db.OrderHistory.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            await db.Purchases.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            await db.PurchaseLines.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            await db.Cart.AsNoTracking().OrderBy(x=>x.CustomerId).ThenBy(x=>x.VariantId).ToListAsync());
    }
    public static void Same<T>(T expected,T actual,string label)=>Check.True(JsonSerializer.Serialize(expected)==JsonSerializer.Serialize(actual),label);
    public async Task Unchanged(MatrixSnapshot before)=>Same(before,await Snapshot(),"Rejected/read-only matrix request changed the database.");
    public async Task<OrderView> Read(long id)
    {
        await using var db=context.Open();return await new OrderQueryService(context.Kernel(db)).Read(context.Admin,id,true);
    }
    public async Task<OrderView> CreateOrder(string state)
    {
        OrderView order;
        await using(var db=context.Open())
        {
            var kernel=context.Kernel(db);var cart=new CartService(kernel);
            foreach(var line in (await cart.Read(context.Customer)).Lines)await cart.Set(context.Customer,new(line.VariantId,0));
            for(var i=0;i<2;i++)await cart.Set(context.Customer,new(data.Product.Variants[i].Id,i+2));
            var current=await cart.Read(context.Customer);
            order=await new CheckoutService(kernel).Place(context.Customer,new(Guid.NewGuid(),"Matrix recipient","0909999999","Matrix fixture address",
                current.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price!.Value)).ToList()));
        }
        if(state==States[4])await Apply(order,"admin-cancel",new(order.Version,Guid.NewGuid(),"Fixture cancellation"));
        else if(state!=States[0])
        {
            await Apply(order,"confirm",new(order.Version,Guid.NewGuid()));order=await Read(order.Id);
            if(state!=States[1])
            {
                await Apply(order,"dispatch",new(order.Version,Guid.NewGuid()));order=await Read(order.Id);
                if(state==States[3])await Apply(order,"complete",new(order.Version,Guid.NewGuid()));
                if(state==States[5])await Apply(order,"fail",new(order.Version,Guid.NewGuid(),"Fixture failure"));
            }
        }
        return await Read(order.Id);
    }
    public async Task Apply(OrderView order,string action,VersionedCommand command)
    {
        await using var db=context.Open();var kernel=context.Kernel(db);var flow=new OrderWorkflow(kernel);
        switch(action)
        {
            case "customer-cancel":await flow.Cancel(context.Customer,order.Id,command);break;
            case "admin-cancel":await flow.Cancel(context.Admin,order.Id,command,true);break;
            case "confirm":await flow.Confirm(context.Admin,order.Id,command);break;
            case "dispatch":await flow.Dispatch(context.Admin,order.Id,command);break;
            case "complete":await flow.Complete(context.Admin,order.Id,command);break;
            case "fail":await flow.Fail(context.Admin,order.Id,command);break;
            case "receive-return":await flow.ReceiveReturn(context.Admin,order.Id,command);break;
            case "invoice":
                var invoice=await new InvoiceService(kernel).Read(context.Admin,order.Id);
                Check.True(invoice.Order.Id==order.Id&&invoice.Order.Total==order.Total,"Invoice returned another order or total.");
                Same(order.Lines,invoice.Order.Lines,"Invoice changed snapshot lines.");break;
            default:throw new ArgumentOutOfRangeException(nameof(action));
        }
    }
    public async Task AssertOrderEffect(MatrixSnapshot before,OrderView order,OrderMatrixCase action)
    {
        var after=await Snapshot();
        if(action.Name=="invoice"){Same(before,after,"Invoice mutated the database.");return;}
        var header=after.Orders.Single(x=>x.Id==order.Id);
        Check.True(header.Status==action.Target&&!Convert.ToBase64String(header.Version).Equals(order.Version,StringComparison.Ordinal),"Order matrix status/version wrong.");
        Same(before.Orders.Where(x=>x.Id!=order.Id),after.Orders.Where(x=>x.Id!=order.Id),"Another order changed.");
        Same(before.Lines,after.Lines,"Order snapshots changed.");Same(before.Variants,after.Variants,"Order transition repriced catalog.");
        Same(before.Purchases,after.Purchases,"Order changed receipts.");Same(before.PurchaseLines,after.PurchaseLines,"Order changed receipt lines.");Same(before.Cart,after.Cart,"Order transition changed cart.");
        foreach(var old in before.Stock)
        {
            var line=order.Lines.SingleOrDefault(x=>x.VariantId==old.VariantId);var q=line?.Quantity??0;var now=after.Stock.Single(x=>x.VariantId==old.VariantId);
            Check.True(now.OnHand==old.OnHand+action.DeltaH*q&&now.Reserved==old.Reserved+action.DeltaR*q,"Order matrix H/R delta wrong.");
            if(q==0||action.DeltaH==0&&action.DeltaR==0)Same(old,now,"Untouched inventory row/version changed.");
        }
        var payment=after.Payments.Single(x=>x.OrderId==order.Id);
        if(action.Name=="complete")Check.True(payment.Method=="COD"&&payment.Status=="DA_THU"&&payment.Amount==order.Total&&payment.ActorId==context.Admin.AccountId&&payment.CollectedAt.HasValue&&payment.CollectedAt==header.CompletedAt,"Completion/COD atomic values wrong.");
        else {Same(before.Payments.Single(x=>x.OrderId==order.Id),payment,"Non-completion action changed COD.");Check.True(payment.Status=="CHUA_THU"&&payment.Amount==0&&payment.CollectedAt==null&&payment.ActorId==null&&header.CompletedAt==null,"Unpaid matrix values wrong.");}
        Same(before.Payments.Where(x=>x.OrderId!=order.Id),after.Payments.Where(x=>x.OrderId!=order.Id),"Another payment changed.");
        var addedHistory=after.History.Where(x=>!before.History.Any(y=>y.Id==x.Id)).ToList();
        Same(before.History,after.History.Where(x=>before.History.Any(y=>y.Id==x.Id)).ToList(),"Existing history changed.");
        Check.True(action.Name=="receive-return"?addedHistory.Count is 0 or 1:addedHistory.Count==1,"Transition history count wrong.");
        Check.True(addedHistory.All(x=>x.OrderId==order.Id&&x.From==order.Status&&x.To==action.Target&&x.ActorId==(action.Name=="customer-cancel"?context.Customer.AccountId:context.Admin.AccountId)),"History state/actor wrong.");
        var added=after.Movements.Where(x=>!before.Movements.Any(y=>y.Id==x.Id)).ToList();
        Same(before.Movements,after.Movements.Where(x=>before.Movements.Any(y=>y.Id==x.Id)).ToList(),"Existing stock journal changed.");
        Check.True(added.Count==(action.Kind is null?0:order.Lines.Count),"Movement count differs from section 3.");
        foreach(var movement in added){var q=order.Lines.Single(x=>x.VariantId==movement.VariantId).Quantity;Check.True(movement.OrderId==order.Id&&movement.PurchaseId==null&&movement.Kind==action.Kind&&movement.ActorId==(action.Name=="customer-cancel"?context.Customer.AccountId:context.Admin.AccountId)&&movement.DeltaOnHand==action.DeltaH*q&&movement.DeltaReserved==action.DeltaR*q,"Movement kind/actor/delta wrong.");}
    }
}

// Independent expected table transcribed from sections 3 and 4, not inferred from service guards.
internal sealed record OrderMatrixCase(string Name,int[] Allowed,string Target,int DeltaH=0,int DeltaR=0,string? Kind=null)
{
    public static readonly OrderMatrixCase[] All=[
        new("customer-cancel",[0],"DA_HUY",0,-1,"HUY_GIU"),
        new("confirm",[0],"DA_XAC_NHAN"),
        new("admin-cancel",[0,1],"DA_HUY",0,-1,"HUY_GIU"),
        new("dispatch",[1],"DANG_GIAO",-1,-1,"XUAT_GIAO"),
        new("complete",[2],"HOAN_TAT"),
        new("fail",[2],"GIAO_THAT_BAI"),
        new("receive-return",[5],"GIAO_THAT_BAI",1,0,"NHAN_LAI"),
        new("invoice",[1,2,3],"")];
}
