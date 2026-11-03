using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PhoneStore.DemoData.Cli;
using PhoneStore.DemoData.Generation;
using PhoneStore.DemoData.Persistence;
using PhoneStore.DemoData.Verification;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.DemoData.Checks;

internal static class Program
{
    private static int passed;
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    private static async Task Check(string name, Func<Task> action) { await action(); passed++; Console.WriteLine("PASS " + name); }
    private static void Refuses(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Expected refusal.");
    }
    private static Options OptionsFor(string scale = "full") => new(2611, Today, scale, false, "unused", "unused");

    public static async Task<int> Main(string[] args)
    {
        try
        {
            Check("DATA.SAFE.01 full identifier allowlist, no file attachment", () =>
            {
                foreach (var name in new[] { "PhoneStore_Demo_Test", "PhoneStore_QA_123" })
                    Options.GuardConnection($"Server=unreachable;Database={name};Integrated Security=True");
                foreach (var name in new[] { "PhoneStore_Rebuild", "master", "PhoneStore_Demo_", "xPhoneStore_QA_x", "PhoneStore_Demo_x;Other", "PhoneStore_Demo_x]", "phonestore_demo_x", "PhoneStore" })
                    Refuses(() => Options.GuardConnection(new SqlConnectionStringBuilder { DataSource = "unreachable", InitialCatalog = name }.ConnectionString));
                Refuses(() => Options.GuardConnection("Server=x;Database=PhoneStore_QA_x;AttachDbFilename=C:\\x.mdf"));
            });
            Check("DATA.SAFE.02 environment required, validation and redaction", () =>
            {
                Refuses(() => Secrets.Read(_ => null));
                Refuses(() => Secrets.Read(name => name.EndsWith("ADMIN_PASSWORD", StringComparison.Ordinal) ? "abcdefghij1" : null));
                Refuses(() => Secrets.Read(_ => "short"));
                var secret = "Aa1" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
                Require(!Secrets.Read(_ => secret).ToString().Contains(secret, StringComparison.Ordinal), "Secret string leak.");
            });
            Check("DATA.SAFE.03 explicit flags, strict dates, scale, unknown/repeated args", () =>
            {
                string[] valid = ["--seed-demo-large", "--confirm", "--images-dir", ".artifacts/images", "--output", ".artifacts/summary.json"];
                var parsed = Options.Parse(valid, Today);
                Require(parsed.AsOf == Today && parsed.Scale == "full", "Defaults differ.");
                Refuses(() => Options.Parse([], Today));
                Refuses(() => Options.Parse(valid.Where(x => x != "--confirm").ToArray(), Today));
                foreach (var bad in new[] { new[] { "--confirm" }, new[] { "--scale", "large" }, new[] { "--as-of", Today.AddDays(1).ToString("yyyy-MM-dd") }, new[] { "--as-of", "not-date" }, new[] { "--seed", "x" }, new[] { "--password", "hidden" }, new[] { "--seed" } })
                    Refuses(() => Options.Parse([.. valid, .. bad], Today));
                string[] verification = ["--verify-demo-data", "--manifest", ".artifacts/manifest.json", "--images-dir", ".artifacts/images", "--output", ".artifacts/verification.json"];
                Require(Options.Parse(verification, Today).VerifyOnly, "Read-only mode not selected.");
                foreach (var mutation in new[] { "--confirm", "--reset", "--seed-demo-large" }) Refuses(() => Options.Parse([.. verification, mutation], Today));
                Refuses(() => Options.Parse(["--verify-demo-data", "--images-dir", ".artifacts/images", "--output", ".artifacts/verification.json"], Today));
                Refuses(() => Options.Parse(["--verify-demo-data", "--manifest", ".artifacts/same.json", "--images-dir", ".artifacts/images", "--output", ".artifacts/same.json"], Today));
            });
            Check("full/small plan unique identities, limits, segments, history, independent operation keys", () =>
            {
                foreach (var scale in new[] { "small", "full" })
                {
                    var plan = DemoPlan.Create(OptionsFor(scale));
                    Require(plan.Products.Length == (scale == "full" ? 50 : 6) && plan.Products.Sum(x => x.Variants.Length) == (scale == "full" ? 125 : 15), "Scale.");
                    Require(plan.Products.Select(x => x.Code).Distinct().Count() == plan.Products.Length && plan.Products.SelectMany(x => x.Variants).Select(x => x.Sku).Distinct().Count() == plan.Products.Sum(x => x.Variants.Length), "Product/SKU uniqueness.");
                    Require(plan.Products.All(x => x.Name.Length <= 200 && x.Description.Length <= 50000 && x.Specifications.Length <= 20000 && x.Specifications.Split('\n').Length >= 9 && x.Variants.All(v => v.Sku.Length <= 50 && v.Color.Length <= 50)), "Product text limits.");
                    Require(plan.Products.Select(x => x.Description).Distinct().Count() == plan.Products.Length && plan.Products.Select(x => x.Specifications).Distinct().Count() == plan.Products.Length, "Repeated text.");
                    Require(plan.Customers.Select(x => x.Username).Distinct().Count() == plan.Customers.Length && plan.Customers.Select(x => x.Phone).Distinct().Count() == plan.Customers.Length, "Customer uniqueness.");
                    Require(plan.Customers.All(x => x.Username == x.Username.ToLowerInvariant() && x.Name.Length <= 120 && x.Address.Length <= 500 && (x.Email is null || x.Email.EndsWith("@example.com", StringComparison.Ordinal))), "Customer text.");
                    Require(plan.Customers.Select(x => x.CreatedAt).SequenceEqual(plan.Customers.Select(x => x.CreatedAt).Order()) && plan.Customers.All(x => x.CreatedAt < DateTime.UtcNow), "Customer timeline.");
                    Require(Enumerable.Range(0, 100).Select(i => DemoPlan.Key(plan.Seed, $"action/{i}")).Distinct().Count() == 100 && DemoPlan.Key(plan.Seed, "purchase/create") != DemoPlan.Key(plan.Seed, "purchase/confirm"), "Operation key reuse.");
                }
                Require(DemoPlan.Create(OptionsFor()).Products.GroupBy(x => x.Category).OrderBy(x => x.Key).Select(x => x.Count()).SequenceEqual(new[] { 14, 16, 12, 4, 4 }), "Segments.");
            });
            Check("DATA.SAFE.05 deterministic plans, changed seed changes data, 150 valid SVG views", () =>
            {
                var plan = DemoPlan.Create(OptionsFor());
                Require(BusinessManifest.Hash(plan) == BusinessManifest.Hash(DemoPlan.Create(OptionsFor())), "Unstable plan.");
                Require(BusinessManifest.Hash(plan) != BusinessManifest.Hash(DemoPlan.Create(OptionsFor() with { Seed = 2612 })), "Seed ignored.");
                var assets = SvgAssets.Render(plan);
                Require(assets.Count == 150 && assets.Values.Distinct().Count() == 150, "Repeated images.");
                foreach (var svg in assets.Values) Require(XDocument.Parse(svg).Root?.Name.LocalName == "svg", "Invalid SVG.");
            });
            Check("image export preserves existing assets and preflights all collisions", () =>
            {
                var directory = Path.Combine(FindRoot(), ".artifacts", "demo-data", "collision-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "keep.svg"), "existing");
                var refused = false;
                try { SvgAssets.WriteNew(directory, new Dictionary<string, string> { ["new.svg"] = "new", ["keep.svg"] = "different" }); }
                catch (InvalidOperationException) { refused = true; }
                Require(refused && File.ReadAllText(Path.Combine(directory, "keep.svg")) == "existing" && !File.Exists(Path.Combine(directory, "new.svg")), "Image overwrite or partial preflight.");
            });
            Check("D2 complete chronological plans including first-of-month/quarter and seed variation", () =>
            {
                var quarterStart = new DateOnly(Today.Year, (Today.Month - 1) / 3 * 3 + 1, 1);
                var dates = new[] { Today, new DateOnly(Today.Year, Today.Month, 1), quarterStart, quarterStart.AddDays(-1) }.Distinct();
                foreach (var date in dates)
                foreach (var scale in new[] { "small", "full" })
                foreach (var seed in new[] { 2611, 7 })
                {
                    var timeline = SalesTimeline.Create(DemoPlan.Create(OptionsFor(scale) with { AsOf = date, Seed = seed }));
                    Require(timeline.Sales.Count == (scale == "full" ? 601 : 60), "Order quota.");
                    Require(timeline.Receipts.Count == (scale == "full" ? 56 : 22), "Purchase quota.");
                    Require(timeline.Events.All(x => x.At <= timeline.Context.Cutoff), "Future event.");
                    Require(timeline.Sales.Count(x => x.CreatedAt >= timeline.Context.Cutoff.AddDays(-30)) >= (scale == "full" ? 20 : 6), "Recent window at month/quarter end.");
                    Require(timeline.Sales.All(x => x.Lines.Count is >= 1 and <= 3), "Missing order lines.");
                    Require(timeline.Counts.Count == (scale == "full" ? 10 : 6), "Count scenarios.");
                    Require(timeline.Carts.Count == (scale == "full" ? 25 : 6), "Cart scenarios.");
                }
            });
            if (args.Contains("--integration")) await Integration();
            Console.WriteLine($"PASS {passed} groups" + (args.Contains("--integration") ? " (real SQL + Kestrel)" : " (unit; SQL NOT RUN)"));
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL " + error.GetType().Name + ": " + error.Message);
            return 1;
        }
    }

    private static async Task Integration()
    {
        var configured = Environment.GetEnvironmentVariable("PHONESTORE_TEST_CONNECTION") ?? throw new InvalidOperationException("Missing QA connection.");
        var target = new SqlConnectionStringBuilder(Options.GuardConnection(configured));
        Require(target.InitialCatalog.StartsWith("PhoneStore_QA_", StringComparison.Ordinal) &&
            target.DataSource == Environment.GetEnvironmentVariable("PHONESTORE_QA_SERVER") &&
            target.InitialCatalog == Environment.GetEnvironmentVariable("PHONESTORE_QA_DATABASE"), "QA guard mismatch.");
        var name = "PhoneStore_QA_DemoD1_" + Guid.NewGuid().ToString("N");
        target.InitialCatalog = name;
        var store = new SeedStore(target.ConnectionString);
        var root = FindRoot();
        var directory = Path.Combine(root, ".artifacts", "demo-data", name);
        var images = Path.Combine(directory, "images");
        var summary = Path.Combine(directory, "summary.json");
        var secrets = new Secrets("Aa1" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20)), "Bb2" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20)));
        var previous = new[] { "ConnectionStrings__Store", "PHONESTORE_DEMO_ADMIN_PASSWORD", "PHONESTORE_DEMO_CUSTOMER_PASSWORD" }
            .ToDictionary(x => x, Environment.GetEnvironmentVariable);
        try
        {
            await using (var db = store.Open()) await new DatabaseMigrator(db).MigrateAsync();
            Environment.SetEnvironmentVariable("ConnectionStrings__Store", target.ConnectionString);
            Environment.SetEnvironmentVariable("PHONESTORE_DEMO_ADMIN_PASSWORD", secrets.Admin);
            Environment.SetEnvironmentVariable("PHONESTORE_DEMO_CUSTOMER_PASSWORD", secrets.Customer);
            string[] cli = ["--seed-demo-large", "--confirm", "--seed", "2611", "--as-of", Today.ToString("yyyy-MM-dd"), "--scale", "small", "--images-dir", images, "--output", summary];
            await Check("CLI small writes real SQL, SQL verifier, salted passwords and zero sessions", async () =>
            {
                Require(await RunCli(cli, secrets) == 0, "CLI failed.");
                await using var db = store.Open();
                var account = await db.Accounts.SingleAsync(x => x.Username == "demo.admin.large");
                Require(new PasswordHasher<Account>().VerifyHashedPassword(account, account.PasswordHash, secrets.Admin) != PasswordVerificationResult.Failed, "Admin hash.");
                var hashes = await db.Accounts.Where(x => x.Role == "CUSTOMER").Select(x => x.PasswordHash).ToListAsync();
                Require(hashes.Distinct().Count() == 6 && hashes.All(x => x != secrets.Customer), "Customer salted hashes.");
                await FoundationVerifier.Verify(db, "small", images);
                var contents = await File.ReadAllTextAsync(summary);
                Require(!contents.Contains(secrets.Admin, StringComparison.Ordinal) && !contents.Contains(secrets.Customer, StringComparison.Ordinal) && !contents.Contains("PasswordHash", StringComparison.Ordinal), "Manifest secret leak.");
            });
            await Check("nonempty refusal is non-destructive; reset produces same persisted manifest", async () =>
            {
                var before = await File.ReadAllTextAsync(summary);
                await using var db = store.Open();
                var hash = BusinessManifest.Hash(await BusinessManifest.Read(db));
                Require(await RunCli(cli, secrets) != 0 && BusinessManifest.Hash(await BusinessManifest.Read(db)) == hash, "Nonempty target changed.");
                Require(await RunCli([.. cli, "--reset"], secrets) == 0, "Reset failed.");
                Require(BusinessManifest.Hash(await BusinessManifest.Read(db)) == hash, "Persisted manifest not reproducible.");
                using var first = JsonDocument.Parse(before);
                using var second = JsonDocument.Parse(await File.ReadAllTextAsync(summary));
                Require(first.RootElement.GetProperty("Hash").GetString() == second.RootElement.GetProperty("Hash").GetString(), "CLI normalized hash changed.");
            });
            await Check("independent SQL verifier detects tampered price", async () =>
            {
                await using var db = store.Open();
                var id = await db.Variants.MinAsync(x => x.Id);
                await db.Variants.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Price, x => x.Price + 1));
                var refused = false;
                try { await FoundationVerifier.Verify(db, "small", images); } catch (InvalidOperationException) { refused = true; }
                Require(refused, "Tampered price accepted.");
                await db.Variants.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Price, x => x.Price - 1));
            });
            await Check("real SQL rejects duplicate SKU and overlong product name without changing data", async () =>
            {
                await using var db = store.Open();
                var variants = await db.Variants.OrderBy(x => x.Id).Take(2).ToArrayAsync();
                var duplicate = false;
                try { await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.BIEN_THE SET Sku={variants[0].Sku} WHERE Id={variants[1].Id}"); }
                catch (SqlException error) when (error.Number is 2601 or 2627) { duplicate = true; }
                var overlong = false;
                var product = await db.Products.OrderBy(x => x.Id).FirstAsync();
                var before = BusinessManifest.Hash(await BusinessManifest.Read(db));
                try { await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.SAN_PHAM SET Name={new string('x', 201)} WHERE Id={product.Id}"); }
                catch (SqlException error) when (error.Number is 2628 or 8152) { overlong = true; }
                Require(duplicate && overlong && BusinessManifest.Hash(await BusinessManifest.Read(db)) == before, "SQL uniqueness/length enforcement failed.");
            });
            await Check("missing secrets and forbidden target refuse before images/database writes", async () =>
            {
                await using var db = store.Open();
                var hash = BusinessManifest.Hash(await BusinessManifest.Read(db));
                Environment.SetEnvironmentVariable("PHONESTORE_DEMO_CUSTOMER_PASSWORD", null);
                Require(await RunCli([.. cli, "--reset"], secrets) != 0, "Missing secret accepted.");
                Environment.SetEnvironmentVariable("PHONESTORE_DEMO_CUSTOMER_PASSWORD", secrets.Customer);
                Environment.SetEnvironmentVariable("ConnectionStrings__Store", "Server=unreachable;Database=PhoneStore_Rebuild;Integrated Security=True");
                Require(await RunCli([.. cli, "--reset"], secrets) != 0, "Protected target accepted.");
                Environment.SetEnvironmentVariable("ConnectionStrings__Store", target.ConnectionString);
                Require(BusinessManifest.Hash(await BusinessManifest.Read(db)) == hash, "Safety refusal changed data.");
            });
            await Check("real Kestrel serves all 150 committed SVGs with HTTP 200", async () => await HttpImages(root, target.ConnectionString));
            passed += await D2VerificationChecks.Run(store, summary, images, secrets);
        }
        finally
        {
            foreach (var (key, value) in previous) Environment.SetEnvironmentVariable(key, value);
            SqlConnection.ClearAllPools();
            var masterTarget = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };
            await using var master = new SqlConnection(masterTarget.ConnectionString);
            await master.OpenAsync();
            await using var drop = master.CreateCommand();
            drop.CommandText = $"IF DB_ID('{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END";
            await drop.ExecuteNonQueryAsync();
        }
    }

    internal static async Task<int> RunCli(string[] args, Secrets secrets)
    {
        var stdout = Console.Out;
        var stderr = Console.Error;
        using var captured = new StringWriter();
        int code;
        try { Console.SetOut(captured); Console.SetError(captured); code = await PhoneStore.DemoData.Program.Main(args); }
        finally { Console.SetOut(stdout); Console.SetError(stderr); }
        Require(!captured.ToString().Contains(secrets.Admin, StringComparison.Ordinal) && !captured.ToString().Contains(secrets.Customer, StringComparison.Ordinal), "CLI log secret leak.");
        if (code != 0) stdout.WriteLine("CLI nonzero: " + string.Join(" | ", captured.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(8)));
        return code;
    }

    private static string FindRoot()
    {
        var path = new DirectoryInfo(AppContext.BaseDirectory);
        while (path is not null && !File.Exists(Path.Combine(path.FullName, "PhoneStore.slnx"))) path = path.Parent;
        return path?.FullName ?? throw new InvalidOperationException("Repository root missing.");
    }

    private static async Task HttpImages(string root, string connection)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = Path.Combine(root, "src", "PhoneStore.Web"), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(Path.Combine(root, "src", "PhoneStore.Web", "bin", "Release", "net10.0", "PhoneStore.Web.dll"));
        start.ArgumentList.Add("--urls"); start.ArgumentList.Add($"http://127.0.0.1:{port}");
        start.Environment["ConnectionStrings__Store"] = connection;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        start.Environment.Remove("PHONESTORE_DEMO_ADMIN_PASSWORD"); start.Environment.Remove("PHONESTORE_DEMO_CUSTOMER_PASSWORD");
        using var host = Process.Start(start) ?? throw new InvalidOperationException("Host failed.");
        var output = host.StandardOutput.ReadToEndAsync(); var errors = host.StandardError.ReadToEndAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(5) };
            var ready = false;
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (host.HasExited) throw new InvalidOperationException("Host exited before readiness.");
                try { ready = (await client.GetAsync("/health/ready")).IsSuccessStatusCode; } catch (HttpRequestException) { }
                if (ready) break;
                await Task.Delay(200);
            }
            Require(ready, "Host readiness timeout.");
            var plan = DemoPlan.Create(OptionsFor());
            foreach (var (file, svg) in SvgAssets.Render(plan))
            {
                var physical = Path.Combine(root, "src", "PhoneStore.Web", "wwwroot", "images", "demo", file);
                Require(File.Exists(physical) && await File.ReadAllTextAsync(physical) == svg, "Committed SVG differs from generator.");
                using var response = await client.GetAsync("/images/demo/" + file);
                Require(response.StatusCode == HttpStatusCode.OK && response.Content.Headers.ContentType?.MediaType == "image/svg+xml" && await response.Content.ReadAsStringAsync() == svg, "HTTP SVG response differs.");
            }
        }
        finally { if (!host.HasExited) host.Kill(true); await host.WaitForExitAsync(); await output; await errors; }
    }
}
