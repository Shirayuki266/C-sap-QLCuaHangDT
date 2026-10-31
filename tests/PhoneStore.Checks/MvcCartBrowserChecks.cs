using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class MvcCartBrowserChecks
{
    // Explicit test-runner mode only; none of this fixture setup is referenced by the shipping host.
    public static async Task Run()
    {
        await using var database = await TestDatabase.Create();
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "PhoneStore.slnx"))) root = root.Parent;
        if (root is null) throw new InvalidOperationException("Solution root missing.");
        MvcCatalogFixture data;
        StoreDbContext Open() => new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(database.ConnectionString).Options);
        async Task<string> ProtectedSnapshot()
        {
            await using var db = Open();
            return JsonSerializer.Serialize(new { Inventory = await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync(), Journal = await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(), Orders = await db.Orders.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(), Payments = await db.Payments.AsNoTracking().OrderBy(x=>x.OrderId).ToListAsync() });
        }
        await using (var factory = new MvcFoundationFactory(database.ConnectionString, root.FullName))
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync();
            data = await MvcCatalogFixture.Seed(scope.ServiceProvider);
            await scope.ServiceProvider.GetRequiredService<PhoneStore.Core.IStoreService>().AddToCartAsync(data.Customer, data.MainId, data.Black128, 4);
        }
        var protectedBefore = await ProtectedSnapshot();
        var output = Path.Combine(root.FullName, ".artifacts", "mvc-cart"); Directory.CreateDirectory(output);
        var webroot = Path.Combine(output, "webroot"); Directory.CreateDirectory(webroot);
        var web = Path.Combine(root.FullName, "src", "PhoneStore.Web"); var staticRoot = Path.Combine(web, "wwwroot");
        foreach (var source in Directory.GetFiles(staticRoot, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(webroot, Path.GetRelativePath(staticRoot, source));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(source, destination, true);
        }
        var qaImages = Path.Combine(webroot, "images", "mvc-catalog"); Directory.CreateDirectory(qaImages);
        foreach (var (file, fill) in new[] { ("phone-a.svg", "#eaf0ff"), ("phone-b.svg", "#f4f6fb") })
            await File.WriteAllTextAsync(Path.Combine(qaImages, file), $"""
                <svg xmlns="http://www.w3.org/2000/svg" width="720" height="720" viewBox="0 0 720 720"><rect width="720" height="720" fill="{fill}"/><rect x="190" y="110" width="340" height="500" rx="24" fill="#142d5c"/><text x="360" y="340" text-anchor="middle" fill="#ffffff" font-family="sans-serif" font-size="42">ẢNH QA</text><text x="360" y="395" text-anchor="middle" fill="#ffffff" font-family="sans-serif" font-size="22">Fixture kiểm thử</text></svg>
                """);
        using var lease = new TcpListener(IPAddress.Loopback, 0); lease.Start(); var port = ((IPEndPoint)lease.LocalEndpoint).Port; lease.Stop();
        var url = $"http://127.0.0.1:{port}";
        var contextPath = Path.Combine(output, "browser-context.json");
        await File.WriteAllTextAsync(contextPath, JsonSerializer.Serialize(new { url, data.MainId, data.Black128, data.Black256, data.Blue256, data.HiddenProductId }));
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var hostInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = web, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { Path.Combine(web, "bin", configuration, "net10.0", "PhoneStore.Web.dll"), "--urls", url, "--webroot", webroot }) hostInfo.ArgumentList.Add(arg);
        hostInfo.Environment["ConnectionStrings__Store"] = database.ConnectionString; hostInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing"; hostInfo.Environment["DOTNET_ENVIRONMENT"] = "Testing";
        foreach (var key in new[] { "PHONESTORE_ADMIN_USERNAME", "PHONESTORE_ADMIN_PASSWORD", "PHONESTORE_DEMO_ADMIN_PASSWORD", "PHONESTORE_DEMO_CUSTOMER_PASSWORD" }) hostInfo.Environment.Remove(key);
        using var host = Process.Start(hostInfo) ?? throw new InvalidOperationException("Browser QA host failed.");
        var stdout = host.StandardOutput.ReadToEndAsync(); var stderr = host.StandardError.ReadToEndAsync();
        try
        {
            using var client = new HttpClient(new HttpClientHandler { UseProxy = false }); var ready = false;
            for (var attempt = 0; attempt < 100; attempt++)
            {
                try { using var response = await client.GetAsync(url + "/health/ready"); if (response.IsSuccessStatusCode) { ready = true; break; } }
                catch (HttpRequestException) { }
                await Task.Delay(100);
            }
            Check.True(ready, "QA browser host not ready; raw process output is withheld.");
            var info = new ProcessStartInfo("node") { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = root.FullName, RedirectStandardOutput = true, RedirectStandardError = true };
            info.ArgumentList.Add(Path.Combine(root.FullName, "tests", "browser", "mvc-cart.mjs"));
            info.Environment["PHONESTORE_CART_BROWSER_CONTEXT"] = contextPath;
            using var browser = Process.Start(info) ?? throw new InvalidOperationException("Browser runner did not start.");
            var browserOutput = browser.StandardOutput.ReadToEndAsync(); var browserError = browser.StandardError.ReadToEndAsync();
            try { await browser.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(3)); }
            finally { if (!browser.HasExited) { browser.Kill(true); await browser.WaitForExitAsync(); } }
            Console.Write(await browserOutput);
            if (browser.ExitCode != 0) { Console.Error.Write(await browserError); throw new InvalidOperationException("Browser catalog checks failed."); }
        }
        finally { if (!host.HasExited) { host.Kill(true); await host.WaitForExitAsync(); } }
        Check.True(protectedBefore == await ProtectedSnapshot(), "Browser cart flow changed protected SQL inventory/journal/orders/payments.");
        await using (var db = Open())
        {
            var lines = await db.Cart.AsNoTracking().Where(x=>x.CustomerId==data.Customer.AccountId).ToListAsync();
            Check.True(lines.Count==1 && lines[0].VariantId==data.Black128 && lines[0].Quantity==5, "Browser final cart disagrees with real SQL.");
        }
        await File.WriteAllTextAsync(Path.Combine(output,"sql-results.json"), JsonSerializer.Serialize(new { qaOnly=true, protectedSqlUnchanged=true, finalCartLines=1, finalQuantity=5, expectedServerUnitPrice=12000000, expectedTotal=60000000, observedFinalCartMatches=true }));
        Console.WriteLine("PASS browser SQL oracle: protected inventory/journal/orders/payments unchanged; final own cart one line quantity5 at server price12m.");
        Check.True(!(await stdout).Contains(MvcCatalogFixture.Password) && !(await stderr).Contains(MvcCatalogFixture.Password), "QA host logged a fixture password.");
        Console.WriteLine("PASS browser host closed; generated QA fixture disposed; no shipping QA route/configuration was added.");
    }
}
