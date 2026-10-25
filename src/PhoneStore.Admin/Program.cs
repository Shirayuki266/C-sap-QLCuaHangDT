using System.Globalization;
using PhoneStore.Core.Contracts;
namespace PhoneStore.Admin;
static class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr handle);
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool ShowWindow(IntPtr handle,int command);
    [STAThread] static void Main(string[] args) {
        CultureInfo.DefaultThreadCurrentCulture=CultureInfo.GetCultureInfo("vi-VN");CultureInfo.DefaultThreadCurrentUICulture=CultureInfo.GetCultureInfo("vi-VN");ApplicationConfiguration.Initialize();
        if(args.Contains("--launcher-check")){string message;try{BackendLauncher.EnsureReady(AdminSettings.Load()).GetAwaiter().GetResult();Environment.ExitCode=0;message="Backend sẵn sàng và đúng đích.";}catch(Exception ex){Environment.ExitCode=1;message=ex is InvalidOperationException?ex.Message:"Cấu hình hoặc tệp ứng dụng không hợp lệ.";}Directory.CreateDirectory(AdminSettings.DataDirectory);File.WriteAllText(Path.Combine(AdminSettings.DataDirectory,"launcher-check.json"),System.Text.Json.JsonSerializer.Serialize(new{status=Environment.ExitCode==0?"PASS":"FAIL",message}));return;}
        using var instance=new Mutex(true,"Local\\PhoneStore.Admin.Interactive",out var firstInstance);
        if(!firstInstance&&!args.Contains("--qa")&&!args.Contains("--smoke")){var open=System.Diagnostics.Process.GetProcessesByName("PhoneStore.Admin").FirstOrDefault(p=>p.Id!=System.Environment.ProcessId&&p.MainWindowHandle!=IntPtr.Zero);if(open!=null){ShowWindow(open.MainWindowHandle,9);SetForegroundWindow(open.MainWindowHandle);}return;}
        if(args.Contains("--smoke")||args.Contains("--qa")) {
            var qaUrl=Environment.GetEnvironmentVariable("PHONESTORE_API_URL")??throw new InvalidOperationException("Cần URL QA rõ ràng, không fallback.");
            var qaDatabase=Environment.GetEnvironmentVariable("PHONESTORE_QA_DATABASE");
            if(!Uri.TryCreate(qaUrl,UriKind.Absolute,out var qaUri)||!qaUri.IsLoopback||!System.Text.RegularExpressions.Regex.IsMatch(qaDatabase??"",@"^PhoneStore_QA(?:_[A-Za-z0-9_]+)?$"))throw new InvalidOperationException("Chỉ chạy smoke/QA trên đích QA loopback.");
            using(var http=new HttpClient(new HttpClientHandler{UseProxy=false})){
                var identity=System.Text.Json.JsonDocument.Parse(http.GetStringAsync(new Uri(qaUri,"/api/qa/identity")).GetAwaiter().GetResult()).RootElement;
                if(identity.GetProperty("environment").GetString()!="QA"||identity.GetProperty("database").GetString()!=qaDatabase||identity.GetProperty("server").GetString()!=Environment.GetEnvironmentVariable("PHONESTORE_QA_SERVER"))throw new InvalidOperationException("Backend không khớp server/database QA.");
            }
            using var api=new ApiClient();api.Connect(qaUrl);
            var user=api.Send<UserDto>("/api/auth/login?admin=true",new LoginRequest("admin",Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_PASSWORD")??throw new InvalidOperationException("Thiếu credential QA."))).GetAwaiter().GetResult();
            using var form=new MainForm(api,user,true){ShowInTaskbar=false,Opacity=0};
            form.Shown+=async(_,_)=>{try{if(args.Contains("--qa")){if(Environment.GetEnvironmentVariable("PHONESTORE_QA_MODE")!="1")throw new InvalidOperationException("QA mode requires isolated test runner.");await NativeQa.Run(form,api,Path.Combine(Environment.CurrentDirectory,".artifacts","qa","native"));}else await form.Smoke(Path.Combine(Environment.CurrentDirectory,".artifacts","winforms"));}catch(Exception ex){Directory.CreateDirectory(".artifacts/winforms");File.WriteAllText(".artifacts/winforms/error.txt",ex.ToString());Environment.ExitCode=1;}finally{form.Close();}};
            Application.Run(form);return;
        }
        do {using var login=new LoginForm();if(login.ShowDialog()!=DialogResult.OK)break;using var main=new MainForm(login.Api,login.User!);Application.Run(main);if(!main.LoginAgain)break;}while(true);
    }
}




