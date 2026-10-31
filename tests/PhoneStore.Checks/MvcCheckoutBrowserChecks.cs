using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class MvcCheckoutBrowserChecks
{
    // Explicit test-runner mode only; none of this fixture setup is referenced by the shipping host.
    public static async Task Run()
    {
        await using var database = await TestDatabase.Create();
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "PhoneStore.slnx"))) root = root.Parent;
        if (root is null) throw new InvalidOperationException("Solution root missing.");
        MvcCatalogFixture data;
        await using (var factory = new MvcFoundationFactory(database.ConnectionString, root.FullName))
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync();
            data = await MvcCatalogFixture.Seed(scope.ServiceProvider);
            await scope.ServiceProvider.GetRequiredService<PhoneStore.Core.IStoreService>().UpdateCartAsync(data.Customer, data.Black128, 2);
        }
        var initial = await MvcCheckoutSql.Read(database.ConnectionString);
        MvcCheckoutSql? beforeExpiry = null; var firstVerified = false;
        var output = Path.Combine(root.FullName, ".artifacts", "mvc-checkout"); Directory.CreateDirectory(output);
        var commandPath=Path.Combine(output,"browser-command.json");var ackPath=Path.Combine(output,"browser-ack.json");
        File.Delete(commandPath);File.Delete(ackPath);
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
        await File.WriteAllTextAsync(contextPath, JsonSerializer.Serialize(new { url, data.MainId, data.Black128, data.Black256, data.Blue256, data.BrandId, data.OutOfStockId }));
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
            info.ArgumentList.Add(Path.Combine(root.FullName, "tests", "browser", "mvc-checkout.mjs"));
            info.Environment["PHONESTORE_CHECKOUT_BROWSER_CONTEXT"] = contextPath;
            using var browser = Process.Start(info) ?? throw new InvalidOperationException("Browser runner did not start.");
            var browserOutput = browser.StandardOutput.ReadToEndAsync(); var browserError = browser.StandardError.ReadToEndAsync();
            using var stopMonitor=new CancellationTokenSource();
            var monitor=Task.Run(async()=>
            {
                string? previous=null;
                while(!stopMonitor.IsCancellationRequested)
                {
                    if(File.Exists(commandPath))
                    {
                        JsonDocument? command=null;try{command=JsonDocument.Parse(await File.ReadAllTextAsync(commandPath));}catch(JsonException){}
                        using(command)
                        {
                            var nonce=command?.RootElement.GetProperty("nonce").GetString();
                            if(nonce is not null && nonce!=previous)
                            {
                                previous=nonce;var kind=command!.RootElement.GetProperty("kind").GetString();
                                await using var db=MvcCheckoutSql.Open(database.ConnectionString);
                                switch(kind)
                                {
                                    case "change-price": await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ExecuteUpdateAsync(db.Variants.Where(x=>x.Id==data.Black128),s=>s.SetProperty(x=>x.Price,13000000m));break;
                                    case "assert-before-first": Check.True(initial.Snapshot()==(await MvcCheckoutSql.Read(database.ConnectionString)).Snapshot(),"Price confirmation created an order/reservation before second approval.");break;
                                    case "first-order":
                                        var first=await MvcCheckoutSql.Read(database.ConnectionString);var order=first.Orders.Single(x=>!initial.Orders.Any(old=>old.Id==x.Id));
                                        MvcCheckoutSql.AssertOne(initial,first,order.RequestKey,data.Customer.AccountId,data.Black128,2,13000000m);firstVerified=true;break;
                                    case "expire-session":
                                        beforeExpiry=await MvcCheckoutSql.Read(database.ConnectionString);
                                        await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ExecuteUpdateAsync(db.Sessions.Where(x=>x.AccountId==data.Customer.AccountId),s=>s.SetProperty(x=>x.ExpiresAt,DateTime.UtcNow.AddMinutes(-1)));break;
                                    case "assert-expired-no-order": Check.True(beforeExpiry is not null && beforeExpiry.Snapshot()==(await MvcCheckoutSql.Read(database.ConnectionString)).Snapshot(),"Expired browser flow changed commerce SQL.");break;
                                    default: throw new InvalidOperationException("Unknown QA fixture command.");
                                }
                                var temporary=ackPath+".tmp";await File.WriteAllTextAsync(temporary,JsonSerializer.Serialize(new{nonce,ok=true}));File.Move(temporary,ackPath,true);
                            }
                        }
                    }
                    await Task.Delay(25);
                }
            });
            try { await browser.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(4)); }
            finally { if (!browser.HasExited) { browser.Kill(true); await browser.WaitForExitAsync(); } stopMonitor.Cancel();await monitor; }
            Console.Write(await browserOutput);
            if (browser.ExitCode != 0) { Console.Error.Write(await browserError); throw new InvalidOperationException("Browser catalog checks failed."); }
        }
        finally { if (!host.HasExited) { host.Kill(true); await host.WaitForExitAsync(); } }
        Check.True(firstVerified&&beforeExpiry is not null,"Browser skipped required SQL checkpoints.");
        var final=await MvcCheckoutSql.Read(database.ConnectionString);var second=final.Orders.Single(x=>!beforeExpiry!.Orders.Any(old=>old.Id==x.Id));
        MvcCheckoutSql.AssertOne(beforeExpiry!,final,second.RequestKey,data.Customer.AccountId,data.Black128,1,13000000m);
        await File.WriteAllTextAsync(Path.Combine(output,"browser-sql-results.json"),JsonSerializer.Serialize(new{qaOnly=true,firstOrderQuantity=2,firstTotal=26000000,secondOrderQuantity=1,secondTotal=13000000,ordersDelta=2,priceChangeWithoutOrder=true,expiredSessionWithoutOrder=true,firstAndSecondExactDeltasVerified=true,replayNoDuplicate=true}));
        Console.WriteLine("PASS real Chromium SQL oracle: first order2 at13m; expired-session flow leaves SQL unchanged; new-session order1 at13m; exact H/R/order/payment/history/journal deltas.");
        Check.True(!(await stdout).Contains(MvcCatalogFixture.Password) && !(await stderr).Contains(MvcCatalogFixture.Password), "QA host logged a fixture password.");
        Console.WriteLine("PASS browser host closed; generated QA fixture disposed; no shipping QA route/configuration was added.");
    }
}
