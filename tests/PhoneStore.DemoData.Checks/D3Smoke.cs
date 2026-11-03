using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.DemoData.Cli;
using PhoneStore.DemoData.Persistence;
using PhoneStore.DemoData.Verification;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.DemoData.Checks;

// Explicit local D3 mode. No runtime endpoint, fixture injection, mock form or default credentials.
internal static class D3Smoke
{
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public static async Task<int> Run()
    {
        try { await Execute(); return 0; }
        catch (Exception error) { Console.Error.WriteLine("D3 smoke failed: " + error.GetType().Name + ". See sanitized stage evidence."); return 1; }
    }

    private static async Task Execute()
    {
        Require(OperatingSystem.IsWindows(), "Interactive Windows is required.");
        var connection = Options.GuardConnection(Environment.GetEnvironmentVariable("ConnectionStrings__Store"));
        var target = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
        Require(target.InitialCatalog.StartsWith("PhoneStore_Demo_D3_", StringComparison.Ordinal), "Dedicated D3 target required.");
        var secrets = Secrets.Read(Environment.GetEnvironmentVariable);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PhoneStore.slnx"))) root = root.Parent;
        Require(root is not null, "Root missing.");
        var repo = root!.FullName;
        var output = Path.GetFullPath(Environment.GetEnvironmentVariable("PHONESTORE_DEMO_SMOKE_OUTPUT") ?? Path.Combine(repo, ".artifacts", "demo-data", "d3"));
        Directory.CreateDirectory(output);
        var store = new SeedStore(connection);
        await using var db = store.Open();
        Require(await new DatabaseMigrator(db).IsCurrentAsync() && await db.Orders.CountAsync() == 601 && !await db.Sessions.AnyAsync(), "Fresh full demo with zero sessions required.");
        var baseline = BusinessManifest.Hash(await BusinessManifest.Read(db));
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var utcToday = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
        var periodStart = utcToday.AddDays(-29); var weekStart = utcToday.AddDays(-6); var end = utcToday.AddDays(1);
        var products = await db.Products.AsNoTracking().Where(x => x.Active).OrderBy(x => x.Id)
            .Select(x => new { id=x.Id, name=x.Name, description=x.Description, specifications=x.Specifications, imageUrl=x.ImageUrl }).ToArrayAsync();
        var customer = await db.Accounts.AsNoTracking().SingleAsync(x => x.Username == "demo.customer.01");
        var customerOrders = await db.Orders.CountAsync(x => x.CustomerId == customer.Id);
        var ownedOrders = await db.Orders.AsNoTracking().Where(x => x.CustomerId == customer.Id).Select(x => new { id=x.Id,code=x.Code }).ToArrayAsync();
        var completed = await db.Orders.AsNoTracking().Where(x => x.Status == "HOAN_TAT").OrderBy(x => x.Id).FirstAsync();
        var expectedQuarter = new List<object>();
        var localStart = new DateTime(today.Year, (today.Month - 1) / 3 * 3 + 1, 1).AddMonths(-24);
        for (var q = 0; q < 9; q++)
        {
            var from = DateOnly.FromDateTime(localStart.AddMonths(q * 3));
            var to = q == 8 ? today : DateOnly.FromDateTime(localStart.AddMonths((q + 1) * 3).AddDays(-1));
            var startUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
            var endUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
            var revenue = await db.Orders.Where(x => x.Status == "HOAN_TAT" && x.CompletedAt >= startUtc && x.CompletedAt < endUtc).SumAsync(x => x.Total);
            Require(revenue > 0, "Quarter revenue must be nonzero.");
            expectedQuarter.Add(new { from=from.ToString("yyyy-MM-dd"), to=to.ToString("yyyy-MM-dd"), revenue });
        }
        var pending = await db.Orders.CountAsync(x => x.Status == "CHO_XAC_NHAN");
        var shipping = await db.Orders.CountAsync(x => x.Status == "DANG_GIAO");
        var returns = await db.Orders.CountAsync(x => x.Status == "GIAO_THAT_BAI" && !db.Movements.Any(m => m.OrderId == x.Id && m.Kind == "NHAN_LAI"));
        var periodRevenue = await db.Orders.Where(x => x.Status == "HOAN_TAT" && x.CompletedAt >= periodStart && x.CompletedAt < end).SumAsync(x => x.Total);
        var weekRevenue = await db.Orders.Where(x => x.Status == "HOAN_TAT" && x.CompletedAt >= weekStart && x.CompletedAt < end).SumAsync(x => x.Total);
        var weekOrders = await db.Orders.CountAsync(x => x.Status == "HOAN_TAT" && x.CompletedAt >= weekStart && x.CompletedAt < end);
        var todayRevenue = await db.Orders.Where(x => x.Status == "HOAN_TAT" && x.CompletedAt >= utcToday && x.CompletedAt < end).SumAsync(x => x.Total);
        var low = await db.Inventory.CountAsync(x => x.OnHand - x.Reserved <= 5);
        var outs = await db.Inventory.CountAsync(x => x.OnHand == 0);
        var photos = await db.ProductImages.Select(x => x.Url).Distinct().ToArrayAsync();
        using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var url = $"http://127.0.0.1:{port}";
        var context = new { url, database=target.InitialCatalog, today=today.ToString("yyyy-MM-dd"), products, customerUsername=customer.Username, customerOrders, ownedOrders,
            completedOrder=new { id=completed.Id, code=completed.Code, total=completed.Total }, quarters=expectedQuarter,
            expected=new { activeProducts=products.Length, orders=601, stockVariants=125, pending, shipping, returns, periodRevenue, weekRevenue, weekOrders, todayRevenue, lowThreshold=5, lowCount=low, outCount=outs },
            thresholds=new { httpMilliseconds=2000, browserMilliseconds=5000, nativeMilliseconds=5000 } };
        var contextPath = Path.Combine(output, "context.json"); await Save(contextPath, context);
        var web = Path.Combine(repo, "src", "PhoneStore.Web");
        var webDll = Path.Combine(web, "bin", "Release", "net10.0", "PhoneStore.Web.dll");
        ProcessStartInfo WebInfo(params string[] args)
        {
            var info = new ProcessStartInfo("dotnet") { WorkingDirectory=web, UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true };
            info.ArgumentList.Add(webDll); foreach (var argument in args) info.ArgumentList.Add(argument);
            info.Environment["ConnectionStrings__Store"]=connection; info.Environment["ASPNETCORE_ENVIRONMENT"]="Testing"; info.Environment["DOTNET_ENVIRONMENT"]="Testing";
            info.Environment["Logging__LogLevel__Default"]="Warning";
            info.Environment.Remove("PHONESTORE_DEMO_ADMIN_PASSWORD"); info.Environment.Remove("PHONESTORE_DEMO_CUSTOMER_PASSWORD");
            return info;
        }
        async Task Audit(string name)
        {
            using var audit = Process.Start(WebInfo("--audit-db")) ?? throw new InvalidOperationException("Audit launch failed.");
            var text = audit.StandardOutput.ReadToEndAsync(); var error = audit.StandardError.ReadToEndAsync();
            await audit.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
            var stdout = await text; var stderr = await error; CheckSecrets(stdout + stderr, secrets);
            Require(audit.ExitCode == 0, "Existing audit CLI failed.");
            using var parsed = JsonDocument.Parse(stdout[stdout.IndexOf('{')..(stdout.LastIndexOf('}')+1)]);
            Require(parsed.RootElement.GetProperty("Valid").GetBoolean(), "Audit invalid.");
            await Save(Path.Combine(output, name+".json"), parsed.RootElement.Clone());
            Console.WriteLine("PASS D3 existing --audit-db " + name);
        }
        await Audit("audit-before");
        using var host = Process.Start(WebInfo("--urls", url)) ?? throw new InvalidOperationException("Host launch failed.");
        var hostOutput = host.StandardOutput.ReadToEndAsync(); var hostError = host.StandardError.ReadToEndAsync();
        try
        {
            using var jar = new HttpClientHandler { CookieContainer = new CookieContainer(), UseProxy = false };
            using var http = new HttpClient(jar) { BaseAddress=new(url), Timeout=TimeSpan.FromSeconds(20) };
            var ready = false;
            for (var i = 0; i < 100; i++) { try { using var response=await http.GetAsync("/health/ready"); ready=response.IsSuccessStatusCode; } catch(HttpRequestException) { } if(ready)break; await Task.Delay(100); }
            Require(ready, "Real host readiness timeout.");
            async Task Post(string path, object value)
            {
                using var token = await http.GetFromJsonAsync<JsonDocument>("/api/security/token");
                using var request = new HttpRequestMessage(HttpMethod.Post,path) { Content=JsonContent.Create(value) };
                request.Headers.Add("X-CSRF-TOKEN",token!.RootElement.GetProperty("token").GetString());
                using var response=await http.SendAsync(request); Require(response.IsSuccessStatusCode, "Authenticated POST failed: " + path);
            }
            var measurements = new List<object>();
            async Task<JsonElement> Get(string path)
            {
                var timer=Stopwatch.StartNew(); using var response=await http.GetAsync(path); Require(response.IsSuccessStatusCode,"HTTP read failed: "+path);
                using var data=JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var elapsed=timer.Elapsed.TotalMilliseconds;
                measurements.Add(new { path, milliseconds=elapsed }); Require(elapsed<2000,"HTTP measurement exceeds 2000 ms."); return data.RootElement.Clone();
            }
            await Post("/api/auth/login",new { username="demo.admin.large", password=secrets.Admin, surface="admin" });
            var page1=await Get("/api/admin/orders?page=1&pageSize=20"); var page2=await Get("/api/admin/orders?page=2&pageSize=20");
            Require(page1.GetProperty("total").GetInt32()==601 && page2.GetProperty("total").GetInt32()==601 && page1.GetProperty("items").GetArrayLength()==20 && page2.GetProperty("items").GetArrayLength()==20,"Order paging mismatch.");
            Require(!page1.GetProperty("items").EnumerateArray().Select(x=>x.GetProperty("id").GetInt64()).Intersect(page2.GetProperty("items").EnumerateArray().Select(x=>x.GetProperty("id").GetInt64())).Any(),"Pages overlap.");
            using var expectedData=JsonDocument.Parse(JsonSerializer.Serialize(context));
            var revenues=new List<decimal>();
            foreach(var quarter in expectedData.RootElement.GetProperty("quarters").EnumerateArray())
            {
                var report=await Get($"/api/admin/reports?kind=revenue&from={quarter.GetProperty("from").GetString()}&to={quarter.GetProperty("to").GetString()}");
                var revenue=report.GetProperty("revenue").GetDecimal(); Require(revenue==quarter.GetProperty("revenue").GetDecimal() && revenue>0,"Quarter HTTP/SQL revenue mismatch."); revenues.Add(revenue);
            }
            Require(revenues.Distinct().Count()==9,"Quarter revenues must differ.");
            var best=await Get($"/api/admin/reports?kind=bestsellers&from={today.AddDays(1-today.Day):yyyy-MM-dd}&to={today:yyyy-MM-dd}");
            Require(best.GetProperty("bestsellers").GetArrayLength()>=8,"Admin bestseller report empty.");
            var stock=await Get("/api/admin/reports?kind=stock"); Require(stock.GetProperty("stock").GetArrayLength()==125,"Stock report count mismatch.");
            var lowStock=await Get("/api/admin/stock?below=5&page=1&pageSize=100"); Require(lowStock.GetProperty("total").GetInt32()==low && outs>=3,"Low/out stock mismatch.");
            var invoice=await Get($"/api/admin/orders/{completed.Id}/invoice"); Require(invoice.GetProperty("order").GetProperty("status").GetString()=="HOAN_TAT" && invoice.GetProperty("order").GetProperty("total").GetDecimal()==completed.Total,"Invoice mismatch.");
            for(var sample=0;sample<5;sample++){await Get("/api/admin/orders?page=1&pageSize=20");await Get($"/api/admin/reports?kind=revenue&from={today.AddMonths(-24):yyyy-MM-dd}&to={today:yyyy-MM-dd}");}
            foreach(var photo in photos){using var response=await http.GetAsync(photo);Require(response.IsSuccessStatusCode,"Demo image HTTP failure.");}
            await Post("/api/auth/logout",new { });
            await Save(Path.Combine(output,"http.json"),new { realHost=true,authenticatedAdmin=true,orders=601,pages=2,quarters=expectedQuarter,stockVariants=125,lowStock=low,outOfStock=outs,invoiceOrder=completed.Code,imagesHttp200=photos.Length,measurements,thresholdMilliseconds=2000 });
            Console.WriteLine("PASS D3 real ADMIN HTTP paging, nine SQL-matched revenues, bestsellers, stock, invoice and timings");
            async Task Child(ProcessStartInfo info, string label)
            {
                using var child=Process.Start(info)??throw new InvalidOperationException("Child launch failed.");
                var stdout=child.StandardOutput.ReadToEndAsync();var stderr=child.StandardError.ReadToEndAsync();
                try { await child.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(5)); }
                finally { if(!child.HasExited){child.Kill(true);await child.WaitForExitAsync();} }
                var text=await stdout;var error=await stderr;CheckSecrets(text+error,secrets);
                await File.WriteAllTextAsync(Path.Combine(output,label+"-trace.txt"),text+error);
                Require(child.ExitCode==0,"Smoke child failed: "+label);Console.WriteLine("PASS D3 "+label);
            }
            var browser=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=repo};
            browser.ArgumentList.Add(Path.Combine(repo,"tests","PhoneStore.DemoData.Checks","D3.Browser.cjs"));
            browser.Environment["PHONESTORE_DEMO_CONTEXT"]=contextPath;browser.Environment["PHONESTORE_DEMO_CUSTOMER_PASSWORD"]=secrets.Customer;
            await Child(browser,"browser");
            var native=new ProcessStartInfo("powershell"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=repo};
            foreach(var arg in new[]{"-NoProfile","-ExecutionPolicy","Bypass","-File",Path.Combine(repo,"tests","PhoneStore.DemoData.Checks","D3.WinForms.ps1")})native.ArgumentList.Add(arg);
            native.Environment["PHONESTORE_DEMO_CONTEXT"]=contextPath;native.Environment["PHONESTORE_WALK_APP"]=Path.Combine(repo,"src","PhoneStore.Admin","bin","Release","net10.0-windows","PhoneStore.Admin.exe");
            native.Environment["PHONESTORE_WALK_OUTPUT"]=Path.Combine(output,"winforms");native.Environment["PHONESTORE_WALK_SIGNALS"]=output;
            native.Environment["PHONESTORE_ADMIN_QA_URL"]=url;native.Environment["PHONESTORE_ADMIN_QA_USERNAME"]="demo.admin.large";native.Environment["PHONESTORE_ADMIN_QA_PASSWORD"]=secrets.Admin;
            var settingsDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PhoneStore.Admin");
            var settingsFiles=new[]{"settings.json","stock-settings.json"}.Select(name=>Path.Combine(settingsDirectory,name)).ToArray();
            var originalSettings=new Dictionary<string,byte[]?>();
            foreach(var file in settingsFiles) originalSettings[file]=File.Exists(file)?await File.ReadAllBytesAsync(file):null;
            try { await Child(native,"winforms"); }
            finally
            {
                // Parent backup also survives a timed-out helper that cannot run its own finally.
                foreach(var(file,bytes)in originalSettings)
                    if(bytes is null){if(File.Exists(file))File.Delete(file);}else{Directory.CreateDirectory(settingsDirectory);await File.WriteAllBytesAsync(file,bytes);}
            }
            Require(!await db.Sessions.AnyAsync(),"Smoke left authenticated sessions.");
            Require(baseline==BusinessManifest.Hash(await BusinessManifest.Read(db)),"Application smoke changed business data.");
            await Audit("audit-after");
            var app=Path.Combine(repo,"src","PhoneStore.Admin","bin","Release","net10.0-windows","PhoneStore.Admin.exe");
            await Save(Path.Combine(output,"smoke.json"),new { result="PASS",actualFullDemo=true,realKestrel=true,actualReleaseWinForms=true,fakeApi=false,agentAssisted=true,baselineBusinessHash=baseline,afterBusinessHash=baseline,zeroSessions=true,
                webDllSha256=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(webDll))),adminExeSha256=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(app))),adminDllSha256=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.ChangeExtension(app,".dll")))) });
        }
        finally
        {
            if(!host.HasExited){host.Kill(true);await host.WaitForExitAsync();}
            CheckSecrets(await hostOutput+await hostError,secrets);
            // Recovery of our own full-demo fixture only; successful runs must already have zero sessions.
            await db.Sessions.ExecuteDeleteAsync();
        }
        Console.WriteLine("PASS D3 full real application smoke, screenshots, SQL agreement and business-data preservation");
    }
    private static void CheckSecrets(string text, Secrets secrets) { Require(!text.Contains(secrets.Admin,StringComparison.Ordinal)&&!text.Contains(secrets.Customer,StringComparison.Ordinal),"Process exposed a secret; raw output withheld."); }
    private static Task Save(string path,object value)=>File.WriteAllTextAsync(path,JsonSerializer.Serialize(value,Json));
}
