using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Commerce;
public sealed class CheckoutService(BusinessKernel kernel)
{
    public async Task<OrderView> Resolve(Actor actor,Guid requestKey)
    {
        await kernel.Require(actor,"CUSTOMER");Rules.Key(requestKey);
        var id=await kernel.Db.Orders.AsNoTracking().Where(x=>x.RequestKey==requestKey&&x.CustomerId==actor.AccountId).Select(x=>(long?)x.Id).SingleOrDefaultAsync()
            ??throw Rules.Error("NOT_FOUND","Không tìm thấy yêu cầu đặt hàng của bạn.");
        return await OrderReadModel.Load(kernel.Db,id);
    }
    public async Task<OrderView> Place(Actor actor,CheckoutInput input)
    {
        var id=await kernel.Write(async()=>
        {
            await kernel.Require(actor,"CUSTOMER");Rules.Key(input.RequestKey);
            if(input.PaymentMethod!="COD")throw Rules.Error("INVALID_INPUT","Chỉ hỗ trợ thanh toán COD.");
            var recipient=Rules.Text(input.Recipient,120,"Người nhận");var phone=Rules.Phone(input.Phone);var address=Rules.Text(input.Address,500,"Địa chỉ giao hàng");
            if(input.Lines is null||input.Lines.Count is 0 or >100||input.Lines.Any(x=>x is null||x.VariantId<=0)||input.Lines.Select(x=>x.VariantId).Distinct().Count()!=input.Lines.Count)
                throw Rules.Error("INVALID_INPUT","Dòng xác nhận giá không hợp lệ.");
            foreach(var line in input.Lines){Rules.Quantity(line.Quantity);Rules.Money(line.Price);}
            var db=kernel.Db;var previous=await db.Orders.AsNoTracking().SingleOrDefaultAsync(x=>x.RequestKey==input.RequestKey);
            if(previous is not null)
            {
                if(previous.CustomerId!=actor.AccountId)throw Rules.Error("FORBIDDEN","Khóa yêu cầu không thuộc tài khoản của bạn.");
                var old=await db.OrderLines.AsNoTracking().Where(x=>x.OrderId==previous.Id).OrderBy(x=>x.VariantId).Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price)).ToListAsync();
                if(previous.Recipient!=recipient||previous.Phone!=phone||previous.Address!=address||!old.SequenceEqual(input.Lines.OrderBy(x=>x.VariantId)))
                    throw Rules.Error("INVALID_STATE","Khóa đặt hàng đã dùng cho nội dung khác. Vui lòng tra cứu đơn trước khi thử lại.");
                return previous.Id;
            }
            var cart=await db.Cart.Where(x=>x.CustomerId==actor.AccountId).OrderBy(x=>x.VariantId).ToListAsync();
            if(cart.Count==0)throw Rules.Error("INVALID_INPUT","Giỏ hàng đang rỗng.");
            var current=await new CartService(kernel).Read(actor);
            var confirmed=current.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price??0)).OrderBy(x=>x.VariantId);
            if(!confirmed.SequenceEqual(input.Lines.OrderBy(x=>x.VariantId)))throw new PriceChangedException(current);
            if(current.Lines.Any(x=>!x.Sellable))throw Rules.Error("INVALID_INPUT","Giỏ có sản phẩm đã ngừng kinh doanh hoặc không tồn tại.");
            if(current.Lines.Any(x=>x.Available<x.Quantity))throw Rules.Error("OUT_OF_STOCK","Không đủ hàng để đặt đơn. Vui lòng kiểm tra lại giỏ.");
            Rules.Money(current.Total);
            if(await db.Movements.AnyAsync(x=>x.OperationKey==input.RequestKey))throw Rules.Error("INVALID_STATE","Khóa yêu cầu đã được dùng. Vui lòng tạo yêu cầu mới sau khi kiểm tra kết quả cũ.");
            var order=new Order{Code="DH"+input.RequestKey.ToString("N"),CustomerId=actor.AccountId,Recipient=recipient,Phone=phone,Address=address,Total=current.Total,CreatedAt=kernel.Now,RequestKey=input.RequestKey};
            db.Orders.Add(order);await db.SaveChangesAsync();
            db.Payments.Add(new Payment{OrderId=order.Id});
            foreach(var line in current.Lines)
            {
                if(await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.TON_KHO SET Reserved=Reserved+{line.Quantity} WHERE VariantId={line.VariantId} AND OnHand-Reserved>={line.Quantity}")!=1)
                    throw Rules.Error("OUT_OF_STOCK","Tồn kho vừa thay đổi. Không có đơn dở dang được tạo.");
                db.OrderLines.Add(new OrderLine{OrderId=order.Id,VariantId=line.VariantId,Name=line.Name!,Sku=line.Sku!,Description=line.Color+" / "+line.StorageGb+"GB",Quantity=line.Quantity,Price=line.Price!.Value});
                db.Movements.Add(new StockMovement{VariantId=line.VariantId,ActorId=actor.AccountId,OrderId=order.Id,Kind="GIU",DeltaReserved=line.Quantity,At=kernel.Now,OperationKey=input.RequestKey});
            }
            db.OrderHistory.Add(new OrderHistory{OrderId=order.Id,ActorId=actor.AccountId,To="CHO_XAC_NHAN",At=kernel.Now});
            db.Cart.RemoveRange(cart);return order.Id;
        });
        return await OrderReadModel.Load(kernel.Db,id);
    }
}
