using System.Text.Json;

namespace PhoneStore.DemoData.Generation;

// Frozen manufacturer facts/photos. Generation never fetches mutable remote content.
public static class RealCatalog
{
    public sealed record Product(string Code, string Brand, string Name, string SourceUrl,
        string Display, string Chip, string Ram, int[] Storage, string[] Colors,
        string Camera, string Battery, string Charging, string Os, string Network,
        Photo[] Photos);
    public sealed record Photo(string File, string SourceUrl, string Sha256, int Width, int Height, string WrapperSha256);
    public static IReadOnlyList<Product> Products { get; } = Load();

    private static Product[] Load()
    {
        using var stream = typeof(RealCatalog).Assembly.GetManifestResourceStream("PhoneStore.DemoData.RealCatalog.json")
            ?? throw new InvalidOperationException("Thiếu danh mục thật đã đóng gói.");
        var products = JsonSerializer.Deserialize<Product[]>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Danh mục thật không hợp lệ.");
        if (products.Length != 50 || products.Select(x => x.Code).Distinct().Count() != 50 || products.Select(x => x.Brand).Distinct().Count() != 10 ||
            products.Any(x => !Uri.TryCreate(x.SourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" || x.Photos.Length != 3 ||
                x.Storage.Length == 0 || x.Colors.Length == 0 || x.Storage.Length * x.Colors.Length < 2))
            throw new InvalidOperationException("Sai quy mô hoặc nguồn danh mục thật.");
        return products;
    }

    public static DemoPlan Apply(DemoPlan original)
    {
        if (original.AsOf < new DateOnly(2026, 10, 1)) throw new ArgumentException("Danh mục thật cần --as-of từ 2026-10-01.");
        var facts = Products.Take(original.Products.Length).ToArray();
        var brands = Products.Select(x => x.Brand).Distinct(StringComparer.Ordinal).ToArray();
        var counts = Enumerable.Repeat(2, facts.Length).ToArray();
        var target = original.Scale == "full" ? 125 : 15;
        if (facts.Sum(x => Math.Min(3, x.Storage.Length * x.Colors.Length)) < target)
            throw new InvalidOperationException("Danh mục thật thiếu cấu hình hợp lệ để đạt quy mô.");
        for (var i = 0; counts.Sum() < target; i = (i + 1) % counts.Length)
            if (counts[i] < Math.Min(3, facts[i].Storage.Length * facts[i].Colors.Length)) counts[i]++;
        var products = original.Products.Select((basis, i) =>
        {
            var fact = facts[i];
            var combinations = fact.Colors.SelectMany(color => fact.Storage.Select(storage => (storage, color))).ToArray();
            var variants = Enumerable.Range(0, counts[i]).Select(v =>
            {
                var configuration = combinations[v];
                return new PlannedVariant($"{fact.Code}-{configuration.storage}-{v + 1:00}", configuration.color, configuration.storage,
                    basis.Variants[0].Cost + v * 250000m, basis.Variants[0].ProfitPercent);
            }).ToArray();
            var description = $"{fact.Name} kết hợp màn hình {fact.Display} với bộ xử lý {fact.Chip}. " +
                $"Cụm camera sau {fact.Camera} mang lại nhiều lựa chọn ghi hình cho nhu cầu hằng ngày. " +
                $"Máy hỗ trợ kết nối {fact.Network}, sử dụng {fact.Os} và có các tùy chọn bộ nhớ {string.Join("/", fact.Storage)} GB.";
            var specs = $"Màn hình: {fact.Display}\nChip: {fact.Chip}\nRAM: {fact.Ram}\nBộ nhớ: {string.Join("/", fact.Storage)} GB\n" +
                $"Camera sau: {fact.Camera}\nPin: {fact.Battery}\nSạc: {fact.Charging}\nHệ điều hành: {fact.Os}\nKết nối: {fact.Network}";
            return basis with { Code = fact.Code, Name = fact.Name, Brand = Array.IndexOf(brands, fact.Brand), Description = description, Specifications = specs, Variants = variants };
        }).ToArray();
        return original with { Brands = brands, Products = products, Catalog = "real" };
    }

    public static Dictionary<string, string> Render(DemoPlan plan)
    {
        var assets = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var product in plan.Products)
        foreach (var photo in Products.Single(x => x.Code == product.Code).Photos)
        {
            using var stream = typeof(RealCatalog).Assembly.GetManifestResourceStream("PhoneStore.DemoData.RealImages." + photo.File)
                ?? throw new InvalidOperationException("Thiếu ảnh sản phẩm thật đã đóng gói.");
            using var reader = new StreamReader(stream);
            assets.Add(photo.File, reader.ReadToEnd());
        }
        return assets;
    }
}
