using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace PhoneStore.Checks;

internal static class HttpChecks
{
    public static async Task Run(string? migratedConnection = null)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PhoneStore.slnx"))) root = root.Parent;
        if (root is null) throw new InvalidOperationException("Solution directory was not found.");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(root.FullName, "src", "PhoneStore.Web", "bin", configuration, "net10.0", "PhoneStore.Web.dll");
        using var portLease = new TcpListener(IPAddress.Loopback, 0);
        portLease.Start();
        var port = ((IPEndPoint)portLease.LocalEndpoint).Port;
        portLease.Stop();
        var url = $"http://127.0.0.1:{port}";
        var target = new SqlConnectionStringBuilder(migratedConnection ?? Environment.GetEnvironmentVariable("PHONESTORE_TEST_CONNECTION") ??
            "Server=localhost;Integrated Security=True;Encrypt=True;TrustServerCertificate=True")
        {
            ConnectTimeout = 2
        };
        if (migratedConnection is null) target.InitialCatalog = "PhoneStore_QA_Unmigrated_" + Guid.NewGuid().ToString("N");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = root.FullName };
        start.ArgumentList.Add(dll);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add(url);
        start.Environment["ConnectionStrings__Store"] = target.ConnectionString;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        start.Environment["Logging__LogLevel__Default"] = "None";
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Backend did not start.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var client = new HttpClient(new HttpClientHandler { UseProxy = false }) { BaseAddress = new(url), Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            var ready = false;
            for (var attempt = 0; attempt < 100 && !process.HasExited; attempt++)
            {
                try { using var response = await client.GetAsync("/health/live"); ready = response.IsSuccessStatusCode; }
                catch (HttpRequestException) { }
                if (ready) break;
                await Task.Delay(100);
            }
            Check.True(ready, "Backend liveness did not become available within 10 seconds.");
            await Check.Run("HTTP liveness works without a migrated database", async () =>
            {
                using var response = await client.GetAsync("/health/live");
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Check.True(response.IsSuccessStatusCode && json.RootElement.GetProperty("application").GetString() == "PhoneStore", "Liveness contract failed.");
            });
            await Check.Run(migratedConnection is null ? "HTTP readiness fails closed before schema installation" : "HTTP readiness recognizes the migrated schema", async () =>
            {
                using var response = await client.GetAsync("/health/ready");
                var body = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(body);
                if (migratedConnection is not null)
                {
                    Check.True(response.IsSuccessStatusCode && json.RootElement.GetProperty("status").GetString() == "ready", "Migrated schema was not reported ready.");
                    return;
                }
                Check.True(response.StatusCode == HttpStatusCode.ServiceUnavailable && json.RootElement.GetProperty("code").GetString() == "DATABASE_UNAVAILABLE", "Unmigrated database was reported ready.");
                Check.True(!body.Contains(target.InitialCatalog) && !body.Contains("Password"), "Readiness exposed configuration.");
            });
            await Check.Run("HTTP missing routes and methods return structured errors", async () =>
            {
                using var missing = await client.GetAsync("/api/not-implemented");
                using var missingJson = JsonDocument.Parse(await missing.Content.ReadAsStringAsync());
                Check.True(missing.StatusCode == HttpStatusCode.NotFound && missingJson.RootElement.GetProperty("code").GetString() == "NOT_FOUND", "Missing route error failed.");
                using var wrongMethod = await client.PostAsync("/health/live", null);
                Check.True(wrongMethod.StatusCode == HttpStatusCode.MethodNotAllowed, "Wrong method was accepted.");
            });
            if (migratedConnection is not null) await RegistrationHttpChecks.Run(client, migratedConnection);
            if (migratedConnection is not null) await SessionHttpChecks.Run(new Uri(url), migratedConnection);
            if (migratedConnection is not null) await LogoutChecks.Run(new Uri(url), migratedConnection);
            if (migratedConnection is not null) await PasswordChecks.Run(new Uri(url), migratedConnection);
            if (migratedConnection is not null) await ProfileChecks.Run(new Uri(url), migratedConnection);
            if (migratedConnection is not null) await BusinessKernelChecks.Run(new Uri(url), migratedConnection);
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
    }
}
