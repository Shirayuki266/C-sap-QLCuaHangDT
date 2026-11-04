using System.Net;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Checks;
internal static class ProductChecks
{
    public static ProductInput Input(AdminProduct p) => new(p.Code, p.Name, p.BrandId, p.CategoryId, p.Specifications, p.Description, p.ImageUrl, p.Active,
        p.Variants.Select(v => new VariantInput(v.Id, v.Sku, v.Color, v.StorageGb, v.Cost, v.ProfitPercent, v.Active, v.Version)).ToList(), p.Images);
    public static async Task Run(Uri address, string connection)
    {
        using var c = new CommerceContext(connection, address, "products"); await c.Initialize();
        await using var db = c.Open(); var kernel = c.Kernel(db); var lookups = new LookupService(kernel); var service = new ProductService(kernel);
        var brand = await lookups.Save(c.Admin, "brands", 0, new("Products brand")); var category = await lookups.Save(c.Admin, "categories", 0, new("Products category"));
        var input = new ProductInput("WRITE-TEST", "Điện thoại mới", brand, category, "Thông số", "Mô tả", "", true, [new(0, "WRITE-SKU", "Đen", 128, 1, 0.5m)]);
        long id = 0;
        await Check.Run("A05 creates computed prices and zero inventory; invalid aggregates roll back", async () =>
        {
            id = await service.Save(c.Admin, 0, input); var p = await service.Read(c.Admin, id);
            Check.True(p.Variants.Single().Price == 1.01m && p.Variants.Single().OnHand == 0, "Creation bypassed price/zero-stock rules.");
            var before = await db.Products.CountAsync();
            await CommerceContext.Reject("INVALID_INPUT", async () => await service.Save(c.Admin, 0, input with { Code = "OTHER-CODE" }));
            await CommerceContext.Reject("INVALID_INPUT", async () => await service.Save(c.Admin, 0, input with { Code = "BAD-COST", Variants = [new(0,"BAD-COST","Đen",128,0,10)] }));
            Check.True(await db.Products.CountAsync() == before, "Failed aggregate left a product behind.");
        });
        await Check.Run("A06 server costs, retained variants and common metadata rowversions protect edits", async () =>
        {
            var p = await service.Read(c.Admin, id); var original = Input(p);
            await service.Save(c.Admin, id, original with { Name = "Tên mới", Variants = original.Variants!.Select(v => v with { Cost = 9999m, ProfitPercent = 100m }).ToList() });
            var updated = await service.Read(c.Admin, id);
            Check.True(updated.Variants[0].Cost == 1m && updated.Variants[0].Price == 2m && updated.Variants[0].OnHand == 0, "Client cost or stock changed through product editing.");
            await CommerceContext.Reject("CONCURRENT_UPDATE", async () => await service.Save(c.Admin, id, original));
            await CommerceContext.Reject("INVALID_INPUT", async () => await service.Save(c.Admin, id, Input(updated) with { Variants = [] }));
        });
        await Check.Run("A07 trading changes preserve records and API denies role/CSRF/stock overposting", async () =>
        {
            var p = await service.Read(c.Admin, id); await service.Save(c.Admin, id, Input(p) with { Active = false });
            p = await service.Read(c.Admin, id); Check.True(!p.Active && p.Variants.Count == 1, "Disabling deleted product history.");
            await service.Save(c.Admin, id, Input(p) with { Active = true });
            using var denied = await c.CustomerClient.Send("/api/admin/products", input);
            using var csrf = await c.AdminClient.Send("/api/admin/products", input, csrf: false);
            using var forged = await c.AdminClient.Send("/api/admin/products", new { code="OVERPOST",name="X",brandId=brand,categoryId=category,active=true,variants=input.Variants,onHand=100 });
            Check.True(denied.StatusCode == HttpStatusCode.Forbidden && csrf.StatusCode == HttpStatusCode.BadRequest && forged.StatusCode == HttpStatusCode.BadRequest, "Product HTTP boundary failed.");
        });
    }
}
