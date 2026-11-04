using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Commerce;
internal static class OrderReadModel
{
    public static async Task<OrderView> Load(StoreDbContext db,long id)
    {
        var order=await db.Orders.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id)??throw Rules.Error("NOT_FOUND","Không tìm thấy đơn hàng.");
        var payment=await db.Payments.AsNoTracking().SingleOrDefaultAsync(x=>x.OrderId==id)??throw Rules.Error("INVALID_STATE","Đơn thiếu dữ liệu thanh toán. Vui lòng kiểm tra dữ liệu.");
        var lines=await db.OrderLines.AsNoTracking().Where(x=>x.OrderId==id).OrderBy(x=>x.Id).Select(x=>new OrderLineView(x.VariantId,x.Name,x.Sku,x.Description,x.Quantity,x.Price,x.Quantity*x.Price)).ToListAsync();
        var history=await (from h in db.OrderHistory.AsNoTracking() join a in db.Accounts on h.ActorId equals a.Id where h.OrderId==id orderby h.Id select new{h,a.Username}).ToListAsync();
        return new(order.Id,order.Code,order.CustomerId,order.Recipient,order.Phone,order.Address,order.Status,payment.Status,order.Total,
            new(DateTime.SpecifyKind(order.CreatedAt,DateTimeKind.Utc)),order.CompletedAt.HasValue?new DateTimeOffset(DateTime.SpecifyKind(order.CompletedAt.Value,DateTimeKind.Utc)):null,
            order.RequestKey,Convert.ToBase64String(order.Version),await db.Movements.AnyAsync(x=>x.OrderId==id&&x.Kind=="NHAN_LAI"),lines,
            history.Select(x=>new OrderHistoryView(x.h.Id,x.h.From,x.h.To,x.h.ActorId,x.Username,new(DateTime.SpecifyKind(x.h.At,DateTimeKind.Utc)),x.h.Reason)).ToList());
    }
}
