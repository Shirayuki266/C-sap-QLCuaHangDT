using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhoneStore.DemoData.Cli;
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
            var secrets = Secrets.Read(Environment.GetEnvironmentVariable);
            var plan = DemoPlan.Create(options);
            var assets = SvgAssets.Render(plan);
            // Image collision preflight occurs before any database writes.
            SvgAssets.WriteNew(options.ImagesDirectory, assets);
            var timer = Stopwatch.StartNew();
            var store = new SeedStore(connection);
            await store.Seed(plan, secrets, options.Reset);
            await using var db = store.Open();
            await FoundationVerifier.Verify(db, options.Scale, options.ImagesDirectory);
            var business = await BusinessManifest.Read(db);
            var canonical = new
            {
                plan.Seed, plan.AsOf, plan.Scale,
                Launches = plan.Products.Select(x => new { x.Code, x.LaunchAt }).ToArray(),
                Data = business,
                Images = assets.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new
                {
                    File = x.Key,
                    Sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(x.Value))).ToLowerInvariant()
                }).ToArray()
            };
            var hash = BusinessManifest.Hash(canonical);
            var summary = new { Stage = "D1", plan.Seed, plan.AsOf, plan.Scale, Hash = hash,
                ElapsedMilliseconds = timer.ElapsedMilliseconds, Manifest = canonical };
            Directory.CreateDirectory(Path.GetDirectoryName(options.Output)!);
            await File.WriteAllTextAsync(options.Output, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"D1 OK: {plan.Products.Length} products, {plan.Customers.Length} customers, manifest SHA256 {hash}; {timer.ElapsedMilliseconds} ms.");
            return 0;
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
            Console.Error.WriteLine("D1 thất bại: kiểm tra schema đã migrate, quyền SQL, CSDL rỗng/--reset, đường dẫn ảnh và kết quả kiểm chứng. Không in bí mật hoặc chi tiết kết nối. Nếu lần chạy dở để lại dữ liệu, cần --reset chủ động.");
            return 1;
        }
    }
}
