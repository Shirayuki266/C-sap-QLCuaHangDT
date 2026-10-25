using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Contracts;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Services;
public sealed partial class StoreService
{
    public async Task<Page<CustomerDto>> Customers(long actor,string? query=null,int page=1) {
        await Require(actor,"ADMIN"); var q=db.Customers.Include(x=>x.Account).AsNoTracking();
        if(!string.IsNullOrWhiteSpace(query)) q=q.Where(x=>x.FullName.Contains(query)||x.Phone.Contains(query)||(x.Email!=null&&x.Email.Contains(query))||x.Account.Username.Contains(query));
        var total=await q.CountAsync(); page=Math.Clamp(page,1,100000);
        return new(await q.OrderBy(x=>x.Id).Skip((page-1)*20).Take(20).Select(x=>new CustomerDto(x.Id,x.Account.Username,x.FullName,x.Phone,x.Email,x.Address,x.Account.Active)).ToListAsync(),total,page,20);
    }
    public Task<bool> SaveCustomer(long actor,long id,CustomerInput input)=>Atomic(async()=> {
        await Require(actor,"ADMIN"); var c=await db.Customers.Include(x=>x.Account).SingleOrDefaultAsync(x=>x.Id==id)??throw Error("NOT_FOUND","Không tìm thấy khách hàng.");
        SetProfile(c,new(input.FullName,input.Phone,input.Email,input.Address)); c.Account.Active=input.Active;
        if(!input.Active) db.Sessions.RemoveRange(await db.Sessions.Where(x=>x.AccountId==id).ToListAsync()); return true;
    });
    public async Task<List<SupplierDto>> Suppliers(long actor,string? query=null) {
        await Require(actor,"ADMIN"); var q=db.Suppliers.AsNoTracking(); if(!string.IsNullOrWhiteSpace(query)) q=q.Where(x=>x.Code.Contains(query)||x.Name.Contains(query));
        return await q.OrderBy(x=>x.Name).Select(x=>new SupplierDto(x.Id,x.Code,x.Name,x.Phone,x.Address,x.Active)).ToListAsync();
    }
    public Task<long> SaveSupplier(long actor,long id,SupplierInput input)=>Atomic(async()=> {
        await Require(actor,"ADMIN"); var s=id==0?new Supplier():await db.Suppliers.FindAsync(id)??throw Error("NOT_FOUND","Không tìm thấy nhà cung cấp.");
        s.Code=Text(input.Code,30,"Mã NCC").ToUpperInvariant(); s.Name=Text(input.Name,200,"Tên NCC"); s.Phone=string.IsNullOrWhiteSpace(input.Phone)?null:Phone(input.Phone); s.Address=Text(input.Address,500,"Địa chỉ",true); s.Active=input.Active;
        if(id==0) db.Suppliers.Add(s); await db.SaveChangesAsync(); return s.Id;
    });
    public async Task<List<StockDto>> Stock(long actor,string? query=null,int? brandId=null,bool low=false) {
        await Require(actor,"ADMIN"); var q=db.Stock.AsNoTracking().Include(x=>x.Variant).ThenInclude(x=>x.Product).ThenInclude(x=>x.Brand).AsQueryable();
        if(!string.IsNullOrWhiteSpace(query)) q=q.Where(x=>x.Variant.Sku.Contains(query)||x.Variant.Product.Name.Contains(query)||x.Variant.Color.Contains(query));
        if(brandId.HasValue) q=q.Where(x=>x.Variant.Product.BrandId==brandId);
        if(low) q=q.Where(x=>x.OnHand-x.Reserved<=3);
        return (await q.OrderBy(x=>x.Variant.Sku).ToListAsync()).Select(x=>new StockDto(x.VariantId,x.Variant.Sku,x.Variant.Product.Name,x.Variant.Product.Brand.Name,x.Variant.Color,x.Variant.StorageGb,x.OnHand,x.Reserved,x.OnHand-x.Reserved,Convert.ToBase64String(x.Version))).ToList();
    }
    public Task<bool> AdjustStock(long actor,long variantId,AdjustmentInput input)=>Atomic(async()=> {
        await Require(actor,"ADMIN"); Key(input.OperationKey); var reason=Text(input.Reason,500,"Lý do kiểm kê");
        var stock=await db.Stock.AsNoTracking().SingleOrDefaultAsync(x=>x.VariantId==variantId)??throw Error("NOT_FOUND","Không tìm thấy biến thể."); Version(stock.Version,input.Version);
        if(input.Counted<stock.Reserved||input.Counted>10000000) throw Error("INVALID_INPUT","Tồn kiểm kê không được âm, thấp hơn lượng giữ hoặc vượt 10 triệu.");
        var diff=input.Counted-stock.OnHand; if(diff==0) throw Error("INVALID_INPUT","Số kiểm kê không thay đổi.");
        if(await db.Movements.AnyAsync(x=>x.OperationKey==input.OperationKey)) throw Error("INVALID_STATE","Yêu cầu này đã được thực hiện.");
        var count=input.Counted;
        if(await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE TON_KHO SET OnHand={count} WHERE VariantId={variantId} AND Reserved<={count}")!=1) throw Error("CONCURRENT_UPDATE","Tồn kho đã thay đổi.");
        db.Movements.Add(new Movement{VariantId=variantId,ActorId=actor,Kind="DIEU_CHINH",DeltaOnHand=diff,OperationKey=input.OperationKey,Reason=reason}); return true;
    });
    public async Task<Page<MovementDto>> Movements(long actor,long? variantId=null,int page=1) {
        await Require(actor,"ADMIN"); var q=db.Movements.Include(x=>x.Variant).Include(x=>x.Actor).Include(x=>x.Order).Include(x=>x.Purchase).AsNoTracking();
        if(variantId.HasValue) q=q.Where(x=>x.VariantId==variantId); var count=await q.CountAsync(); page=Math.Clamp(page,1,100000);
        return new(await q.OrderByDescending(x=>x.Id).Skip((page-1)*50).Take(50).Select(x=>new MovementDto(x.Id,x.VariantId,x.Variant.Sku,x.Kind,x.DeltaOnHand,x.DeltaReserved,x.At,x.Actor.Username,x.Order!=null?x.Order.Code:x.Purchase!=null?x.Purchase.Code:null,x.Reason)).ToListAsync(),count,page,50);
    }
    private IQueryable<Purchase> PurchaseQuery=>db.Purchases.Include(x=>x.Supplier).Include(x=>x.Creator).Include(x=>x.Confirmer).Include(x=>x.Lines).ThenInclude(x=>x.Variant).ThenInclude(x=>x.Product);
    private static PurchaseDto MapPurchase(Purchase p)=>new(p.Id,p.Code,p.SupplierId,p.Supplier.Name,p.Status,p.Total,p.CreatedAt,p.ConfirmedAt,p.Creator.Username,p.Confirmer?.Username,Convert.ToBase64String(p.Version),p.Lines.OrderBy(x=>x.Id).Select(x=>new PurchaseLineDto(x.VariantId,x.Variant.Sku,x.Variant.Product.Name,x.Quantity,x.Cost)).ToList());
    public async Task<PurchaseDto> GetPurchase(long actor,long id) { await Require(actor,"ADMIN"); return MapPurchase(await PurchaseQuery.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id)??throw Error("NOT_FOUND","Không tìm thấy phiếu nhập.")); }
    public async Task<Page<PurchaseDto>> Purchases(long actor,string? query=null,string? status=null,int page=1) {
        await Require(actor,"ADMIN"); var q=PurchaseQuery.AsNoTracking(); if(!string.IsNullOrWhiteSpace(query)) q=q.Where(x=>x.Code.Contains(query)||x.Supplier.Name.Contains(query)); if(!string.IsNullOrWhiteSpace(status)) q=q.Where(x=>x.Status==status);
        var count=await q.CountAsync(); page=Math.Clamp(page,1,100000); return new((await q.OrderByDescending(x=>x.Id).Skip((page-1)*20).Take(20).ToListAsync()).Select(MapPurchase).ToList(),count,page,20);
    }
    public Task<long> SavePurchase(long actor,long id,PurchaseInput input)=>Atomic(async()=> {
        await Require(actor,"ADMIN"); var p=id==0?new Purchase{CreatorId=actor,Code="PN"+Guid.NewGuid().ToString("N")[..24]}:await db.Purchases.Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Id==id)??throw Error("NOT_FOUND","Không tìm thấy phiếu nhập.");
        if(id!=0) Version(p.Version,input.Version); if(p.Status!="NHAP") throw Error("INVALID_STATE","Chỉ được sửa phiếu nháp.");
        if(!await db.Suppliers.AnyAsync(x=>x.Id==input.SupplierId&&x.Active)) throw Error("INVALID_INPUT","Nhà cung cấp không hoạt động.");
        if(input.Lines is null||input.Lines.Any(x=>x is null)||input.Lines.Count>100||input.Lines.Select(x=>x.VariantId).Distinct().Count()!=input.Lines.Count) throw Error("INVALID_INPUT","Dòng nhập trùng hoặc quá nhiều dòng.");
        foreach(var line in input.Lines) { Quantity(line.Quantity); Money(line.Cost); if(!await db.Variants.AnyAsync(x=>x.Id==line.VariantId)) throw Error("INVALID_INPUT","Biến thể không tồn tại."); }
        if(id!=0) {db.PurchaseLines.RemoveRange(p.Lines);p.Lines.Clear();await db.SaveChangesAsync();}
        p.SupplierId=input.SupplierId; p.Total=input.Lines.Sum(x=>x.Quantity*x.Cost); if(p.Total>9999999999999999m) throw Error("INVALID_INPUT","Tổng tiền vượt phạm vi hỗ trợ."); p.Lines=input.Lines.Select(x=>new PurchaseLine{VariantId=x.VariantId,Quantity=x.Quantity,Cost=x.Cost}).ToList();
        if(id==0) db.Purchases.Add(p); await db.SaveChangesAsync(); return p.Id;
    });
    public Task<bool> ConfirmPurchase(long actor,long id,TransitionRequest input,bool cancel=false)=>Atomic(async()=> {
        await Require(actor,"ADMIN"); Key(input.OperationKey); var p=await db.Purchases.Include(x=>x.Lines).Include(x=>x.Supplier).SingleOrDefaultAsync(x=>x.Id==id)??throw Error("NOT_FOUND","Không tìm thấy phiếu nhập.");
        Version(p.Version,input.Version); if(p.Status!="NHAP") throw Error("INVALID_STATE","Phiếu đã được xác nhận hoặc hủy.");
        if(cancel) {p.Status="DA_HUY";return true;}
        if(!p.Supplier.Active||p.Lines.Count==0||p.Total<=0) throw Error("INVALID_INPUT","Phiếu rỗng hoặc nhà cung cấp không hoạt động.");
        foreach(var line in p.Lines.OrderBy(x=>x.VariantId)) { var q=line.Quantity; var v=line.VariantId; Quantity(q); Money(line.Cost);
            if(await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE TON_KHO SET OnHand=OnHand+{q} WHERE VariantId={v}")!=1) throw Error("INVALID_STATE","Không tìm thấy dòng tồn kho.");
            db.Movements.Add(new Movement{VariantId=v,ActorId=actor,PurchaseId=id,Kind="NHAP",DeltaOnHand=q,OperationKey=input.OperationKey});
        }
        p.Total=p.Lines.Sum(x=>x.Quantity*x.Cost); p.Status="DA_XAC_NHAN"; p.ConfirmerId=actor; p.ConfirmedAt=DateTime.UtcNow; return true;
    });
}

