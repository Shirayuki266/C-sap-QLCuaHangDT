using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Commerce;
public sealed partial class OrderWorkflow(BusinessKernel kernel)
{
    private async Task<Order> Load(Actor actor,long id,VersionedCommand command,bool admin)
    {
        await kernel.Require(actor,admin?"ADMIN":"CUSTOMER");
        var order=await kernel.Db.Orders.SingleOrDefaultAsync(x=>x.Id==id&&(admin||x.CustomerId==actor.AccountId))??throw Rules.Error("NOT_FOUND","Không tìm thấy đơn hàng.");
        Rules.Key(command.OperationKey);Rules.Version(order.Version,command.Version);
        var lines=await kernel.Db.OrderLines.AsNoTracking().Where(x=>x.OrderId==id).ToListAsync();
        if(lines.Count==0||lines.Sum(x=>x.Quantity*x.Price)!=order.Total)throw Rules.Error("INVALID_STATE","Tổng đơn không khớp dữ liệu chốt.");
        return order;
    }
    private async Task Unpaid(long id)
    {
        if(!await kernel.Db.Payments.AnyAsync(x=>x.OrderId==id&&x.Method=="COD"&&x.Status=="CHUA_THU"&&x.Amount==0))throw Rules.Error("INVALID_STATE","Trạng thái thanh toán không hợp lệ cho thao tác này.");
    }
    private void History(Actor actor,Order order,string from,string? reason=null)=>kernel.Db.OrderHistory.Add(new OrderHistory{OrderId=order.Id,ActorId=actor.AccountId,From=from,To=order.Status,Reason=reason,At=kernel.Now});
    private async Task KeyUnused(Guid key)
    {
        if(await kernel.Db.Movements.AnyAsync(x=>x.OperationKey==key))throw Rules.Error("INVALID_STATE","Thao tác đã được ghi nhận. Vui lòng tra cứu đơn/nhật ký trước khi thử lại.");
    }
    public Task<bool> Confirm(Actor actor,long id,VersionedCommand command)=>kernel.Write(async()=>
    {
        var order=await Load(actor,id,command,true);await Unpaid(id);
        if(order.Status!="CHO_XAC_NHAN")throw Rules.Error("INVALID_STATE","Chỉ được xác nhận đơn đang chờ.");
        order.Status="DA_XAC_NHAN";History(actor,order,"CHO_XAC_NHAN");return true;
    });
    public Task<bool> Cancel(Actor actor,long id,VersionedCommand command,bool admin=false)=>kernel.Write(async()=>
    {
        var order=await Load(actor,id,command,admin);await Unpaid(id);
        if(order.Status!="CHO_XAC_NHAN"&&!(admin&&order.Status=="DA_XAC_NHAN"))throw Rules.Error("INVALID_STATE","Không được hủy đơn ở trạng thái hiện tại.");
        var reason=Rules.Text(command.Reason,1000,"Lý do hủy đơn");await KeyUnused(command.OperationKey);
        var lines=await kernel.Db.OrderLines.AsNoTracking().Where(x=>x.OrderId==id).OrderBy(x=>x.VariantId).ToListAsync();
        foreach(var line in lines)
        {
            if(await kernel.Db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.TON_KHO SET Reserved=Reserved-{line.Quantity} WHERE VariantId={line.VariantId} AND Reserved>={line.Quantity}")!=1)throw Rules.Error("CONCURRENT_UPDATE","Lượng giữ hàng không còn khớp. Không có hủy đơn dở dang.");
            kernel.Db.Movements.Add(new StockMovement{VariantId=line.VariantId,ActorId=actor.AccountId,OrderId=id,Kind="HUY_GIU",DeltaReserved=-line.Quantity,At=kernel.Now,Reason=reason,OperationKey=command.OperationKey});
        }
        var previous=order.Status;order.Status="DA_HUY";History(actor,order,previous,reason);return true;
    });
}
