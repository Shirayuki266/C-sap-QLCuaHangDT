using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Admin.Client.Checks;

internal static class DesktopQaChecks
{
    public static async Task Run(bool catalog=false,bool contacts=false,bool supply=false)
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("Explicit desktop smoke requires Windows.");
        await using var qa = await QaBackend.Create(); using var lease = new TcpListener(IPAddress.Loopback, 0); lease.Start(); var port = ((IPEndPoint)lease.LocalEndpoint).Port; lease.Stop();
        var url = $"http://127.0.0.1:{port}"; var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var hostInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.Combine(QaBackend.Root, "src", "PhoneStore.Web"), RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { Path.Combine(QaBackend.Root, "src", "PhoneStore.Web", "bin", configuration, "net10.0", "PhoneStore.Web.dll"), "--urls", url }) hostInfo.ArgumentList.Add(arg);
        hostInfo.Environment["ConnectionStrings__Store"] = qa.Connection; hostInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing"; hostInfo.Environment["DOTNET_ENVIRONMENT"] = "Testing";
        using var host = Process.Start(hostInfo) ?? throw new InvalidOperationException("QA host failed"); var logs = host.StandardOutput.ReadToEndAsync(); var errors = host.StandardError.ReadToEndAsync();
        try
        {
            using var client = new HttpClient(new HttpClientHandler { UseProxy = false }); var ready = false;
            for(var attempt=0;attempt<100;attempt++){try{using var response=await client.GetAsync(url+"/health/ready");if(response.IsSuccessStatusCode){ready=true;break;}}catch(HttpRequestException){}await Task.Delay(100);}
            if (!ready) throw new InvalidOperationException("Real QA host not ready; output withheld.");
            var ui = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = QaBackend.Root, RedirectStandardOutput = true, RedirectStandardError = true };
            ui.ArgumentList.Add(Path.Combine(QaBackend.Root,"tests","PhoneStore.Admin.Tests","bin",configuration,"net10.0-windows","PhoneStore.Admin.Tests.dll"));ui.ArgumentList.Add(supply?"--real-supply":contacts?"--real-contacts":catalog?"--real-catalog":"--real-api");
            ui.Environment["PHONESTORE_ADMIN_QA_URL"] = url; ui.Environment["PHONESTORE_ADMIN_QA_USERNAME"] = "admin-w1-qa"; ui.Environment["PHONESTORE_ADMIN_QA_PASSWORD"] = qa.Password;
            ui.Environment["PHONESTORE_ADMIN_UI_EVIDENCE"] = Path.Combine(QaBackend.Root,".artifacts",supply?"admin-w4":contacts?"admin-w3":catalog?"admin-w2":"admin-w1","real-forms");
            var signals=Path.Combine(QaBackend.Root,".artifacts","admin-w4","desktop-steps",Guid.NewGuid().ToString("N"));
            if(supply){Directory.CreateDirectory(signals);ui.Environment["PHONESTORE_ADMIN_QA_SIGNAL_DIR"]=signals;}
            using var process = Process.Start(ui) ?? throw new InvalidOperationException("QA WinForms runner failed"); var result = process.StandardOutput.ReadToEndAsync(); var failure = process.StandardError.ReadToEndAsync();
            var observation=supply?ObserveSupply(qa,process,signals):Task.CompletedTask;
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(2)); }
            finally { if(!process.HasExited){process.Kill(true);await process.WaitForExitAsync();} }
            Console.Write(await result);await observation; if(process.ExitCode!=0)throw new InvalidOperationException("QA WinForms smoke failed: "+await failure);
            await using var db=qa.Open();if(await db.Sessions.AnyAsync(x=>x.AccountId==qa.AdminId)||await db.Orders.AnyAsync()||(!catalog&&!supply&&await db.Inventory.AnyAsync()))throw new InvalidOperationException("Desktop smoke left unexpected session/commerce mutation");
            if(catalog)
            {
                var data=new{qaOnly=true,products=await db.Products.CountAsync(),variants=await db.Variants.CountAsync(),H=await db.Inventory.SumAsync(x=>x.OnHand),R=await db.Inventory.SumAsync(x=>x.Reserved),prices=await db.Variants.Select(x=>x.Price).ToListAsync(),orders=await db.Orders.CountAsync(),journal=await db.Movements.CountAsync(),payments=await db.Payments.CountAsync()};
                if(data.products!=1||data.variants!=1||data.H!=0||data.R!=0||data.prices.Single()!=12000000||data.journal!=0||data.payments!=0)throw new InvalidOperationException("Desktop catalog SQL result wrong");
                await File.WriteAllTextAsync(Path.Combine(QaBackend.Root,".artifacts","admin-w2","desktop-sql-results.json"),System.Text.Json.JsonSerializer.Serialize(data,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
            }
            if(contacts)
            {
                var data=new{qaOnly=true,customers=await db.Customers.CountAsync(),customerName=await db.Customers.Select(c=>c.FullName).SingleAsync(),suppliers=await db.Suppliers.CountAsync(),activeSuppliers=await db.Suppliers.CountAsync(s=>s.Active),orders=await db.Orders.CountAsync(),inventoryRows=await db.Inventory.CountAsync(),payments=await db.Payments.CountAsync(),journal=await db.Movements.CountAsync(),adminSessions=await db.Sessions.CountAsync(s=>s.AccountId==qa.AdminId)};
                if(data.customerName!="Khách QA W3 · API thực"||data.suppliers!=1||data.activeSuppliers!=1||data.payments!=0||data.journal!=0)throw new InvalidOperationException("Real W3 UI SQL result wrong");
                await File.WriteAllTextAsync(Path.Combine(QaBackend.Root,".artifacts","admin-w3","desktop-sql-results.json"),System.Text.Json.JsonSerializer.Serialize(data,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
            }
            Console.WriteLine(supply?"PASS real W4 WinForms/Kestrel/SQL QA: product hints4 -> unsaved receipt0 -> saved draft H0 -> confirmed H4 -> counted H5; journal2; logout revoked":contacts?"PASS real W3 WinForms/Kestrel/SQL QA: profile saved, supplier created/listed/selected; orders/inventory unchanged; admin session removed":catalog?"PASS real W2 WinForms/Kestrel/SQL QA: editor created product, server price12000000, stock H0/R0; list displayed; session removed":"PASS real WinForms/Kestrel/SQL QA: login session created then logout removed; orders/inventory unchanged");
        }
        finally { if(!host.HasExited){host.Kill(true);await host.WaitForExitAsync();} }
        if((await logs).Contains(qa.Password)||(await errors).Contains(qa.Password))throw new InvalidOperationException("QA host exposed a credential");
    }
    private static async Task ObserveSupply(QaBackend qa,Process process,string signals)
    {
        var records=new List<object>();
        foreach(var stage in new[]{"product-saved","handoff-open","draft-saved","confirmed","counted"})
        {
            var ready=Path.Combine(signals,stage+".ready");var deadline=DateTime.UtcNow.AddSeconds(40);
            while(!File.Exists(ready)){if(process.HasExited||DateTime.UtcNow>deadline)throw new InvalidOperationException("QA desktop step missing: "+stage);await Task.Delay(40);}
            await using var db=qa.Open();var snapshot=new{stage,purchases=await db.Purchases.CountAsync(),status=await db.Purchases.Select(x=>x.Status).SingleOrDefaultAsync(),quantity=await db.PurchaseLines.SumAsync(x=>(int?)x.Quantity)??0,H=await db.Inventory.SumAsync(x=>(int?)x.OnHand)??0,R=await db.Inventory.SumAsync(x=>(int?)x.Reserved)??0,journal=await db.Movements.CountAsync(),variants=await db.Variants.Select(x=>new{x.Cost,x.Price}).ToListAsync(),orders=await db.Orders.CountAsync(),payments=await db.Payments.CountAsync()};records.Add(snapshot);
            await File.WriteAllTextAsync(Path.Combine(QaBackend.Root,".artifacts","admin-w4","desktop-sql-results.json"),System.Text.Json.JsonSerializer.Serialize(new{qaOnly=true,records},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
            var expectedH=stage=="confirmed"?4:stage=="counted"?5:0;var saved=stage is "draft-saved" or "confirmed" or "counted";
            if(snapshot.H!=expectedH||snapshot.R!=0||snapshot.purchases!=(saved?1:0)||snapshot.quantity!=(saved?4:0)||snapshot.orders!=0||snapshot.payments!=0||snapshot.variants.Single() is not{Cost:10000000,Price:12000000}||snapshot.journal!=(stage=="confirmed"?1:stage=="counted"?2:0)||saved&&snapshot.status!=(stage=="draft-saved"?"NHAP":"DA_XAC_NHAN"))throw new InvalidOperationException("QA desktop SQL mismatch at "+stage);
            await File.WriteAllTextAsync(Path.Combine(signals,stage+".continue"),"SQL verified");
        }
    }
}
