using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Contracts;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Services;
public sealed partial class StoreService
{
    private IQueryable<SalesOrder> RevenueOrders=>db.Orders.AsNoTracking().Where(x=>x.Status=="HOAN_TAT"&&x.Payment.Status=="DA_THU");
    private IQueryable<SalesOrder> OrderQuery=>db.Orders.AsSingleQuery().Include(x=>x.Customer).Include(x=>x.Payment).Include(x=>x.Lines).Include(x=>x.History).ThenInclude(x=>x.Actor);
    private static OrderDto MapOrder(SalesOrder o,bool returned)=>new(o.Id,o.Code,o.CustomerId,o.Customer.FullName,o.Recipient,o.Phone,o.Address,o.Status,o.Payment.Status,o.Total,o.CreatedAt,o.CompletedAt,returned,Convert.ToBase64String(o.Version),o.Lines.OrderBy(x=>x.Id).Select(x=>new OrderLineDto(x.VariantId,x.Name,x.Sku,x.Description,x.Quantity,x.Price)).ToList(),o.History.OrderBy(x=>x.At).ThenBy(x=>x.Id).Select(x=>new HistoryDto(x.From,x.To,x.At,x.Actor.Username,x.Reason)).ToList());
    public async Task<OrderDto> GetOrder(long actor,long id,bool admin=false) {
        await Require(actor,admin?"ADMIN":"CUSTOMER"); var o=await OrderQuery.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&(admin||x.CustomerId==actor))??throw Error("NOT_FOUND","Không tìm thấy đơn hàng.");
        return MapOrder(o,await db.Movements.AnyAsync(x=>x.OrderId==id&&x.Kind=="NHAN_LAI"));
    }
    private static (DateTime? Start,DateTime? End) Bounds(DateOnly? from,DateOnly? to) {
        if(from>to) throw Error("INVALID_INPUT","Từ ngày không được sau đến ngày.");
        if(to==DateOnly.MaxValue||from==DateOnly.MinValue) throw Error("INVALID_INPUT","Ngày ngoài phạm vi hỗ trợ.");
        return(from?.ToDateTime(TimeOnly.MinValue).AddHours(-7),to?.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7));
    }
    public async Task<Page<OrderDto>> GetOrders(long actor,bool admin=false,string? query=null,string? status=null,DateOnly? from=null,DateOnly? to=null,long? customerId=null,int page=1) {
        await Require(actor,admin?"ADMIN":"CUSTOMER"); var dates=Bounds(from,to);
        var q=OrderQuery.AsNoTracking().Where(x=>admin||x.CustomerId==actor);
        if(customerId.HasValue&&admin) q=q.Where(x=>x.CustomerId==customerId);
        if(!string.IsNullOrWhiteSpace(query)) q=q.Where(x=>x.Code.Contains(query)||x.Customer.FullName.Contains(query)||x.Phone.Contains(query));
        if(!string.IsNullOrWhiteSpace(status)) q=q.Where(x=>x.Status==status);
        if(dates.Start.HasValue) q=q.Where(x=>x.CreatedAt>=dates.Start); if(dates.End.HasValue) q=q.Where(x=>x.CreatedAt<dates.End);
        var total=await q.CountAsync(); page=Math.Clamp(page,1,100000);
        var orders=await q.OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id).Skip((page-1)*20).Take(20).ToListAsync();
        var ids=orders.Select(x=>x.Id).ToArray(); var returns=await db.Movements.Where(x=>x.OrderId.HasValue&&ids.Contains(x.OrderId.Value)&&x.Kind=="NHAN_LAI").Select(x=>x.OrderId!.Value).Distinct().ToListAsync();
        return new(orders.Select(o=>MapOrder(o,returns.Contains(o.Id))).ToList(),total,page,20);
    }
    public Task<long> PlaceOrder(long actor,CheckoutRequest input)=>Atomic(async()=> {
        if(input.PaymentMethod!="COD")throw Error("INVALID_INPUT","Chỉ hỗ trợ thanh toán COD. Thanh toán QR sắp ra mắt, chưa thể đặt đơn theo phương thức này.");
        await Require(actor,"CUSTOMER"); Key(input.RequestKey);
        var existing=await db.Orders.AsNoTracking().SingleOrDefaultAsync(x=>x.RequestKey==input.RequestKey);
        if(existing!=null) {if(existing.CustomerId!=actor) throw Error("FORBIDDEN","Khóa yêu cầu không hợp lệ.");return existing.Id;}
        var recipient=Text(input.Recipient,120,"Người nhận"); var phone=Phone(input.Phone); var address=Text(input.Address,500,"Địa chỉ"); if(address.Length<10) throw Error("INVALID_INPUT","Vui lòng nhập địa chỉ đầy đủ ít nhất 10 ký tự.");
        var cart=await CartQuery(actor).ToListAsync();
        if(cart.Count==0||input.Lines is null||input.Lines.Any(x=>x is null)||cart.Count!=input.Lines.Count||input.Lines.Select(x=>x.VariantId).Distinct().Count()!=input.Lines.Count) throw Error("INVALID_INPUT","Giỏ hàng đã thay đổi hoặc đang trống. Vui lòng kiểm tra lại.");
        var order=new SalesOrder{CustomerId=actor,RequestKey=input.RequestKey,Recipient=recipient,Phone=phone,Address=address,Code="DH"+Guid.NewGuid().ToString("N")[..24],Payment=new Payment()};
        foreach(var line in cart.OrderBy(x=>x.VariantId)) {
            var v=await Sellable(line.VariantId); var agreed=input.Lines.SingleOrDefault(x=>x.VariantId==line.VariantId);
            if(agreed is null||agreed.Quantity!=line.Quantity||agreed.Price!=v.Price) throw Error("PRICE_CHANGED","Giá hoặc số lượng đã thay đổi. Vui lòng xem lại giỏ và xác nhận giá mới.");
            var qty=line.Quantity; var id=v.Id;
            if(await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE TON_KHO SET Reserved=Reserved+{qty} WHERE VariantId={id} AND OnHand-Reserved>={qty}")!=1) throw Error("OUT_OF_STOCK",$"{v.Product.Name}: không đủ hàng. Chưa có mặt hàng nào được đặt.");
            order.Lines.Add(new SalesLine{VariantId=id,Name=v.Product.Name,Sku=v.Sku,Description=$"{v.Color} · {v.StorageGb} GB",Quantity=qty,Price=v.Price});
        }
        order.Total=order.Lines.Sum(x=>x.Quantity*x.Price); if(order.Total>9999999999999999m) throw Error("INVALID_INPUT","Tổng đơn vượt phạm vi hỗ trợ."); db.Orders.Add(order); await db.SaveChangesAsync();
        foreach(var line in order.Lines) db.Movements.Add(new Movement{VariantId=line.VariantId,ActorId=actor,OrderId=order.Id,Kind="GIU",DeltaReserved=line.Quantity,OperationKey=input.RequestKey});
        db.Histories.Add(new OrderHistory{OrderId=order.Id,ActorId=actor,To=order.Status}); db.Cart.RemoveRange(cart); return order.Id;
    });
    public Task<bool> Transition(long actor,long id,string action,TransitionRequest input,bool admin)=>Atomic(async()=> {
        await Require(actor,admin?"ADMIN":"CUSTOMER"); Key(input.OperationKey);
        var o=await db.Orders.Include(x=>x.Lines).Include(x=>x.Payment).SingleOrDefaultAsync(x=>x.Id==id&&(admin||x.CustomerId==actor))??throw Error("NOT_FOUND","Không tìm thấy đơn hàng.");
        Version(o.Version,input.Version); var before=o.Status; var reason=Text(input.Reason,500,"Lý do",action is not ("cancel" or "fail")); string? movement=null;
        if(!admin&&action!="cancel") throw Error("FORBIDDEN","Bạn không có quyền xử lý đơn.");
        switch(action) {
            case "confirm" when o.Status=="CHO_XAC_NHAN": o.Status="DA_XAC_NHAN"; break;
            case "cancel" when o.Status=="CHO_XAC_NHAN"||(admin&&o.Status=="DA_XAC_NHAN"): o.Status="DA_HUY"; movement="HUY_GIU"; break;
            case "dispatch" when o.Status=="DA_XAC_NHAN": o.Status="DANG_GIAO"; movement="XUAT_GIAO"; break;
            case "complete" when o.Status=="DANG_GIAO": o.Status="HOAN_TAT"; o.CompletedAt=DateTime.UtcNow; o.Payment.Status="DA_THU"; o.Payment.Amount=o.Total; o.Payment.CollectedAt=o.CompletedAt; o.Payment.ActorId=actor; break;
            case "fail" when o.Status=="DANG_GIAO": o.Status="GIAO_THAT_BAI"; break;
            case "return" when o.Status=="GIAO_THAT_BAI":
                if(await db.Movements.AnyAsync(x=>x.OrderId==id&&x.Kind=="NHAN_LAI")) throw Error("INVALID_STATE","Đơn hàng đã nhận lại kho."); movement="NHAN_LAI";
                // Touch the order to advance rowversion even though the status is unchanged.
                db.Entry(o).Property(x=>x.Status).IsModified=true; break;
            default: throw Error("INVALID_STATE","Thao tác không phù hợp trạng thái hiện tại.");
        }
        if(movement!=null) foreach(var line in o.Lines.OrderBy(x=>x.VariantId)) {
            var q=line.Quantity; var v=line.VariantId; var rows=movement switch {
                "HUY_GIU"=>await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE TON_KHO SET Reserved=Reserved-{q} WHERE VariantId={v} AND Reserved>={q}"),
                "XUAT_GIAO"=>await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE TON_KHO SET OnHand=OnHand-{q},Reserved=Reserved-{q} WHERE VariantId={v} AND Reserved>={q} AND OnHand>={q}"),
                _=>await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE TON_KHO SET OnHand=OnHand+{q} WHERE VariantId={v}")};
            if(rows!=1) throw Error("INVALID_STATE","Số dư kho không hợp lệ; toàn bộ thao tác đã hoàn tác.");
            db.Movements.Add(new Movement{VariantId=v,ActorId=actor,OrderId=id,Kind=movement,DeltaOnHand=movement=="XUAT_GIAO"?-q:movement=="NHAN_LAI"?q:0,DeltaReserved=movement=="NHAN_LAI"?0:-q,OperationKey=input.OperationKey,Reason=reason});
        }
        db.Histories.Add(new OrderHistory{OrderId=id,From=before,To=o.Status,ActorId=actor,Reason=action=="return"?"Đã nhận lại toàn bộ hàng thực tế. "+reason:reason}); return true;
    });
    public async Task<ReportResult> Report(long actor,string kind,DateOnly? from,DateOnly? to) {
        await Require(actor,"ADMIN");
        if(kind=="stock") {var rows=await Stock(actor);return new(kind,rows.Select(x=>new ReportRow(x.Sku,x.Reserved,x.Available,x.OnHand,x.Reserved,x.Available)).ToList());}
        if(kind is not ("revenue" or "bestsellers")) throw Error("INVALID_INPUT","Loại báo cáo không hợp lệ.");
        var dates=Bounds(from,to); IQueryable<SalesOrder> q=RevenueOrders.Include(x=>x.Lines).Include(x=>x.Payment);
        if(dates.Start.HasValue) q=q.Where(x=>x.CompletedAt>=dates.Start); if(dates.End.HasValue) q=q.Where(x=>x.CompletedAt<dates.End);
        var orders=await q.ToListAsync();
        return kind=="revenue"?new(kind,orders.GroupBy(x=>x.CompletedAt!.Value.AddHours(7).Date).OrderBy(x=>x.Key).Select(x=>new ReportRow(x.Key.ToString("dd/MM/yyyy"),x.Count(),x.Sum(o=>o.Total))).ToList()):new(kind,orders.SelectMany(x=>x.Lines).GroupBy(x=>new{x.VariantId,x.Sku}).Select(g=>new ReportRow(g.Key.Sku,g.Sum(x=>x.Quantity),g.Sum(x=>(decimal)x.Quantity))).OrderByDescending(x=>x.Value).ThenBy(x=>x.Label).ToList());
    }
}



