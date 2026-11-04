using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace PhoneStore.DemoData.Cli;

public sealed record Options(int Seed, DateOnly AsOf, string Scale, bool Reset, string ImagesDirectory, string Output)
{
    public bool VerifyOnly { get; init; }
    public string? ManifestPath { get; init; }
    public string Catalog { get; init; } = "real";
    public static Options Parse(string[] args, DateOnly today)
    {
        var flags = new HashSet<string>(StringComparer.Ordinal);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            var key = args[i];
            if (key is "--seed-demo-large" or "--confirm" or "--reset" or "--verify-demo-data")
            {
                if (!flags.Add(key)) throw new ArgumentException("Cờ bị lặp.");
            }
            else if (key is "--seed" or "--as-of" or "--scale" or "--images-dir" or "--output" or "--manifest" or "--catalog")
            {
                if (++i >= args.Length || args[i].StartsWith("--", StringComparison.Ordinal) || !values.TryAdd(key, args[i]))
                    throw new ArgumentException("Tham số thiếu giá trị hoặc bị lặp.");
            }
            else throw new ArgumentException("Tham số không được hỗ trợ.");
        }
        var verify = flags.Contains("--verify-demo-data");
        if (verify && flags.Any(x => x != "--verify-demo-data")) throw new ArgumentException("Kiểm chứng chỉ đọc không được kèm cờ seed/confirm/reset.");
        if (!verify && (!flags.Contains("--seed-demo-large") || !flags.Contains("--confirm")))
            throw new ArgumentException("Cần --seed-demo-large và --confirm.");
        if (verify && !values.ContainsKey("--manifest")) throw new ArgumentException("Kiểm chứng cần --manifest từ lần tạo dữ liệu.");
        var seed = 2611;
        if (values.TryGetValue("--seed", out var rawSeed) && !int.TryParse(rawSeed, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
            throw new ArgumentException("--seed cần là số nguyên.");
        var asOf = today;
        if (values.TryGetValue("--as-of", out var rawDate) && !DateOnly.TryParseExact(rawDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out asOf))
            throw new ArgumentException("--as-of cần có dạng YYYY-MM-DD.");
        if (asOf > today || asOf.Year < 2002) throw new ArgumentException("--as-of không được ở tương lai hoặc trước năm 2002.");
        var scale = values.GetValueOrDefault("--scale", "full");
        if (scale is not ("small" or "full")) throw new ArgumentException("--scale chỉ nhận small hoặc full.");
        var catalog = values.GetValueOrDefault("--catalog", "real");
        if (catalog != "real") throw new ArgumentException("Công cụ demo chỉ hỗ trợ --catalog real; catalog lịch sử không được sinh lại.");
        if (!verify && catalog == "real" && asOf < new DateOnly(2026, 10, 1))
            throw new ArgumentException("Danh mục thật cần --as-of từ 2026-10-01 để toàn bộ lịch sử bắt đầu sau khi các model đã ra mắt.");
        if (!values.TryGetValue("--images-dir", out var images) || !values.TryGetValue("--output", out var output))
            throw new ArgumentException("Cần --images-dir và --output rõ ràng.");
        var outputPath = Path.GetFullPath(output);
        var manifestPath = values.TryGetValue("--manifest", out var manifest) ? Path.GetFullPath(manifest) : null;
        if (verify && string.Equals(outputPath, manifestPath, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Không ghi đè manifest bằng báo cáo kiểm chứng.");
        return new(seed, asOf, scale, flags.Contains("--reset"), Path.GetFullPath(images), outputPath) { VerifyOnly = verify, ManifestPath = manifestPath, Catalog = catalog };
    }

    public static string GuardConnection(string? connection)
    {
        SqlConnectionStringBuilder target;
        try { target = new(connection ?? ""); }
        catch (ArgumentException) { throw new ArgumentException("Cấu hình SQL không hợp lệ."); }
        // Restrict the entire identifier, not merely a substring; also reject attached-file targets.
        if (string.IsNullOrWhiteSpace(target.DataSource) || !Regex.IsMatch(target.InitialCatalog,
            @"\APhoneStore_(?:Demo|QA)_[A-Za-z0-9_]+\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking) ||
            target.AttachDBFilename.Length != 0 || target.UserInstance)
            throw new ArgumentException("Chỉ chấp nhận CSDL PhoneStore_Demo_* hoặc PhoneStore_QA_* có hậu tố hợp lệ.");
        return target.ConnectionString;
    }
}

public sealed record Secrets(string Admin, string Customer)
{
    public static Secrets Read(Func<string, string?> environment)
    {
        static string Require(string name, Func<string, string?> read)
        {
            var value = read(name);
            if (value is null || value.Length is < 10 or > 128 || !value.Any(char.IsLetter) || !value.Any(char.IsDigit))
                throw new ArgumentException($"Cần biến môi trường {name} với mật khẩu 10–128 ký tự, có chữ và số.");
            return value;
        }
        return new(Require("PHONESTORE_DEMO_ADMIN_PASSWORD", environment), Require("PHONESTORE_DEMO_CUSTOMER_PASSWORD", environment));
    }

    public override string ToString() => "Secrets [redacted]";
}
