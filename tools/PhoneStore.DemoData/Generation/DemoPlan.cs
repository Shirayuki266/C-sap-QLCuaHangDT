using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PhoneStore.DemoData.Cli;

namespace PhoneStore.DemoData.Generation;

public sealed record PlannedVariant(string Sku, string Color, int StorageGb, decimal Cost, decimal ProfitPercent);
public sealed record PlannedProduct(string Code, string Name, int Brand, int Category, string Description,
    string Specifications, DateTime LaunchAt, PlannedVariant[] Variants);
public sealed record PlannedCustomer(string Username, string Name, string Phone, string? Email, string Address, DateTime CreatedAt, bool Locked);
public sealed record PlannedSupplier(string Code, string Name, string Phone, string Address);
public sealed record DemoPlan(int Seed, DateOnly AsOf, string Scale, DateTime Start, string[] Brands, string[] Categories,
    PlannedProduct[] Products, PlannedCustomer[] Customers, PlannedSupplier[] Suppliers)
{
    public string Catalog { get; init; } = "real";
    // Counter-based SHA256 avoids dependence on runtime Random implementation or invocation order.
    public static int Number(int seed, string domain, int maximum)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(FormattableString.Invariant($"{seed}:{domain}")));
        return (int)(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(digest) % (uint)maximum);
    }

    public static Guid Key(int seed, string operation)
        => new(SHA256.HashData(Encoding.UTF8.GetBytes(FormattableString.Invariant($"operation:{seed}:{operation}"))).AsSpan(0, 16));

    public static DemoPlan Create(Options options)
    {
        if (options.Catalog != "real") throw new ArgumentException("Công cụ demo chỉ tạo catalog thật.");
        var quarter = new DateOnly(options.AsOf.Year, (options.AsOf.Month - 1) / 3 * 3 + 1, 1).AddMonths(-24);
        var start = quarter.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
        var brands = RealCatalog.Products.Select(x => x.Brand).Distinct(StringComparer.Ordinal).ToArray();
        string[] categories = ["Giá rẻ", "Tầm trung", "Cao cấp", "Gaming", "Màn hình gập", "Thiết kế gọn nhẹ"];
        var full = options.Scale == "full";
        var count = full ? 50 : 6;
        var products = Enumerable.Range(0, count).Select(i =>
        {
            var category = full ? i < 14 ? 0 : i < 30 ? 1 : i < 42 ? 2 : i < 46 ? 3 : 4 : i % 5;
            var fact = RealCatalog.Products[i];
            var baseCost = new[] { 2500000m, 5000000m, 14000000m, 12000000m, 20000000m }[category];
            var step = new[] { 100000m, 200000m, 500000m, 350000m, 500000m }[category];
            var cost = baseCost + Number(options.Seed, $"cost/{i}", 18) * step;
            var profit = new[] { 15m, 12m, 10m, 11m, 9m }[category];
            PlannedVariant[] variants = [new($"{fact.Code}-{fact.Storage[0]}-01", fact.Colors[0], fact.Storage[0], cost, profit)];
            var launch = i < (full ? 24 : 3) ? start : start.AddDays(30 + Number(options.Seed, $"launch/{i}", 540));
            return new PlannedProduct(fact.Code, fact.Name, Array.IndexOf(brands, fact.Brand), category, "", "", launch, variants);
        }).ToArray();
        string[] lastNames = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đặng", "Bùi", "Đỗ", "Hồ"];
        string[] givenNames = ["Minh An", "Hoàng Bảo", "Ngọc Châu", "Anh Dũng", "Thu Giang", "Quang Hải"];
        string[] cities = ["TP.HCM", "Hà Nội", "Đà Nẵng", "Cần Thơ", "Hải Phòng", "Bình Dương", "Đồng Nai", "Huế", "Quy Nhơn", "Vũng Tàu"];
        var customerCount = full ? 60 : 6;
        var cutoff = options.AsOf.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
        var customerStart = new DateTimeOffset(cutoff).AddMonths(-24).UtcDateTime;
        var firstCustomerLocalMonth = customerStart.AddHours(7);
        var foundingCustomer = new DateTime(firstCustomerLocalMonth.Year, firstCustomerLocalMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(-7);
        var customers = Enumerable.Range(0, customerCount).Select(i =>
        {
            // Convex spacing increases registrations toward the end, while spanning 24 months.
            var ratio = i / (double)customerCount;
            var days = (int)((cutoff - customerStart).TotalDays * Math.Sqrt(ratio) * .98);
            var phone = $"09{Number(options.Seed, $"phone/{i}", 1000000):000000}{i:00}";
            return new PlannedCustomer($"demo.customer.{i + 1:00}", $"{lastNames[i % 10]} {givenNames[i / 10]}", phone,
                i % 7 == 0 ? null : $"customer{i + 1:00}@example.com",
                $"{1000 + Number(options.Seed, $"address/{i}", 8000)} Đường Minh Họa {i + 1}, Khu Dân Cư Mẫu, {cities[i % 10]}",
                (i == 0 ? foundingCustomer : customerStart.AddDays(days)).AddHours(1), i >= customerCount - (full ? 4 : 1));
        }).ToArray();
        var suppliers = Enumerable.Range(0, full ? 12 : 3).Select(i => new PlannedSupplier($"NCCDM{i + 1:00}",
            $"Phân phối hư cấu {i + 1:00}", $"0289000{i + 1:000}", $"{9000 + i} Khu Kho Minh Họa, {cities[i % 10]}")).ToArray();
        var plan = new DemoPlan(options.Seed, options.AsOf, options.Scale, start, brands, categories, products, customers, suppliers);
        return RealCatalog.Apply(plan);
    }
}
