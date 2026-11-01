using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Admin.Client.Checks;

internal static class DesktopQaChecks
{
    public static async Task Run()
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
            ui.ArgumentList.Add(Path.Combine(QaBackend.Root,"tests","PhoneStore.Admin.Tests","bin",configuration,"net10.0-windows","PhoneStore.Admin.Tests.dll"));ui.ArgumentList.Add("--real-api");
            ui.Environment["PHONESTORE_ADMIN_QA_URL"] = url; ui.Environment["PHONESTORE_ADMIN_QA_USERNAME"] = "admin-w1-qa"; ui.Environment["PHONESTORE_ADMIN_QA_PASSWORD"] = qa.Password;
            ui.Environment["PHONESTORE_ADMIN_UI_EVIDENCE"] = Path.Combine(QaBackend.Root,".artifacts","admin-w1","real-forms");
            using var process = Process.Start(ui) ?? throw new InvalidOperationException("QA WinForms runner failed"); var result = process.StandardOutput.ReadToEndAsync(); var failure = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(2)); }
            finally { if(!process.HasExited){process.Kill(true);await process.WaitForExitAsync();} }
            Console.Write(await result); if(process.ExitCode!=0)throw new InvalidOperationException("QA WinForms smoke failed: "+await failure);
            await using var db=qa.Open();if(await db.Sessions.AnyAsync(x=>x.AccountId==qa.AdminId)||await db.Orders.AnyAsync()||await db.Inventory.AnyAsync())throw new InvalidOperationException("Desktop smoke left session/commerce mutation");
            Console.WriteLine("PASS real WinForms/Kestrel/SQL QA: login session created then logout removed; orders/inventory unchanged");
        }
        finally { if(!host.HasExited){host.Kill(true);await host.WaitForExitAsync();} }
        if((await logs).Contains(qa.Password)||(await errors).Contains(qa.Password))throw new InvalidOperationException("QA host exposed a credential");
    }
}
