using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Commerce;
public sealed partial class OrderWorkflow
{
    public Task<bool> Dispatch(Actor actor,long id,VersionedCommand command)=>kernel.Write(async()=>
    {
        var order=await Load(actor,id,command,true);await Unpaid(id);
        if(order.Status!="DA_XAC_NHAN")throw Rules.Error("INVALID_STATE","Chỉ được xuất giao đơn đã xác nhận.");
        await KeyUnused(command.OperationKey);
        foreach(var line in await kernel.Db.OrderLines.AsNoTracking().Where(x=>x.OrderId==id).OrderBy(x=>x.VariantId).ToListAsync())
        {
            if(await kernel.Db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.TON_KHO SET OnHand=OnHand-{line.Quantity}, Reserved=Reserved-{line.Quantity} WHERE VariantId={line.VariantId} AND OnHand>={line.Quantity} AND Reserved>={line.Quantity}")!=1)
                throw Rules.Error("CONCURRENT_UPDATE","Tồn kho không khớp lượng giữ hàng. Không có xuất giao dở dang.");
            kernel.Db.Movements.Add(new StockMovement{VariantId=line.VariantId,OrderId=id,ActorId=actor.AccountId,Kind="XUAT_GIAO",DeltaOnHand=-line.Quantity,DeltaReserved=-line.Quantity,At=kernel.Now,OperationKey=command.OperationKey});
        }
        order.Status="DANG_GIAO";History(actor,order,"DA_XAC_NHAN");return true;
    });
    public Task<bool> Complete(Actor actor,long id,VersionedCommand command)=>kernel.Write(async()=>
    {
        var order=await Load(actor,id,command,true);await Unpaid(id);
        if(order.Status!="DANG_GIAO")throw Rules.Error("INVALID_STATE","Chỉ được hoàn tất đơn đang giao.");
        var payment=await kernel.Db.Payments.SingleAsync(x=>x.OrderId==id);
        var completedAt=kernel.Now;
        payment.Status="DA_THU";payment.Amount=order.Total;payment.ActorId=actor.AccountId;payment.CollectedAt=completedAt;
        order.Status="HOAN_TAT";order.CompletedAt=completedAt;History(actor,order,"DANG_GIAO");return true;
    });
    public Task<bool> Fail(Actor actor,long id,VersionedCommand command)=>kernel.Write(async()=>
    {
        var order=await Load(actor,id,command,true);await Unpaid(id);
        if(order.Status!="DANG_GIAO")throw Rules.Error("INVALID_STATE","Chỉ được ghi thất bại cho đơn đang giao.");
        var reason=Rules.Text(command.Reason,1000,"Lý do giao thất bại");
        order.Status="GIAO_THAT_BAI";History(actor,order,"DANG_GIAO",reason);return true;
    });
    public Task<bool> ReceiveReturn(Actor actor,long id,VersionedCommand command)=>kernel.Write(async()=>
    {
        var order=await Load(actor,id,command,true);await Unpaid(id);
        if(order.Status!="GIAO_THAT_BAI"||await kernel.Db.Movements.AnyAsync(x=>x.OrderId==id&&x.Kind=="NHAN_LAI"))throw Rules.Error("INVALID_STATE","Chỉ nhận lại một lần hàng của đơn giao thất bại.");
        var reason=Rules.Text(command.Reason,1000,"Lý do nhận lại hàng");await KeyUnused(command.OperationKey);
        foreach(var line in await kernel.Db.OrderLines.AsNoTracking().Where(x=>x.OrderId==id).OrderBy(x=>x.VariantId).ToListAsync())
        {
            var max=int.MaxValue-line.Quantity;
            if(await kernel.Db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.TON_KHO SET OnHand=OnHand+{line.Quantity} WHERE VariantId={line.VariantId} AND OnHand<={max}")!=1)throw Rules.Error("INVALID_INPUT","Tồn kho vượt giới hạn. Chưa ghi nhận nhận lại hàng.");
            kernel.Db.Movements.Add(new StockMovement{VariantId=line.VariantId,OrderId=id,ActorId=actor.AccountId,Kind="NHAN_LAI",DeltaOnHand=line.Quantity,At=kernel.Now,Reason=reason,OperationKey=command.OperationKey});
        }
        // Advance the version even though the failed delivery state stays unchanged.
        kernel.Db.Entry(order).Property(x=>x.Status).IsModified=true;
        History(actor,order,"GIAO_THAT_BAI",reason);return true;
    });
}
