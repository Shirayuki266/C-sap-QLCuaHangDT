using System.Text;

namespace PhoneStore.DemoData.Generation;

public static class SvgAssets
{
    public static string FileName(string code, int view) => $"{code.ToLowerInvariant()}-{view}.svg";
    public static string Url(string code, int view) => "/images/demo/" + FileName(code, view);

    public static Dictionary<string, string> Render(DemoPlan plan)
    {
        if (plan.Catalog != "real") throw new ArgumentException("Không xuất ảnh catalog lịch sử.");
        return RealCatalog.Render(plan);
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
