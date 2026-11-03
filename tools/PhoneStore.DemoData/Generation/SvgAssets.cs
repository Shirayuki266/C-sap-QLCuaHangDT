using System.Security;
using System.Text;

namespace PhoneStore.DemoData.Generation;

public static class SvgAssets
{
    public static string FileName(string code, int view) => $"{code.ToLowerInvariant()}-{view}.svg";
    public static string Url(string code, int view) => "/images/demo/" + FileName(code, view);

    public static Dictionary<string, string> Render(DemoPlan plan)
    {
        if (plan.Catalog == "real") return RealCatalog.Render(plan);
        var assets = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] palette = ["#273f65", "#43684c", "#66466e", "#856042", "#355e69", "#635d3c", "#774754", "#425a7a", "#56683e", "#675b80"];
        foreach (var product in plan.Products)
        for (var view = 1; view <= 3; view++)
        {
            var name = SecurityElement.Escape(product.Name);
            var color = palette[product.Brand];
            var body = view switch
            {
                1 => $"<rect x='180' y='90' width='240' height='510' rx='32' fill='{color}'/><rect x='192' y='108' width='216' height='474' rx='24' fill='#d9e8ed'/><circle cx='300' cy='127' r='5' fill='#243343'/><path d='M192 460 Q310 260 408 390 V558 Q408 582 384 582 H216 Q192 582 192 558Z' fill='{color}' opacity='.5'/>",
                2 => $"<rect x='180' y='90' width='240' height='510' rx='32' fill='{color}'/><rect x='198' y='110' width='88' height='130' rx='18' fill='#172333'/><circle cx='225' cy='140' r='17' fill='#657d90'/><circle cx='255' cy='185' r='17' fill='#657d90'/><circle cx='225' cy='218' r='5' fill='#eadab7'/>",
                _ => $"<path d='M240 100 L360 72 Q382 68 382 95 V565 Q382 590 360 598 L240 630 Q218 636 218 610 V130 Q218 106 240 100Z' fill='{color}'/><path d='M240 118 L358 90 V575 L240 610Z' fill='#d9e8ed'/><path d='M364 210 V280' stroke='#a9bac5' stroke-width='5'/>"
            };
            // No line terminator: Git's Windows autocrlf cannot change these asset bytes/hashes.
            assets.Add(FileName(product.Code, view), $"<svg xmlns='http://www.w3.org/2000/svg' width='600' height='800' viewBox='0 0 600 800' role='img' aria-labelledby='title'><title id='title'>{name} — {new[] { "Mặt trước", "Mặt sau", "Góc nghiêng" }[view - 1]}</title><rect width='600' height='800' fill='#f3f5f6'/>{body}<text x='300' y='690' text-anchor='middle' font-family='sans-serif' font-size='22' fill='#243343'>{name}</text><text x='300' y='728' text-anchor='middle' font-family='sans-serif' font-size='16' fill='{color}'>{product.Variants[view - 1 < product.Variants.Length ? view - 1 : 0].Color}</text></svg>");
        }
        return assets;
    }

    public static void WriteNew(string directory, IReadOnlyDictionary<string, string> assets)
    {
        // Preflight every collision before creating anything. Existing website assets are never replaced.
        foreach (var (name, content) in assets)
        {
            var path = Path.Combine(directory, name);
            if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) != content)
                throw new InvalidOperationException("Ảnh hiện có khác nội dung; không ghi đè tệp website.");
        }
        Directory.CreateDirectory(directory);
        foreach (var (name, content) in assets)
        {
            var path = Path.Combine(directory, name);
            if (!File.Exists(path)) File.WriteAllText(path, content, new UTF8Encoding(false));
        }
    }
}
