using System.Diagnostics;
using System.Text.Json;
using PhoneStore.Core.Contracts;
internal sealed partial class QaSuite
{
    long xssProductId;
    async Task Child(string executable,IEnumerable<string> args,string name){
        var info=new ProcessStartInfo(executable){WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in args)info.ArgumentList.Add(arg);info.Environment["PHONESTORE_API_URL"]=url;info.Environment["PHONESTORE_ADMIN_PASSWORD"]=adminPassword;info.Environment["PHONESTORE_CUSTOMER_PASSWORD"]=customerPassword;info.Environment["PHONESTORE_QA_MODE"]="1";info.Environment["PHONESTORE_XSS_ID"]=xssProductId.ToString();
        using var process=Process.Start(info)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(100));
        // Capture only PASS/FAIL labels, never browser evaluation strings or credential-bearing request data.
        var safe=(await stdout).Split('\n').Where(x=>x.StartsWith("PASS")||x.StartsWith("FAIL")).ToArray();await File.WriteAllLinesAsync(Path.Combine(output,name+".txt"),safe);await stderr;
        Assert(process.ExitCode==0,name+" process failed; inspect private local artifacts without publishing raw credentials");
    }
    async Task BrowserChild(){
        var v=await Fixture(1);var p=await ProductFor(v[0].Id);xssProductId=p.Id;
        await Use(s=>s.SaveProduct(adminId,p.Id,Input(p) with{Name="<script>window.__qaXss=1</script>",Description="<img src=x onerror=window.__qaXss=1>"}));
        await Check("UI-013","Chrome desktop/mobile, XSS và mất mạng guest","Tất cả assertion trình duyệt đạt",async()=>await Child("node",["scripts/browser-checks.mjs"],"browser"),kind:"Chrome runtime");
    }
    bool childWindowReset;
    async Task Children(){
        await BrowserChild();
        await Task.Delay(TimeSpan.FromSeconds(61));childWindowReset=true;
        await Check("ENV-014","WinForms runtime và thao tác tạo sản phẩm/phiếu","Native QA không có FAIL",async()=>{await Child("dotnet",[Path.Combine(root,"src/PhoneStore.Admin/bin/Release/net10.0-windows/PhoneStore.Admin.dll"),"--qa"],"native");var path=Path.Combine(root,".artifacts/qa/native/native-results.json");Assert(File.Exists(path),"Missing native test evidence");using var document=JsonDocument.Parse(await File.ReadAllTextAsync(path));Assert(document.RootElement.EnumerateArray().All(x=>x.GetProperty("status").GetString()=="PASS"),"Native assertion failed; see native-results.json");},kind:"WinForms runtime");
    }
}


