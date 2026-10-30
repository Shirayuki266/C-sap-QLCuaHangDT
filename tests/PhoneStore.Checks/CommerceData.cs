using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal sealed record CommerceData(AdminProduct Product, long SupplierId)
{
    private static int sequence;
    public static async Task<CommerceData> Create(CommerceContext context, int variants = 2)
    {
        var n = Interlocked.Increment(ref sequence);
        await using var db = context.Open(); var kernel = context.Kernel(db);
        var lookups = new LookupService(kernel);
        var brand = await lookups.Save(context.Admin, "brands", 0, new("Fixture brand " + n));
        var category = await lookups.Save(context.Admin, "categories", 0, new("Fixture category " + n));
        var products = new ProductService(kernel);
        var id = await products.Save(context.Admin, 0, new("FIX-"+n, "Điện thoại fixture " + n, brand, category, "Cấu hình", "Mô tả", "", true,
            Enumerable.Range(1, variants).Select(i => new VariantInput(0, $"FIX-{n}-{i}", "Màu " + i, 128*i, 1000m*i, 20)).ToList()));
        var supplier = await new SupplierService(kernel).Save(context.Admin, 0, new("FIX-SUP-"+n, "Nhà cung cấp fixture " + n, "0900000099", "Địa chỉ fixture"));
        return new(await products.Read(context.Admin, id), supplier);
    }
}
