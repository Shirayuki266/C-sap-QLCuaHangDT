using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Contracts;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Services;
public sealed partial class StoreService
{
    private IQueryable<Product> ProductQuery => db.Products.AsSplitQuery().Include(x=>x.Images).Include(x=>x.Brand).Include(x=>x.Category).Include(x=>x.Variants).ThenInclude(x=>x.Inventory);
    private static ProductDto ProductMap(Product p)=>new(p.Id,p.Code,p.Name,p.BrandId,p.CategoryId,p.Brand.Name,p.Category.Name,p.Specifications,p.Description,p.ImageUrl,p.Active,p.Variants.OrderBy(x=>x.Id).Select(x=>new VariantDto(x.Id,x.Sku,x.Color,x.StorageGb,x.Price,x.Active,x.Inventory.OnHand,x.Inventory.Reserved,x.Inventory.OnHand-x.Inventory.Reserved,Convert.ToBase64String(x.Version))).ToList(),p.Images.OrderBy(x=>x.SortOrder).ThenBy(x=>x.Id).Select(x=>new ProductImageInput(x.Url,x.SortOrder)).ToList());
    public async Task<Page<ProductDto>> Catalog(CatalogFilter f,bool internalView=false,long actor=0) {
        if(internalView) await Require(actor,"ADMIN");
        if((f.Query?.Length??0)>100||(f.Color?.Length??0)>50)throw Error("INVALID_INPUT","Từ khóa tối đa 100 ký tự, màu sắc tối đa 50 ký tự.");
        if(f.MinPrice<0||f.MaxPrice<0||(f.MinPrice.HasValue&&f.MaxPrice.HasValue&&f.MinPrice>f.MaxPrice)) throw Error("INVALID_INPUT","Khoảng giá không hợp lệ.");
        var q=ProductQuery.AsNoTracking(); if(!internalView) q=q.Where(p=>p.Active&&p.Brand.Active&&p.Category.Active&&p.Variants.Any(v=>v.Active));
        if(!string.IsNullOrWhiteSpace(f.Query)) { var term=f.Query.Trim().Replace('đ','d').Replace('Đ','D'); q=q.Where(p=>EF.Functions.Collate(p.Name,SearchCollation).Replace("đ","d").Contains(term)||p.Code.Contains(term)||EF.Functions.Collate(p.Brand.Name,SearchCollation).Replace("đ","d").Contains(term)||EF.Functions.Collate(p.Specifications,SearchCollation).Replace("đ","d").Contains(term)||p.Variants.Any(v=>v.Sku.Contains(term))); }
        if(f.BrandId.HasValue) q=q.Where(p=>p.BrandId==f.BrandId);
        q=q.Where(p=>p.Variants.Any(v=>(internalView||v.Active)&&(!f.MinPrice.HasValue||v.Price>=f.MinPrice)&&(!f.MaxPrice.HasValue||v.Price<=f.MaxPrice)&&(!f.StorageGb.HasValue||v.StorageGb==f.StorageGb)&&(string.IsNullOrEmpty(f.Color)||v.Color==f.Color)));
        var total=await q.CountAsync();
        q=f.Sort switch { "price-asc"=>q.OrderBy(p=>p.Variants.Where(v=>(internalView||v.Active)&&(!f.MinPrice.HasValue||v.Price>=f.MinPrice)&&(!f.MaxPrice.HasValue||v.Price<=f.MaxPrice)&&(!f.StorageGb.HasValue||v.StorageGb==f.StorageGb)&&(string.IsNullOrEmpty(f.Color)||v.Color==f.Color)).Min(v=>v.Price)).ThenBy(p=>p.Id), "price-desc"=>q.OrderByDescending(p=>p.Variants.Where(v=>(internalView||v.Active)&&(!f.MinPrice.HasValue||v.Price>=f.MinPrice)&&(!f.MaxPrice.HasValue||v.Price<=f.MaxPrice)&&(!f.StorageGb.HasValue||v.StorageGb==f.StorageGb)&&(string.IsNullOrEmpty(f.Color)||v.Color==f.Color)).Min(v=>v.Price)).ThenBy(p=>p.Id), _=>q.OrderBy(p=>p.Name).ThenBy(p=>p.Id) };
        var page=Math.Clamp(f.Page,1,100000); var size=Math.Clamp(f.PageSize,1,100);
        var items=(await q.Skip((page-1)*size).Take(size).ToListAsync()).Select(ProductMap).ToList();
        if(!internalView) items=items.Select(p=>p with{Variants=p.Variants.Where(v=>v.Active&&(!f.MinPrice.HasValue||v.Price>=f.MinPrice)&&(!f.MaxPrice.HasValue||v.Price<=f.MaxPrice)&&(!f.StorageGb.HasValue||v.StorageGb==f.StorageGb)&&(string.IsNullOrEmpty(f.Color)||v.Color==f.Color)).ToList()}).ToList();
        return new(items,total,page,size);
    }
    public async Task<ProductDto> ProductDetail(long id,bool internalView=false,long actor=0) {
        if(internalView) await Require(actor,"ADMIN"); var p=await ProductQuery.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);
        if(p is null||(!internalView&&(!p.Active||!p.Brand.Active||!p.Category.Active))) throw Error("NOT_FOUND","Sản phẩm không còn khả dụng.");
        var dto=ProductMap(p); return internalView?dto:dto with{Variants=dto.Variants.Where(v=>v.Active).ToList()};
    }
    public async Task<List<LookupDto>> Lookups(string kind,bool all=false,long actor=0) {
        if(all) await Require(actor,"ADMIN"); return kind switch { "brands"=>await db.Brands.AsNoTracking().Where(x=>all||x.Active).OrderBy(x=>x.Name).Select(x=>new LookupDto(x.Id,x.Name,x.Active)).ToListAsync(), "categories"=>await db.Categories.AsNoTracking().Where(x=>all||x.Active).OrderBy(x=>x.Name).Select(x=>new LookupDto(x.Id,x.Name,x.Active)).ToListAsync(), _=>throw Error("NOT_FOUND","Không tìm thấy danh mục.") };
    }
    public Task<int> SaveLookup(long actor,string kind,int id,LookupRequest input)=>Atomic(async()=> {
        await Require(actor,"ADMIN"); var name=Text(input.Name,100,"Tên danh mục");
        if(kind=="brands") { var row=id==0?new Brand():await db.Brands.FindAsync(id)??throw Error("NOT_FOUND","Không tìm thấy hãng."); row.Name=name; row.Active=input.Active; if(id==0) db.Brands.Add(row); await db.SaveChangesAsync(); return row.Id; }
        if(kind=="categories") { var row=id==0?new Category():await db.Categories.FindAsync(id)??throw Error("NOT_FOUND","Không tìm thấy danh mục."); row.Name=name; row.Active=input.Active; if(id==0) db.Categories.Add(row); await db.SaveChangesAsync(); return row.Id; }
        throw Error("NOT_FOUND","Không tìm thấy danh mục.");
    });
    public Task<long> SaveProduct(long actor,long id,ProductInput input)=>Atomic(async()=> {
        await Require(actor,"ADMIN");
        var p=id==0?new Product():await db.Products.Include(x=>x.Images).Include(x=>x.Variants).SingleOrDefaultAsync(x=>x.Id==id)??throw Error("NOT_FOUND","Không tìm thấy sản phẩm.");
        p.Code=Text(input.Code,30,"Mã sản phẩm").ToUpperInvariant(); p.Name=Text(input.Name,200,"Tên sản phẩm"); p.Specifications=Text(input.Specifications,20000,"Cấu hình",true); p.Description=Text(input.Description,50000,"Mô tả",true); p.Active=input.Active;
        var image=Text(input.ImageUrl,1000,"URL ảnh",true); if(image!=""&&!(Uri.TryCreate(image,UriKind.Absolute,out var u)&&u.Scheme=="https")&&!image.StartsWith("/images/",StringComparison.Ordinal)) throw Error("INVALID_INPUT","Ảnh cần URL HTTPS hoặc đường dẫn /images/."); p.ImageUrl=image;
        if(!await db.Brands.AnyAsync(x=>x.Id==input.BrandId&&(x.Active||x.Id==p.BrandId))||!await db.Categories.AnyAsync(x=>x.Id==input.CategoryId&&(x.Active||x.Id==p.CategoryId))) throw Error("INVALID_INPUT","Hãng hoặc danh mục không hợp lệ.");
        p.BrandId=input.BrandId; p.CategoryId=input.CategoryId;
        if(input.Images!=null){if(input.Images.Count>12||input.Images.Any(x=>x is null||x.SortOrder<0||string.IsNullOrWhiteSpace(x.Url)||x.Url.Length>1000||!Uri.TryCreate(x.Url,UriKind.Absolute,out var parsed)||parsed.Scheme!="https")||input.Images.Select(x=>x.Url).Distinct().Count()!=input.Images.Count)throw Error("INVALID_INPUT","Ảnh bổ sung cần URL HTTPS khác nhau, tối đa 12 ảnh, thứ tự không âm.");db.RemoveRange(p.Images);p.Images=input.Images.Select(x=>new ProductImage{Url=x.Url,SortOrder=x.SortOrder}).ToList();}
        if(input.Variants is null||input.Variants.Any(x=>x is null)||input.Variants.Count==0||input.Variants.Count>100||p.Variants.Any(v=>!input.Variants.Any(i=>i.Id==v.Id))) throw Error("INVALID_INPUT","Cần ít nhất một biến thể; dùng ngừng bán thay xóa biến thể cũ.");
        if(input.Variants.Where(x=>x.Id!=0).Select(x=>x.Id).Distinct().Count()!=input.Variants.Count(x=>x.Id!=0)) throw Error("INVALID_INPUT","Biến thể bị lặp.");
        foreach(var i in input.Variants) {
            var v=i.Id==0?new Variant{Inventory=new Inventory()}:p.Variants.SingleOrDefault(x=>x.Id==i.Id)??throw Error("INVALID_INPUT","Biến thể không thuộc sản phẩm.");
            if(i.Id!=0) Version(v.Version,i.Version); Money(i.Price); if(i.StorageGb<=0||i.StorageGb>10000) throw Error("INVALID_INPUT","Dung lượng không hợp lệ.");
            v.Sku=Text(i.Sku,50,"SKU").ToUpperInvariant(); v.Color=Text(i.Color,50,"Màu sắc"); v.StorageGb=i.StorageGb; v.Price=i.Price; v.Active=i.Active; if(i.Id!=0) db.Entry(v).Property(x=>x.Active).IsModified=true; if(i.Id==0) p.Variants.Add(v);
        }
        if(id==0) db.Products.Add(p); await db.SaveChangesAsync(); return p.Id;
    });
    public async Task<CartDto> GetCart(long customer) { await Require(customer,"CUSTOMER"); return new(await CartQuery(customer).Select(x=>new CartLineDto(x.VariantId,x.Variant.Product.Name,x.Variant.Sku,x.Variant.Color,x.Variant.StorageGb,x.Variant.Price,x.Quantity,x.Variant.Inventory.OnHand-x.Variant.Inventory.Reserved,x.Variant.Active&&x.Variant.Product.Active&&x.Variant.Product.Brand.Active&&x.Variant.Product.Category.Active)).ToListAsync()); }
    private IQueryable<CartLine> CartQuery(long customer)=>db.Cart.Where(x=>x.CustomerId==customer).OrderBy(x=>x.VariantId);
    private async Task<Variant> Sellable(long id) {
        var v=await db.Variants.Include(x=>x.Inventory).Include(x=>x.Product).ThenInclude(x=>x.Brand).Include(x=>x.Product).ThenInclude(x=>x.Category).SingleOrDefaultAsync(x=>x.Id==id);
        if(v is null||!v.Active||!v.Product.Active||!v.Product.Brand.Active||!v.Product.Category.Active) throw Error("INVALID_INPUT","Sản phẩm đã ngừng kinh doanh hoặc không tồn tại."); return v;
    }
    public Task<bool> SetCart(long customer,CartInput input,bool add=false)=>Atomic(async()=> {
        await Require(customer,"CUSTOMER"); if(input.VariantId<=0) throw Error("INVALID_INPUT","Mã biến thể không hợp lệ."); var line=await db.Cart.SingleOrDefaultAsync(x=>x.CustomerId==customer&&x.VariantId==input.VariantId);
        if(input.Quantity==0&&!add) { if(line!=null) db.Cart.Remove(line); return true; }
        Quantity(input.Quantity); var quantity=add?checked((line?.Quantity??0)+input.Quantity):input.Quantity; Quantity(quantity);
        var v=await Sellable(input.VariantId); if(v.Inventory.OnHand-v.Inventory.Reserved<quantity) throw Error("OUT_OF_STOCK","Số lượng vượt hàng có thể bán.");
        if(line==null&&await db.Cart.CountAsync(x=>x.CustomerId==customer)>=100) throw Error("INVALID_INPUT","Giỏ hàng có tối đa 100 biến thể.");
        if(line==null) db.Cart.Add(new CartLine{CustomerId=customer,VariantId=input.VariantId,Quantity=quantity}); else {line.Quantity=quantity;line.UpdatedAt=DateTime.UtcNow;} return true;
    });
    public Task<bool> MergeCart(long customer,List<CartInput> inputs)=>Atomic(async()=> {
        await Require(customer,"CUSTOMER"); if(inputs is null||inputs.Any(x=>x is null)||inputs.Count>100) throw Error("INVALID_INPUT","Giỏ hàng không hợp lệ.");
        var existingIds=await db.Cart.Where(x=>x.CustomerId==customer).Select(x=>x.VariantId).ToListAsync();if(existingIds.Union(inputs.Select(x=>x.VariantId)).Count()>100) throw Error("INVALID_INPUT","Giỏ hàng có tối đa 100 biến thể.");
        foreach(var group in inputs.GroupBy(x=>x.VariantId).OrderBy(x=>x.Key)) {
            foreach(var i in group) Quantity(i.Quantity); var quantity=group.Sum(x=>x.Quantity);
            var line=await db.Cart.SingleOrDefaultAsync(x=>x.CustomerId==customer&&x.VariantId==group.Key); quantity+=line?.Quantity??0; Quantity(quantity);
            var v=await Sellable(group.Key); if(v.Inventory.OnHand-v.Inventory.Reserved<quantity) throw Error("OUT_OF_STOCK",$"{v.Product.Name}: không đủ hàng để hợp nhất giỏ. Giỏ tạm vẫn được giữ.");
            if(line==null) db.Cart.Add(new CartLine{CustomerId=customer,VariantId=group.Key,Quantity=quantity}); else {line.Quantity=quantity;line.UpdatedAt=DateTime.UtcNow;}
        } return true;
    });

    public async Task<CartDto> PreviewCart(List<CartInput> inputs) {
        if(inputs is null||inputs.Any(x=>x is null)||inputs.Count>100) throw Error("INVALID_INPUT","Giỏ hàng không hợp lệ.");
        foreach(var input in inputs){if(input.VariantId<=0)throw Error("INVALID_INPUT","Mã biến thể không hợp lệ.");Quantity(input.Quantity);}
        var ids=inputs.Select(x=>x.VariantId).Distinct().ToArray(); var variants=await db.Variants.AsNoTracking().Include(x=>x.Inventory).Include(x=>x.Product).ThenInclude(x=>x.Brand).Include(x=>x.Product).ThenInclude(x=>x.Category).Where(x=>ids.Contains(x.Id)).ToListAsync();
        return new(inputs.Select(i=>{var v=variants.SingleOrDefault(x=>x.Id==i.VariantId);return v is null?new CartLineDto(i.VariantId,"Sản phẩm không còn tồn tại","","",0,0,i.Quantity,0,false):new CartLineDto(v.Id,v.Product.Name,v.Sku,v.Color,v.StorageGb,v.Price,i.Quantity,v.Inventory.OnHand-v.Inventory.Reserved,v.Active&&v.Product.Active&&v.Product.Brand.Active&&v.Product.Category.Active);}).ToList());
    }
}






