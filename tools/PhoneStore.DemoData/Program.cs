using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhoneStore.DemoData.Cli;
using PhoneStore.DemoData.Contracts;
using PhoneStore.DemoData.Generation;
using PhoneStore.DemoData.Persistence;
using PhoneStore.DemoData.Verification;

namespace PhoneStore.DemoData;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
            var options = Options.Parse(args, today);
            var connection = Options.GuardConnection(Environment.GetEnvironmentVariable("ConnectionStrings__Store"));
            var store = new SeedStore(connection);
            if (options.VerifyOnly)
            {
                using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(options.ManifestPath!));
                var canonicalInput = manifest.RootElement.GetProperty("Manifest");
                var context = canonicalInput.GetProperty("Context").Deserialize<DatasetContext>() ?? throw new InvalidOperationException("Thiếu metadata.");
                await using var readDb = store.Open();
                var verification = await DatasetVerifier.Run(readDb, context, options.ImagesDirectory);
                var current = await BusinessManifest.Read(readDb);
                if (BusinessManifest.Hash(current) != BusinessManifest.Hash(canonicalInput.GetProperty("Data"))) verification.Errors.Add("Persisted normalized manifest differs");
                if (BusinessManifest.Hash(canonicalInput) != manifest.RootElement.GetProperty("Hash").GetString()) verification.Errors.Add("Manifest file checksum differs");
                foreach (var asset in canonicalInput.GetProperty("Images").EnumerateArray())
                {
                    var file = asset.GetProperty("File").GetString()!;
                    if (Path.GetFileName(file) != file || !File.Exists(Path.Combine(options.ImagesDirectory, file)) ||
                        Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(options.ImagesDirectory, file)))).ToLowerInvariant() != asset.GetProperty("Sha256").GetString())
                        verification.Errors.Add("Static image checksum differs");
                }
                verification = verification with { Valid = verification.Errors.Count == 0 };
                await Save(options.Output, verification);
                Print(verification);
                return verification.Valid ? 0 : 1;
            }
            var secrets = Secrets.Read(Environment.GetEnvironmentVariable);
            var plan = DemoPlan.Create(options);
            var timeline = SalesTimeline.Create(plan);
            var assets = SvgAssets.Render(plan);
            // Image collision preflight occurs before any database writes.
            SvgAssets.WriteNew(options.ImagesDirectory, assets);
            var timer = Stopwatch.StartNew();
            await store.Seed(plan, secrets, options.Reset, timeline);
            await using var db = store.Open();
            await FoundationVerifier.Verify(db, options.Scale, options.ImagesDirectory);
            var report = await DatasetVerifier.Run(db, timeline.Context, options.ImagesDirectory);
            var business = await BusinessManifest.Read(db);
            var canonical = new
            {
                plan.Seed, plan.AsOf, plan.Scale,
                Context = timeline.Context,
                Launches = plan.Products.Select(x => new { x.Code, x.LaunchAt }).ToArray(),
                Data = business,
                Images = assets.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new
                {
                    File = x.Key,
                    Sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(x.Value))).ToLowerInvariant()
                }).ToArray()
            };
            var hash = BusinessManifest.Hash(canonical);
            var summary = new { Stage = "D2", plan.Seed, plan.AsOf, plan.Scale, Hash = hash,
                ElapsedMilliseconds = timer.ElapsedMilliseconds, Manifest = canonical, Verification = report };
            await Save(options.Output, summary);
            Print(report);
            Console.WriteLine($"D2: manifest SHA256 {hash}; {timer.ElapsedMilliseconds} ms.");
            return report.Valid ? 0 : 1;
        }
        catch (ArgumentException error)
        {
            // Only locally constructed validation messages; never print user values or SQL exceptions.
            Console.Error.WriteLine(error.ParamName is null && error.GetType() == typeof(ArgumentException)
                ? error.Message : "Tham số không hợp lệ.");
            return 2;
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Dữ liệu demo thất bại: kiểm tra schema đã migrate, quyền SQL, CSDL rỗng/--reset, manifest và đường dẫn ảnh. Không in bí mật hoặc chi tiết kết nối. Nếu lần tạo dở để lại dữ liệu, cần --reset chủ động.");
            return 1;
        }
    }

    private static async Task Save(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Print(VerificationReport report)
    {
        foreach (var (table, count) in report.Counts) Console.WriteLine($"{table}: {count}");
        foreach (var quarter in report.Quarters) Console.WriteLine($"{quarter.Year}-Q{quarter.Quarter}: {quarter.Orders} orders (target {quarter.Target}), revenue {quarter.Revenue:0.00}");
        foreach (var (status, count) in report.Statuses) Console.WriteLine($"{status}: {count}");
        foreach (var (metric, value) in report.Metrics) Console.WriteLine($"{metric}: {value}");
        foreach (var error in report.Errors.Distinct()) Console.WriteLine("FAIL: " + error);
        Console.WriteLine(report.Valid ? "Verification PASS" : "Verification FAIL");
    }
}
