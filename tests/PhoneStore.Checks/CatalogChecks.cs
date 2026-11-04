using System.Net;
using System.Text.Json;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class CatalogChecks
{
    public static async Task Run(Uri address, string connection)
    {
        using var c = new CommerceContext(connection, address, "catalog"); await c.Initialize();
        await using var db = c.Open(); var k = c.Kernel(db); var lookups = new LookupService(k); var products = new ProductService(k); var catalog = new CatalogService(k);
        var brand = await lookups.Save(c.Admin, "brands", 0, new("Hãng kiểm thử")); var category = await lookups.Save(c.Admin, "categories", 0, new("Danh mục kiểm thử"));
        var id = await products.Save(c.Admin, 0, new("READ-TEST", "Điện thoại đọc thử", brand, category, "Cấu hình", "Mô tả", "", true,
            [new(0,"READ-64","Đen",64,100,0), new(0,"READ-256","Xanh",256,200,0)]));
        await Check.Run("K06-K08 SQL search, same-variant filtering and stable pages are accurate", async () =>
        {
            var found = await catalog.List(new(Query:"dien thoai doc thu", BrandId:brand, PageSize:1));
            Check.True(found.Total == 1 && found.Items[0].Id == id, "Vietnamese search/pagination failed.");
            Check.True((await catalog.List(new(BrandId:brand, MinPrice:150, StorageGb:64))).Total == 0, "Different variants incorrectly satisfied combined filters.");
            var filtered = await catalog.List(new(BrandId:brand, MaxPrice:150, StorageGb:64, Sort:"price-asc"));
            Check.True(filtered.Items.Single().Variants.Single().StorageGb == 64, "Filtered price/variants did not match.");
            await CommerceContext.Reject("INVALID_INPUT", async () => await catalog.List(new(MinPrice:200, MaxPrice:100)));
            await CommerceContext.Reject("INVALID_INPUT", async () => await catalog.List(new(Sort:"unknown")));
        });
        await Check.Run("K09/A04 public redaction, inactive visibility and internal roles are enforced", async () =>
        {
            using var response = await c.CustomerClient.Http.GetAsync($"/api/catalog/{id}");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Check.True(response.IsSuccessStatusCode && !json.RootElement.GetProperty("variants")[0].TryGetProperty("cost", out _), "Public catalog exposed internal cost.");
            var p = await products.Read(c.Admin, id); await products.Save(c.Admin, id, ProductChecks.Input(p) with { Active = false });
            await CommerceContext.Reject("NOT_FOUND", async () => await catalog.Read(id));
            Check.True((await catalog.Internal(c.Admin, new(BrandId:brand))).Items.Single().Id == id, "Internal query lost discontinued product.");
            await CommerceContext.Reject("FORBIDDEN", async () => await catalog.Internal(c.Customer, new()));
            using var denied = await c.CustomerClient.Http.GetAsync("/api/admin/products");
            Check.True(denied.StatusCode == HttpStatusCode.Forbidden, "Public caller accessed internal products.");
        });
    }
}
