using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Infrastructure.Commerce;

public sealed class ProductService(BusinessKernel kernel)
{
    public Task<AdminProduct> Read(Actor actor, long id) => kernel.Read<AdminProduct>(async () =>
    {
        await kernel.Require(actor, "ADMIN");
        var p = await kernel.Db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Error("NOT_FOUND", "Không tìm thấy sản phẩm.");
        var variants = await (from v in kernel.Db.Variants.AsNoTracking() join s in kernel.Db.Inventory.AsNoTracking() on v.Id equals s.VariantId
                              where v.ProductId == id orderby v.Id select new { v, s }).ToListAsync();
        var images = await kernel.Db.ProductImages.AsNoTracking().Where(x => x.ProductId == id).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => new ImageInput(x.Url, x.SortOrder)).ToListAsync();
        return new(p.Id, p.Code, p.Name, p.BrandId, p.CategoryId, p.Specifications, p.Description, p.ImageUrl, p.Active,
            variants.Select(x => new AdminVariant(x.v.Id, x.v.Sku, x.v.Color, x.v.StorageGb, x.v.Cost, x.v.ProfitPercent, x.v.Price, x.v.Active, x.s.OnHand, x.s.Reserved, Convert.ToBase64String(x.v.Version))).ToList(), images);
    });
    public Task<long> Save(Actor actor, long id, ProductInput input) => kernel.Write(async () =>
    {
        await kernel.Require(actor, "ADMIN"); if (id < 0) throw Rules.Error("INVALID_INPUT", "Mã sản phẩm không hợp lệ.");
        var db = kernel.Db;
        var p = id == 0 ? new Product() : await db.Products.SingleOrDefaultAsync(x => x.Id == id) ?? throw Rules.Error("NOT_FOUND", "Không tìm thấy sản phẩm.");
        var old = id == 0 ? [] : await db.Variants.Where(x => x.ProductId == id).ToListAsync();
        if (input.Variants is null || input.Variants.Count is 0 or > 100 || input.Variants.Any(x => x is null) || old.Any(x => !input.Variants.Any(v => v.Id == x.Id)) ||
            input.Variants.Where(x => x.Id != 0).Select(x => x.Id).Distinct().Count() != input.Variants.Count(x => x.Id != 0))
            throw Rules.Error("INVALID_INPUT", "Cần 1–100 biến thể, giữ đủ biến thể cũ và không lặp mã biến thể.");
        foreach (var v in old) Rules.Version(v.Version, input.Variants.Single(x => x.Id == v.Id).Version);
        if (!await db.Brands.AnyAsync(x => x.Id == input.BrandId && (x.Active || x.Id == p.BrandId)) ||
            !await db.Categories.AnyAsync(x => x.Id == input.CategoryId && (x.Active || x.Id == p.CategoryId)))
            throw Rules.Error("INVALID_INPUT", "Hãng hoặc danh mục không hợp lệ.");
        p.Code = Rules.Text(input.Code, 30, "Mã sản phẩm").ToUpperInvariant(); p.Name = Rules.Text(input.Name, 200, "Tên sản phẩm");
        p.Specifications = Rules.Text(input.Specifications, 20000, "Thông số", true); p.Description = Rules.Text(input.Description, 50000, "Mô tả", true);
        p.ImageUrl = Rules.Image(input.ImageUrl); p.BrandId = input.BrandId; p.CategoryId = input.CategoryId; p.Active = input.Active;
        if (id == 0) db.Products.Add(p); await db.SaveChangesAsync();
        var added = new List<Variant>();
        foreach (var item in input.Variants)
        {
            var v = item.Id == 0 ? new Variant { ProductId = p.Id } : old.SingleOrDefault(x => x.Id == item.Id) ?? throw Rules.Error("INVALID_INPUT", "Biến thể không thuộc sản phẩm.");
            if (item.StorageGb <= 0) throw Rules.Error("INVALID_INPUT", "Dung lượng phải là số nguyên dương.");
            v.Sku = Rules.Text(item.Sku, 50, "SKU").ToUpperInvariant(); v.Color = Rules.Text(item.Color, 50, "Màu sắc"); v.StorageGb = item.StorageGb;
            if (item.Id == 0) { v.Cost = Rules.Money(item.Cost); v.Price = Rules.Price(v.Cost, item.ProfitPercent); db.Variants.Add(v); added.Add(v); }
            else
            {
                Rules.Price(v.Cost, item.ProfitPercent);
                if (v.ProfitPercent != item.ProfitPercent) v.Price = Rules.Price(v.Cost, item.ProfitPercent);
                // Force a version advance even if only shared product metadata changed.
                db.Entry(v).Property(x => x.Active).IsModified = true;
            }
            v.ProfitPercent = item.ProfitPercent; v.Active = item.Active;
        }
        await db.SaveChangesAsync();
        foreach (var v in added) db.Inventory.Add(new Inventory { VariantId = v.Id });
        if (input.Images is not null)
        {
            if (input.Images.Count > 12 || input.Images.Any(x => x is null || x.SortOrder < 0)) throw Rules.Error("INVALID_INPUT", "Tối đa 12 ảnh, thứ tự không âm.");
            var images = input.Images.Select(x => new ImageInput(Rules.Image(x.Url, false), x.SortOrder)).ToList();
            if (images.Select(x => x.Url).Distinct(StringComparer.Ordinal).Count() != images.Count) throw Rules.Error("INVALID_INPUT", "Ảnh bị lặp.");
            await db.ProductImages.Where(x => x.ProductId == p.Id).ExecuteDeleteAsync();
            db.ProductImages.AddRange(images.Select(x => new ProductImage { ProductId = p.Id, Url = x.Url!, SortOrder = x.SortOrder }));
        }
        return p.Id;
    });
}
