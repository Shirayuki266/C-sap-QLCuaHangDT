using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Client.Checks;

internal static class OrderDesktopQaChecks
{
    public static async Task Run()
    {
        if(!OperatingSystem.IsWindows())throw new InvalidOperationException("Windows desktop required");await using var qa=await QaBackend.Create();
        using var admin=qa.Api();await admin.LoginAsync("admin-w1-qa",qa.Password);var catalog=new CatalogWorkspace(admin);var supply=new SupplyWorkspace(admin);var work=new OrdersWorkspace(admin);
        var brand=(await catalog.SaveLookup("brands",0,new("Hãng W5 UI QA",true))).Single().Id;var category=(await catalog.SaveLookup("categories",0,new("Điện thoại W5 UI QA",true))).Single().Id;var product=await catalog.Save(0,new("W5-UI","Điện thoại snapshot UI QA",brand,category,"QA","QA","",true,[new(0,"W5-UI-A","Đen",128,10000000,20,true,null)],[]));var variant=product.Variants.Single();await supply.Adjust(new(await supply.StockRow(variant.Id,variant.Sku),20,"QA initial desktop stock"));
        using var customer=qa.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new("https://localhost")});
        async Task<HttpResponseMessage> Post(string path,object body){var token=await customer.GetFromJsonAsync<TokenResult>("/api/security/token");using var request=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(body)};request.Headers.Add("X-CSRF-TOKEN",token!.Token);return await customer.SendAsync(request);}
        using(var response=await Post("/api/auth/login",new{username="customer-w1-qa",password=qa.Password,surface="customer"})){if(!response.IsSuccessStatusCode)throw new InvalidOperationException("QA customer login failed");}
        var ids=new List<long>();for(var i=0;i<3;i++){using(var add=await Post("/api/cart",new{variantId=variant.Id,quantity=2})){if(!add.IsSuccessStatusCode)throw new InvalidOperationException("QA cart failed");}using var created=await Post("/api/orders",new{requestKey=Guid.NewGuid(),recipient="Người nhận UI QA "+i,phone="0938123591",address="Địa chỉ snapshot UI QA",paymentMethod="COD",lines=new[]{new{variantId=variant.Id,quantity=2,price=12000000}}});if(created.StatusCode!=HttpStatusCode.Created)throw new InvalidOperationException("QA order failed");ids.Add((await created.Content.ReadFromJsonAsync<Created>())!.Id);}
        var dispatched=await work.Execute(new(await work.Read(ids[1]),OrderAction.Confirm));await work.Execute(new(dispatched,OrderAction.Dispatch));await admin.LogoutAsync();
        using var lease=new TcpListener(IPAddress.Loopback,0);lease.Start();var port=((IPEndPoint)lease.LocalEndpoint).Port;lease.Stop();var url=$"http://127.0.0.1:{port}";var config=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;var output=Path.Combine(QaBackend.Root,".artifacts","admin-w5");Directory.CreateDirectory(output);var signals=Path.Combine(output,"desktop-steps",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(signals);
        var hostInfo=new ProcessStartInfo("dotnet"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.Combine(QaBackend.Root,"src","PhoneStore.Web")};foreach(var arg in new[]{Path.Combine(QaBackend.Root,"src","PhoneStore.Web","bin",config,"net10.0","PhoneStore.Web.dll"),"--urls",url})hostInfo.ArgumentList.Add(arg);hostInfo.Environment["ConnectionStrings__Store"]=qa.Connection;hostInfo.Environment["ASPNETCORE_ENVIRONMENT"]="Testing";hostInfo.Environment["DOTNET_ENVIRONMENT"]="Testing";
        using var host=Process.Start(hostInfo)??throw new InvalidOperationException("QA host launch failed");var hostLog=host.StandardOutput.ReadToEndAsync();var hostError=host.StandardError.ReadToEndAsync();
        try
        {
            using var client=new HttpClient(new HttpClientHandler{UseProxy=false});var ready=false;for(var i=0;i<100;i++){try{using var response=await client.GetAsync(url+"/health/ready");if(response.IsSuccessStatusCode){ready=true;break;}}catch(HttpRequestException){}await Task.Delay(100);}if(!ready)throw new InvalidOperationException("QA host not ready");
            var ui=new ProcessStartInfo("dotnet"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=QaBackend.Root};ui.ArgumentList.Add(Path.Combine(QaBackend.Root,"tests","PhoneStore.Admin.Tests","bin",config,"net10.0-windows","PhoneStore.Admin.Tests.dll"));ui.ArgumentList.Add("--real-orders");ui.Environment["PHONESTORE_ADMIN_QA_URL"]=url;ui.Environment["PHONESTORE_ADMIN_QA_USERNAME"]="admin-w1-qa";ui.Environment["PHONESTORE_ADMIN_QA_PASSWORD"]=qa.Password;ui.Environment["PHONESTORE_ADMIN_QA_ORDER_IDS"]=string.Join(',',ids);ui.Environment["PHONESTORE_ADMIN_QA_SIGNAL_DIR"]=signals;ui.Environment["PHONESTORE_ADMIN_UI_EVIDENCE"]=Path.Combine(output,"real-forms");
            using var process=Process.Start(ui)??throw new InvalidOperationException("QA desktop launch failed");var result=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();var observation=Observe(qa,process,signals,output);
            try{await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(3));}finally{if(!process.HasExited){process.Kill(true);await process.WaitForExitAsync();}}
            Console.Write(await result);await observation;if(process.ExitCode!=0)throw new InvalidOperationException("Desktop W5 failed: "+await error);await using var db=qa.Open();if(await db.Sessions.AnyAsync(s=>s.AccountId==qa.AdminId))throw new InvalidOperationException("Desktop logout failed");Console.WriteLine("PASS W5 real WinForms/Kestrel/SQL: confirm/dispatch/complete COD, invoice/PrintPreview read-only, fail/receive/cancel;9 SQL checkpoints; logout revoked");
        }
        finally{if(!host.HasExited){host.Kill(true);await host.WaitForExitAsync();}}
        if((await hostLog).Contains(qa.Password)||(await hostError).Contains(qa.Password))throw new InvalidOperationException("QA host logged password");
    }
    private static async Task Observe(QaBackend qa,Process process,string signals,string output)
    {
        var records=new List<object>();string? completed=null;var index=0;
        foreach(var stage in new[]{"opened","confirmed","dispatched","completed","invoice-read","preview-closed","failed","returned","cancelled"})
        {
            var until=DateTime.UtcNow.AddSeconds(45);while(!File.Exists(Path.Combine(signals,stage+".ready"))){if(process.HasExited||DateTime.UtcNow>until)throw new InvalidOperationException("Missing W5 desktop step: "+stage);await Task.Delay(40);}
            await using var db=qa.Open();var data=new{orders=await db.Orders.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.OrderId).ToListAsync(),stock=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync(),journal=await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),history=await db.OrderHistory.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()};var json=JsonSerializer.Serialize(data);records.Add(new{stage,data});
            var expectedH=index<2?18:index<7?16:18;var expectedR=index<2?4:index<8?2:0;var first=index==0?"CHO_XAC_NHAN":index==1?"DA_XAC_NHAN":index==2?"DANG_GIAO":"HOAN_TAT";var second=index<6?"DANG_GIAO":"GIAO_THAT_BAI";
            if(data.stock.Single().OnHand!=expectedH||data.stock.Single().Reserved!=expectedR||data.orders.Count!=3||data.orders[0].Status!=first||data.orders[1].Status!=second||data.orders[2].Status!=(index==8?"DA_HUY":"CHO_XAC_NHAN")||data.payments.Sum(x=>x.Amount)!=(index<3?0:24000000)||data.payments[0].Status!=(index<3?"CHUA_THU":"DA_THU")||data.payments[0].CollectedAt!=data.orders[0].CompletedAt||data.journal.Count!=(index<2?5:index<7?6:index==7?7:8)||data.history.Count!=5+(index<=3?index:index<=5?3:index-2))throw new InvalidOperationException("W5 desktop SQL mismatch: "+stage);
            if(stage=="completed")completed=json;if(stage is "invoice-read" or "preview-closed"&&json!=completed)throw new InvalidOperationException("Invoice/preview mutated SQL");
            await File.WriteAllTextAsync(Path.Combine(output,"desktop-sql-results.json"),JsonSerializer.Serialize(new{qaOnly=true,records},new JsonSerializerOptions{WriteIndented=true}));await File.WriteAllTextAsync(Path.Combine(signals,stage+".continue"),"SQL verified");index++;
        }
    }
    private sealed record Created(long Id);
}
