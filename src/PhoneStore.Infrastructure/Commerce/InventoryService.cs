using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Commerce;
public sealed class InventoryService(BusinessKernel kernel)
{
    public async Task<Page<StockView>> List(Actor actor, string? query = null, int? brandId = null, int? below = null, int page = 1, int pageSize = 20)
    {
        await kernel.Require(actor,"ADMIN"); var offset=Rules.Offset(page,pageSize);var term=Rules.Text(query,100,"Từ khóa",true);
        if(brandId<=0||below<0)throw Rules.Error("INVALID_INPUT","Điều kiện tồn kho không hợp lệ.");
        var rows=from v in kernel.Db.Variants.AsNoTracking() join s in kernel.Db.Inventory.AsNoTracking() on v.Id equals s.VariantId
                 join p in kernel.Db.Products on v.ProductId equals p.Id join b in kernel.Db.Brands on p.BrandId equals b.Id
                 where (!brandId.HasValue||p.BrandId==brandId)&&(!below.HasValue||s.OnHand-s.Reserved<=below)&&
                    (term==""||v.Sku.Contains(term)||p.Code.Contains(term)||p.Name.Contains(term)||v.Color.Contains(term))
                 select new {v,s,p,b};
        var count=await rows.CountAsync();var data=await rows.OrderBy(x=>x.p.Name).ThenBy(x=>x.v.Id).Skip(offset).Take(pageSize).ToListAsync();
        return new(data.Select(x=>new StockView(x.v.Id,x.p.Id,x.p.Name,x.v.Sku,x.b.Name,x.v.Color,x.v.StorageGb,x.s.OnHand,x.s.Reserved,x.s.OnHand-x.s.Reserved,Convert.ToBase64String(x.s.Version))).ToList(),count,page,pageSize);
    }
    public async Task<Page<MovementView>> Movements(Actor actor,long? variantId=null,Guid? operationKey=null,int page=1,int pageSize=20)
    {
        await kernel.Require(actor,"ADMIN");var offset=Rules.Offset(page,pageSize);
        var rows=from m in kernel.Db.Movements.AsNoTracking() join v in kernel.Db.Variants on m.VariantId equals v.Id
                 join a in kernel.Db.Accounts on m.ActorId equals a.Id
                 where(!variantId.HasValue||m.VariantId==variantId)&&(!operationKey.HasValue||m.OperationKey==operationKey)
                 select new {m,v.Sku,a.Username};
        var count=await rows.CountAsync();var data=await rows.OrderByDescending(x=>x.m.Id).Skip(offset).Take(pageSize).ToListAsync();
        return new(data.Select(x=>new MovementView(x.m.Id,x.m.VariantId,x.Sku,x.m.Kind,x.m.DeltaOnHand,x.m.DeltaReserved,new(DateTime.SpecifyKind(x.m.At,DateTimeKind.Utc)),x.m.ActorId,x.Username,x.m.OrderId,x.m.PurchaseId,x.m.Reason,x.m.OperationKey)).ToList(),count,page,pageSize);
    }
    public Task<bool> Adjust(Actor actor,long variantId,StockCountInput input)=>kernel.Write(async()=>
    {
        await kernel.Require(actor,"ADMIN");Rules.Key(input.OperationKey);var reason=Rules.Text(input.Reason,1000,"Lý do kiểm kê");
        var row=await kernel.Db.Inventory.AsNoTracking().SingleOrDefaultAsync(x=>x.VariantId==variantId)??throw Rules.Error("NOT_FOUND","Không tìm thấy biến thể tồn kho.");
        Rules.Version(row.Version,input.Version);
        if(input.Counted<0||input.Counted<row.Reserved||input.Counted==row.OnHand)throw Rules.Error("INVALID_INPUT","Số kiểm kê phải khác số hiện tại và không thấp hơn số đang giữ.");
        if(await kernel.Db.Movements.AnyAsync(x=>x.OperationKey==input.OperationKey))throw Rules.Error("INVALID_STATE","Thao tác đã được thực hiện. Hãy tra cứu nhật ký trước khi gửi lại.");
        if(await kernel.Db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.TON_KHO SET OnHand={input.Counted} WHERE VariantId={variantId} AND Version={row.Version} AND Reserved<={input.Counted}")!=1)
            throw Rules.Error("CONCURRENT_UPDATE","Tồn kho đã thay đổi. Vui lòng tải lại.");
        kernel.Db.Movements.Add(new StockMovement{VariantId=variantId,ActorId=actor.AccountId,Kind="DIEU_CHINH",DeltaOnHand=input.Counted-row.OnHand,Reason=reason,OperationKey=input.OperationKey,At=kernel.Now});return true;
    });
}
