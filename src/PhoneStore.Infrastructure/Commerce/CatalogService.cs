using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Infrastructure.Commerce;
// [UC02.01, UC02.02, UC06.01] Dịch vụ truy vấn danh mục sản phẩm công khai (lọc Active) và tra cứu nội bộ Admin.
public sealed class CatalogService(BusinessKernel kernel)
{
    private const string SearchCollation = "Latin1_General_100_CI_AI_SC";
    // [UC02.01 - E1] Kiểm tra bộ lọc giá/hãng/dung lượng và lọc biến thể theo điều kiện hoạt động.
    private IQueryable<Variant> Variants(CatalogFilter filter, bool all)
    {
        if (filter.MinPrice.HasValue) Rules.Money(filter.MinPrice.Value, true);
        if (filter.MaxPrice.HasValue) Rules.Money(filter.MaxPrice.Value, true);
        if (filter.MinPrice > filter.MaxPrice || filter.StorageGb <= 0 || filter.BrandId <= 0) throw Rules.Error("INVALID_INPUT", "Khoảng giá, hãng hoặc dung lượng không hợp lệ.");
        var color = Rules.Text(filter.Color, 50, "Màu sắc", true);
        return kernel.Db.Variants.AsNoTracking().Where(v => (all || v.Active) && (!filter.MinPrice.HasValue || v.Price >= filter.MinPrice) &&
            (!filter.MaxPrice.HasValue || v.Price <= filter.MaxPrice) && (!filter.StorageGb.HasValue || v.StorageGb == filter.StorageGb) && (color == "" || v.Color == color));
    }
    // [UC02.01] Tạo truy vấn lọc theo hãng, từ khóa (tên, mã, thông số, hãng, SKU) và sắp xếp.
    private IQueryable<Product> Query(CatalogFilter filter, bool all)
    {
        Rules.Offset(filter.Page, filter.PageSize); var variants = Variants(filter, all);
        var query = kernel.Db.Products.AsNoTracking().Where(p => (all || (p.Active && kernel.Db.Brands.Any(b => b.Id == p.BrandId && b.Active) && kernel.Db.Categories.Any(c => c.Id == p.CategoryId && c.Active))) &&
            (!filter.BrandId.HasValue || p.BrandId == filter.BrandId) && (!all || !filter.Active.HasValue || p.Active == filter.Active.Value) && variants.Any(v => v.ProductId == p.Id));
        var term = Rules.Text(filter.Query, 100, "Từ khóa", true).Replace('đ', 'd').Replace('Đ', 'D');
        if (term.Length > 0) query = query.Where(p => EF.Functions.Collate(p.Name, SearchCollation).Replace("đ", "d").Contains(term) ||
            EF.Functions.Collate(p.Code, SearchCollation).Contains(term) || EF.Functions.Collate(p.Specifications, SearchCollation).Replace("đ", "d").Contains(term) ||
            kernel.Db.Brands.Any(b => b.Id == p.BrandId && EF.Functions.Collate(b.Name, SearchCollation).Replace("đ", "d").Contains(term)) ||
            kernel.Db.Variants.Any(v => v.ProductId == p.Id && (all || v.Active) && EF.Functions.Collate(v.Sku, SearchCollation).Contains(term)));
        return filter.Sort switch
        {
            "name" => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            "newest" => query.OrderByDescending(p => p.Id),
            "price-asc" => query.OrderBy(p => variants.Where(v => v.ProductId == p.Id).Min(v => v.Price)).ThenBy(p => p.Id),
            "price-desc" => query.OrderByDescending(p => variants.Where(v => v.ProductId == p.Id).Min(v => v.Price)).ThenBy(p => p.Id),
            _ => throw Rules.Error("INVALID_INPUT", "Cách sắp xếp không hợp lệ.")
        };
    }
    // [UC02.01] Lấy danh sách sản phẩm công khai phân trang (chỉ lấy sản phẩm, hãng, danh mục và biến thể đều Active).
    public Task<Page<PublicProduct>> List(CatalogFilter filter) => kernel.Read<Page<PublicProduct>>(async () =>
    {
        var query = Query(filter, false); var total = await query.CountAsync();
        var ids = await query.Skip(Rules.Offset(filter.Page, filter.PageSize)).Take(filter.PageSize).Select(p => p.Id).ToListAsync();
        var rows = new List<PublicProduct>(); foreach (var id in ids) rows.Add(await Read(id, filter));
        return new(rows, total, filter.Page, filter.PageSize);
    });
    // [UC06.01] Tra cứu danh sách sản phẩm nội bộ cho Admin (kèm giá vốn và sản phẩm ngừng bán).
    public Task<Page<AdminProduct>> Internal(Actor actor, CatalogFilter filter) => kernel.Read<Page<AdminProduct>>(async () =>
    {
        await kernel.Require(actor, "ADMIN"); var query = Query(filter, true); var total = await query.CountAsync();
        var ids = await query.Skip(Rules.Offset(filter.Page, filter.PageSize)).Take(filter.PageSize).Select(p => p.Id).ToListAsync();
        var rows = new List<AdminProduct>(); var service = new ProductService(kernel);
        foreach (var id in ids) rows.Add(await service.Read(actor, id));
        return new(rows, total, filter.Page, filter.PageSize);
    });
    // [UC02.02] Đọc chi tiết sản phẩm công khai, biến thể Active, tồn khả dụng (OnHand - Reserved) và ảnh phụ; ném NOT_FOUND nếu không hợp lệ (E1).
    public Task<PublicProduct> Read(long id, CatalogFilter? filter = null) => kernel.Read<PublicProduct>(async () =>
    {
        filter ??= new(); var variants = Variants(filter, false);
        var row = await (from p in kernel.Db.Products.AsNoTracking() join b in kernel.Db.Brands on p.BrandId equals b.Id
                         join c in kernel.Db.Categories on p.CategoryId equals c.Id
                         where p.Id == id && p.Active && b.Active && c.Active && variants.Any(v => v.ProductId == p.Id)
                         select new { p, Brand = b.Name, Category = c.Name }).SingleOrDefaultAsync()
            ?? throw Rules.Error("NOT_FOUND", "Sản phẩm không tồn tại hoặc không còn kinh doanh.");
        var data = await (from v in variants join s in kernel.Db.Inventory.AsNoTracking() on v.Id equals s.VariantId
                          where v.ProductId == id orderby v.Id select new PublicVariant(v.Id, v.Sku, v.Color, v.StorageGb, v.Price, s.OnHand - s.Reserved)).ToListAsync();
        var images = await kernel.Db.ProductImages.AsNoTracking().Where(x => x.ProductId == id).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => new ImageInput(x.Url, x.SortOrder)).ToListAsync();
        var product = row.p;
        return new(product.Id, product.Code, product.Name, row.Brand, row.Category, product.Specifications, product.Description, product.ImageUrl, data, images);
    });
}
