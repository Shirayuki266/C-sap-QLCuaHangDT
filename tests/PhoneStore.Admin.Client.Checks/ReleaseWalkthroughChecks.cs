using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Client.Checks;

internal static class ReleaseWalkthroughChecks
{
    public static async Task Run()
    {
        if(!OperatingSystem.IsWindows())throw new InvalidOperationException("Actual desktop walkthrough requires Windows");await using var qa=await QaBackend.Create();using(var admin=qa.Api()){await admin.LoginAsync("admin-w1-qa",qa.Password);var catalog=new CatalogWorkspace(admin);await catalog.SaveLookup("brands",0,new("Hãng walkthrough QA",true));await catalog.SaveLookup("categories",0,new("Điện thoại walkthrough QA",true));await new ContactsWorkspace(admin).SaveSupplier(0,new("W6-UI-SUP","NCC walkthrough QA","0938123593","QA isolated",true));await admin.LogoutAsync();}
        using var customer=qa.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new("https://localhost")});
        async Task<HttpResponseMessage> CustomerPost(string path,object body){var token=await customer.GetFromJsonAsync<TokenResult>("/api/security/token");using var request=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(body)};request.Headers.Add("X-CSRF-TOKEN",token!.Token);return await customer.SendAsync(request);}
        using(var login=await CustomerPost("/api/auth/login",new{username="customer-w1-qa",password=qa.Password,surface="customer"})){if(!login.IsSuccessStatusCode)throw new InvalidOperationException("QA customer setup failed");}
        using var lease=new TcpListener(IPAddress.Loopback,0);lease.Start();var port=((IPEndPoint)lease.LocalEndpoint).Port;lease.Stop();var url=$"http://127.0.0.1:{port}";var root=QaBackend.Root;var app=Path.Combine(root,"src","PhoneStore.Admin","bin","Release","net10.0-windows","PhoneStore.Admin.exe");if(!File.Exists(app))throw new InvalidOperationException("Build actual Release app first");var output=Path.Combine(root,".artifacts","admin-w6","release-walkthrough");Directory.CreateDirectory(output);var signals=Path.Combine(output,"signals",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(signals);
        var hostInfo=new ProcessStartInfo("dotnet"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.Combine(root,"src","PhoneStore.Web")};foreach(var arg in new[]{Path.Combine(root,"src","PhoneStore.Web","bin","Release","net10.0","PhoneStore.Web.dll"),"--urls",url})hostInfo.ArgumentList.Add(arg);hostInfo.Environment["ConnectionStrings__Store"]=qa.Connection;hostInfo.Environment["ASPNETCORE_ENVIRONMENT"]="Testing";hostInfo.Environment["DOTNET_ENVIRONMENT"]="Testing";
        var settingsPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PhoneStore.Admin","settings.json");var originalSettings=File.Exists(settingsPath)?await File.ReadAllBytesAsync(settingsPath):null;
        var stockSettingsPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PhoneStore.Admin","stock-settings.json");var originalStockSettings=File.Exists(stockSettingsPath)?await File.ReadAllBytesAsync(stockSettingsPath):null;
        using var host=Process.Start(hostInfo)??throw new InvalidOperationException("QA host launch failed");var hostLog=host.StandardOutput.ReadToEndAsync();var hostError=host.StandardError.ReadToEndAsync();
        try
        {
            using var http=new HttpClient(new HttpClientHandler{UseProxy=false});var ready=false;for(var i=0;i<100;i++){try{using var response=await http.GetAsync(url+"/health/ready");if(response.IsSuccessStatusCode){ready=true;break;}}catch(HttpRequestException){}await Task.Delay(100);}if(!ready)throw new InvalidOperationException("QA Kestrel not ready");
            var ui=new ProcessStartInfo("powershell"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=root};foreach(var arg in new[]{"-NoProfile","-ExecutionPolicy","Bypass","-File",Path.Combine(root,"tests","PhoneStore.Admin.Tests","Walkthrough.Release.ps1")})ui.ArgumentList.Add(arg);ui.Environment["PHONESTORE_WALK_APP"]=app;ui.Environment["PHONESTORE_WALK_SIGNALS"]=signals;ui.Environment["PHONESTORE_WALK_OUTPUT"]=output;ui.Environment["PHONESTORE_ADMIN_QA_URL"]=url;ui.Environment["PHONESTORE_ADMIN_QA_USERNAME"]="admin-w1-qa";ui.Environment["PHONESTORE_ADMIN_QA_PASSWORD"]=qa.Password;
            using var process=Process.Start(ui)??throw new InvalidOperationException("UI Automation launch failed");var result=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();var observation=Observe(qa,process,signals,output,CustomerPost);
            try{var exit=process.WaitForExitAsync();var first=await Task.WhenAny(exit,observation).WaitAsync(TimeSpan.FromMinutes(8));if(first==observation)await observation;await exit.WaitAsync(TimeSpan.FromMinutes(8));}finally{if(!process.HasExited){process.Kill(true);await process.WaitForExitAsync();}}
            var uiError=(await error).Replace(qa.Password,"[redacted]");await File.WriteAllTextAsync(Path.Combine(output,"ui-helper-error.txt"),uiError);Console.Write(await result);await observation;if(process.ExitCode!=0)throw new InvalidOperationException("Actual Release walkthrough failed; QA evidence retained, UI stderr withheld.");
            var manifest=new{label="agent-assisted UI walkthrough",actualReleaseApplication=true,formHarness=false,fakeApi=false,qaOnly=true,appSha256=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(app))),dllSha256=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.ChangeExtension(app,".dll")))),userWillRunManuallyAgain=true,customerSteps="Customer checkout uses the real API; primary Admin actions use actual Release UI",peerAdminSteps="One cancellation via real API by a second Admin account supplies the external concurrent change",uiScriptSha256=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(root,"tests","PhoneStore.Admin.Tests","Walkthrough.Release.ps1"))))};await File.WriteAllTextAsync(Path.Combine(output,"manifest.json"),JsonSerializer.Serialize(manifest,new JsonSerializerOptions{WriteIndented=true}));if((await error).Contains(qa.Password))throw new InvalidOperationException("UI helper exposed QA password");Console.WriteLine("PASS actual Release agent-assisted UI walkthrough FLOW01-05 with Back/Home, peer-admin conflict, stock count and expired-draft warning;22 SQL checkpoints; no form harness or fake API");
        }
        finally{if(!host.HasExited){host.Kill(true);await host.WaitForExitAsync();}if(File.Exists(settingsPath)&&JsonSerializer.Deserialize<ServerSettings>(await File.ReadAllTextAsync(settingsPath))?.BaseUrl==url){if(originalSettings is null)File.Delete(settingsPath);else await File.WriteAllBytesAsync(settingsPath,originalSettings);}if(originalStockSettings is null){if(File.Exists(stockSettingsPath))File.Delete(stockSettingsPath);}else await File.WriteAllBytesAsync(stockSettingsPath,originalStockSettings);}
        if((await hostLog).Contains(qa.Password)||(await hostError).Contains(qa.Password))throw new InvalidOperationException("QA host exposed password");
    }
    private static async Task Observe(QaBackend qa,Process process,string signals,string output,Func<string,object,Task<HttpResponseMessage>> customerPost)
    {
        var stages=new[]{"login","product-created","draft-saved","receipt-confirmed","customer-order1","order1-confirmed","order1-dispatched","order1-completed","revenue-seen","customer-order2","order2-confirmed","order2-dispatched","order2-failed","order2-returned","customer-order3","order3-confirmed","other-admin-cancelled","order3-cancelled","counted-with-navigation","low-stock-after-home","expire-before-save","expired-returned-login"};var records=new List<object>();object? previous=null;
        foreach(var stage in stages)
        {
            var until=DateTime.UtcNow.AddSeconds(75);while(!File.Exists(Path.Combine(signals,stage+".ready"))){if(process.HasExited||DateTime.UtcNow>until)throw new InvalidOperationException("Release UI checkpoint missing: "+stage);await Task.Delay(100);}
            await using var db=qa.Open();
            if(stage=="other-admin-cancelled")
            {
                var account=new Account{Username="navigation-peer-admin",Role="ADMIN",Active=true};
                account.PasswordHash=new PasswordHasher<Account>().HashPassword(account,qa.Password);db.Accounts.Add(account);await db.SaveChangesAsync();
                using var peer=qa.Api();await peer.LoginAsync(account.Username,qa.Password);
                var id=await db.Orders.OrderByDescending(o=>o.Id).Select(o=>o.Id).FirstAsync();var work=new OrdersWorkspace(peer);
                var row=await work.Read(id);await work.Execute(new OrderOperation(row,OrderAction.Cancel,"Quản trị viên QA khác hủy trong lúc màn hình đang mở"));await peer.LogoutAsync();
            }
            if(stage.StartsWith("customer-order"))
            {
                var v=await db.Variants.AsNoTracking().SingleAsync();using(var add=await customerPost("/api/cart",new{variantId=v.Id,quantity=2})){if(!add.IsSuccessStatusCode)throw new InvalidOperationException("Customer walkthrough cart failed");}using var created=await customerPost("/api/orders",new{requestKey=Guid.NewGuid(),recipient=stage=="customer-order1"?"Walkthrough FLOW01":stage=="customer-order2"?"Walkthrough FLOW02":"Walkthrough CANCEL",phone="0938123591",address="QA isolated walkthrough",paymentMethod="COD",lines=new[]{new{variantId=v.Id,quantity=2,price=v.Price}}});if(created.StatusCode!=HttpStatusCode.Created)throw new InvalidOperationException("Customer walkthrough checkout failed");
            }
            if(stage=="expire-before-save")await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.PHIEN SET ExpiresAt={DateTime.UtcNow.AddMinutes(-1)} WHERE AccountId={qa.AdminId}");
            var data=new{products=await db.Products.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),variants=await db.Variants.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),purchases=await db.Purchases.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),purchaseLines=await db.PurchaseLines.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),stock=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).Select(x=>new{x.VariantId,H=x.OnHand,R=x.Reserved,Available=x.OnHand-x.Reserved}).ToListAsync(),orders=await db.Orders.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.OrderId).ToListAsync(),history=await db.OrderHistory.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),journal=await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),activeAdminSessions=await db.Sessions.CountAsync(x=>x.AccountId==qa.AdminId&&x.ExpiresAt>DateTime.UtcNow)};
            var index=Array.IndexOf(stages,stage);var h=index<3?0:index<6?10:index<11?8:index<13?6:8;var reserved=index is 4 or 5 or 9 or 10 or 14 or 15?2:0;var expectedOrders=index<4?0:index<9?1:index<14?2:3;
            if(index>=Array.IndexOf(stages,"counted-with-navigation"))h=9;
            if(data.products.Count!=(index==0?0:1)||data.variants.Count!=(index==0?0:1)||data.purchases.Count!=(index<2?0:1)||data.orders.Count!=expectedOrders||data.stock.Sum(x=>x.H)!=h||data.stock.Sum(x=>x.R)!=reserved||data.stock.Any(x=>x.Available!=x.H-x.R)||data.payments.Sum(x=>x.Amount)!=(index<7?0:19200000))throw new InvalidOperationException("Release walkthrough SQL H/R/COD/count mismatch: "+stage);
            if(index>=1&&(data.variants.Single().Cost!=(index<3?10000000:8000000)||data.variants.Single().Price!=(index<3?12000000:9600000)))throw new InvalidOperationException("Release walkthrough server cost/price mismatch");
            if(expectedOrders>0){var status=index==4?"CHO_XAC_NHAN":index==5?"DA_XAC_NHAN":index==6?"DANG_GIAO":"HOAN_TAT";if(data.orders[0].Status!=status||data.payments[0].Status!=(index<7?"CHUA_THU":"DA_THU"))throw new InvalidOperationException("Release order1 state mismatch");}
            if(expectedOrders>1){var status=index==9?"CHO_XAC_NHAN":index==10?"DA_XAC_NHAN":index==11?"DANG_GIAO":"GIAO_THAT_BAI";if(data.orders[1].Status!=status||data.payments[1].Status!="CHUA_THU")throw new InvalidOperationException("Release order2 state mismatch");}
            if(expectedOrders>2&&data.orders[2].Status!=(index==14?"CHO_XAC_NHAN":index==15?"DA_XAC_NHAN":"DA_HUY"))throw new InvalidOperationException("Release order3 state mismatch");
            if(index>=Array.IndexOf(stages,"expire-before-save")&&data.purchases.Count!=1)throw new InvalidOperationException("Expired session created draft");if(stage=="expired-returned-login"&&data.activeAdminSessions!=0)throw new InvalidOperationException("Expired Admin stayed authenticated");
            records.Add(new{stage,before=previous,after=data});previous=data;await File.WriteAllTextAsync(Path.Combine(output,"sql-results.json"),JsonSerializer.Serialize(new{label="agent-assisted UI walkthrough",qaOnly=true,actualReleaseApplication=true,records},new JsonSerializerOptions{WriteIndented=true}));await File.WriteAllTextAsync(Path.Combine(signals,stage+".continue"),"SQL verified");
        }
    }
}
