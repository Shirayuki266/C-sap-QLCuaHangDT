using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class EmptyDatabaseSmokeChecks
{
    private sealed record MigrationRow(string Id, string Sha256, DateTime AppliedAt);
    private sealed record ResponseProof(string Path, int Status, string? Code = null, int? Items = null);

    public static async Task Run()
    {
        await using var database = await TestDatabase.Create();
        StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(database.ConnectionString).Options);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PhoneStore.slnx"))) root = root.Parent;
        if (root is null) throw new InvalidOperationException("Solution root missing.");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var webDirectory = Path.Combine(root.FullName, "src", "PhoneStore.Web");
        var dll = Path.Combine(webDirectory, "bin", configuration, "net10.0", "PhoneStore.Web.dll");
        var responses = new List<ResponseProof>();
        MigrationRow? installed = null;
        object? schemaProof = null;

        ProcessStartInfo Start(params string[] arguments)
        {
            var info = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = webDirectory };
            info.ArgumentList.Add(dll);
            foreach (var argument in arguments) info.ArgumentList.Add(argument);
            info.Environment["ConnectionStrings__Store"] = database.ConnectionString;
            info.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
            info.Environment["Logging__LogLevel__Default"] = "None";
            // Explicitly withhold optional provisioning/seeding credentials from this no-seed smoke.
            foreach (var name in new[] { "PHONESTORE_ADMIN_USERNAME", "PHONESTORE_ADMIN_PASSWORD", "PHONESTORE_DEMO_ADMIN_PASSWORD", "PHONESTORE_DEMO_CUSTOMER_PASSWORD" })
                info.Environment.Remove(name);
            return info;
        }
        async Task<string> Command(string mode)
        {
            using var process = Process.Start(Start(mode)) ?? throw new InvalidOperationException("CLI process did not start.");
            var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(90)); }
            finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
            var text = await output; await errors;
            Check.True(process.ExitCode == 0, "Empty QA CLI command failed; no connection string or stderr is exported.");
            return text;
        }
        async Task WithHost(Func<HttpClient, Task> action)
        {
            using var lease = new TcpListener(IPAddress.Loopback, 0); lease.Start();
            var port = ((IPEndPoint)lease.LocalEndpoint).Port; lease.Stop();
            using var process = Process.Start(Start("--urls", $"http://127.0.0.1:{port}")) ?? throw new InvalidOperationException("QA host did not start.");
            var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            using var client = new HttpClient(new HttpClientHandler { UseProxy = false })
                { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(10) };
            try
            {
                var ready = false;
                for (var i = 0; i < 100 && !process.HasExited; i++)
                {
                    try { using var response = await client.GetAsync("/health/live"); ready = response.IsSuccessStatusCode; }
                    catch (HttpRequestException) { }
                    if (ready) break;
                    await Task.Delay(100);
                }
                Check.True(ready, "QA host liveness did not become available.");
                await action(client);
            }
            finally
            {
                if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); }
                await output; await errors;
            }
        }
        async Task<List<MigrationRow>> Ledger()
        {
            await using var db = Open();
            return await db.Database.SqlQueryRaw<MigrationRow>("SELECT Id,Sha256,AppliedAt FROM dbo.__PhoneStoreMigrations ORDER BY Id").ToListAsync();
        }
        async Task<Dictionary<string, int>> Counts()
        {
            await using var db = Open();
            var audit = await new DataAudit(db).Run();
            Check.True(audit.Valid, "QA data audit failed.");
            return audit.Counts.Where(row => row.Key != "__PhoneStoreMigrations").ToDictionary(row => row.Key, row => row.Value);
        }
        await Check.Run("Closeout empty QA startup is live but not ready and does not auto-migrate or seed", async () =>
        {
            await using var db = Open();
            Check.True(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE is_ms_shipped=0").SingleAsync() == 0, "QA DB was not empty.");
            await WithHost(async client =>
            {
                using var response = await client.GetAsync("/health/ready");
                var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
                Check.True(response.StatusCode == HttpStatusCode.ServiceUnavailable && body!["code"].GetString() == "DATABASE_UNAVAILABLE", "Blank DB falsely ready.");
                responses.Add(new("/health/ready before migration", 503, "DATABASE_UNAVAILABLE"));
            });
            Check.True(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE is_ms_shipped=0").SingleAsync() == 0, "Startup wrote schema/accounts.");
        });
        await Check.Run("Closeout actual CLI migration installs rebuild001 only with nineteen tables and zero sample rows", async () =>
        {
            await Command("--migrate");
            var rows = await Ledger(); installed = rows.Single();
            Check.True(installed.Id == DatabaseMigrator.MigrationId && installed.Sha256.Length == 64, "Unexpected migration lineage.");
            await using var db = Open();
            Check.True(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE is_ms_shipped=0").SingleAsync() == 19 &&
                await new DatabaseMigrator(db).IsCurrentAsync(), "Installed schema/checksum mismatch.");
            var counts = await Counts(); Check.True(counts.Count == 18 && counts.Values.All(value => value == 0), "Migration seeded data.");
        });
        await Check.Run("Closeout CLI migration replay preserves ledger timestamp checksum and empty application tables", async () =>
        {
            await Command("--migrate");
            Check.True((await Ledger()).Single() == installed && (await Counts()).Values.All(value => value == 0), "Replay mutated data or migration record.");
            using var audit = JsonDocument.Parse(await Command("--audit-db"));
            Check.True(audit.RootElement.GetProperty("Valid").GetBoolean(), "Empty migrated database audit failed.");
        });
        schemaProof = await FinalSchemaSmokeChecks.ReadAndVerify(database.ConnectionString);
        await Check.Run("Closeout migrated QA host smoke checks readiness empty public queries authorization and CSRF", async () =>
        {
            await WithHost(async client =>
            {
                foreach (var path in new[] { "/health/live", "/health/ready", "/api/catalog", "/api/catalog/bestsellers", "/api/lookups/brands" })
                {
                    using var response = await client.GetAsync(path); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    Check.True(response.StatusCode == HttpStatusCode.OK, "Migrated empty smoke query failed: " + path);
                    int? items = null;
                    if (path == "/api/catalog") { items = json.RootElement.GetProperty("items").GetArrayLength(); Check.True(json.RootElement.GetProperty("total").GetInt32() == 0, "Empty catalog total wrong."); }
                    if (path == "/api/catalog/bestsellers") items = json.RootElement.GetProperty("items").GetArrayLength();
                    if (path == "/api/lookups/brands") items = json.RootElement.GetArrayLength();
                    Check.True(items is null or 0, "Empty DB returned seeded public rows.");
                    responses.Add(new(path, (int)response.StatusCode, Items: items));
                }
                foreach (var path in new[] { "/api/admin/session", "/api/admin/orders" })
                {
                    using var response = await client.GetAsync(path); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    Check.True(response.StatusCode == HttpStatusCode.Unauthorized && json.RootElement.GetProperty("code").GetString() == "UNAUTHORIZED", "Unauthenticated admin smoke changed.");
                    responses.Add(new(path, 401, "UNAUTHORIZED"));
                }
                using (var response = await client.GetAsync("/api/security/token"))
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    Check.True(response.IsSuccessStatusCode && !string.IsNullOrEmpty(json.RootElement.GetProperty("token").GetString()), "CSRF token endpoint failed.");
                    // Neither token nor cookie is recorded.
                    responses.Add(new("/api/security/token", 200));
                }
                using var rejected = await client.PostAsJsonAsync("/api/auth/register", new { });
                using var rejectedJson = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
                Check.True(rejected.StatusCode == HttpStatusCode.BadRequest && rejectedJson.RootElement.GetProperty("code").GetString() == "INVALID_INPUT", "Missing CSRF accepted.");
                responses.Add(new("POST /api/auth/register without CSRF", 400, "INVALID_INPUT"));
            });
        });
        var finalCounts = await Counts();
        await Check.Run("Closeout read-only smoke leaves eighteen business tables empty and migration ledger unchanged", async () =>
        {
            Check.True(finalCounts.Values.All(value => value == 0) && (await Ledger()).Single() == installed, "Smoke created accounts/sessions/business data.");
        });
        var destination = Environment.GetEnvironmentVariable("PHONESTORE_BACKEND_SMOKE_REPORT");
        if (!string.IsNullOrWhiteSpace(destination))
            await File.WriteAllTextAsync(destination, JsonSerializer.Serialize(new {
                ActualMigration = installed, AppliedIds = new[] { DatabaseMigrator.MigrationId },
                LegacyUpgrade = "N/A: chỉ hỗ trợ dựng DB mới, không có đường nâng cấp từ DB cũ", Schema = schemaProof, Responses = responses,
                TableCountsAfter = finalCounts, Seeded = false, Provisioned = false,
                Scope = "Generated isolated QA database; no credentials/tokens/cookies exported. Test fixture disposes only its own database."
            }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
