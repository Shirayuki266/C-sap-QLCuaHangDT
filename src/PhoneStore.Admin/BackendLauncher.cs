using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace PhoneStore.Admin;
public static class BackendLauncher
{
    public static async Task EnsureReady(AdminSettings settings){
        settings.Validate();
        // A second Admin never races a first launcher into creating another backend.
        var identity=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(settings.ApiBaseUrl)));
        using var gate=new Semaphore(1,1,"Local\\PhoneStore.Backend."+identity);
        if(!await Task.Run(()=>gate.WaitOne(TimeSpan.FromSeconds(40))))throw new InvalidOperationException("Một phiên khác đang khởi động backend. Vui lòng thử lại.");
        try{
            if(await PortOpen(settings.ApiBaseUrl)&&await Probe(settings))return;
            if(!settings.StartLocalBackend)throw new InvalidOperationException("Không thể kết nối backend máy chủ. Kiểm tra địa chỉ và kết nối mạng; ứng dụng không tự khởi động server từ xa.");
            var uri=new Uri(settings.ApiBaseUrl);
            if(await PortOpen(settings.ApiBaseUrl))throw new InvalidOperationException("Cổng API đang được dịch vụ khác hoặc backend chưa đúng phiên bản sử dụng. Không kết nối nhầm và không dừng tiến trình đó. Kiểm tra Cài đặt kết nối.");
            var executable=Path.GetFullPath(settings.BackendExecutable,AppContext.BaseDirectory);
            if(!File.Exists(executable))throw new InvalidOperationException("Không tìm thấy backend đã publish. Hãy cài lại thư mục portable đầy đủ.");
            if(!Path.IsPathFullyQualified(settings.BackendConfiguration)||!File.Exists(settings.BackendConfiguration))throw new InvalidOperationException("Chưa thiết lập kết nối CSDL. Chạy thiết lập ban đầu; không tự tạo hoặc reset dữ liệu khi mở app.");
            var configuration=JsonSerializer.Deserialize<Dictionary<string,string>>(await File.ReadAllTextAsync(settings.BackendConfiguration))??[];
            if(!configuration.TryGetValue("ConnectionStrings__Store",out var connection)||string.IsNullOrWhiteSpace(connection))throw new InvalidOperationException("Thiếu cấu hình kết nối CSDL. Chạy bước thiết lập ban đầu.");
            var start=new ProcessStartInfo(executable){WorkingDirectory=Path.GetDirectoryName(executable)!,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
            start.ArgumentList.Add("--urls");start.ArgumentList.Add(settings.ApiBaseUrl);
            foreach(var pair in configuration)if(pair.Key is "ConnectionStrings__Store" or "QA__Server" or "QA__Database")start.Environment[pair.Key]=pair.Value;
            start.Environment["ASPNETCORE_ENVIRONMENT"]=settings.Environment;
            start.Environment["PHONESTORE_LAUNCHER_MANAGED"]="1";
            using var process=Process.Start(start)??throw new InvalidOperationException("Không thể khởi động backend đã publish.");
            process.OutputDataReceived+=(_,_)=>{};process.ErrorDataReceived+=(_,_)=>{};process.BeginOutputReadLine();process.BeginErrorReadLine();
            // Do not persist raw SQL/authentication errors or connection details to a launcher log.
            for(var i=0;i<40;i++){
                if(process.HasExited)throw new InvalidOperationException("Backend không khởi động được. Kiểm tra dịch vụ SQL Server và cấu hình môi trường; launcher không tự migration/seed.");
                if(await PortOpen(settings.ApiBaseUrl)&&await Probe(settings))return;await Task.Delay(500);
            }
            throw new InvalidOperationException("Backend chưa sẵn sàng sau thời gian chờ. Kiểm tra SQL Server và cấu hình; không tiếp tục đăng nhập nhầm dịch vụ.");
            // The backend is shared with Website: it deliberately survives closing Admin.
        }finally{gate.Release();}
    }
    static async Task<bool> PortOpen(string url){var uri=new Uri(url);if(uri.IsLoopback)return System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(x=>x.Port==uri.Port);using var socket=new TcpClient();try{await socket.ConnectAsync(uri.Host,uri.Port).WaitAsync(TimeSpan.FromSeconds(3));return true;}catch(SocketException){return false;}catch(TimeoutException){throw new InvalidOperationException("Kết nối máy chủ hết thời gian chờ. Kiểm tra mạng và địa chỉ kết nối.");}}
    static async Task<bool> Probe(AdminSettings settings){
        using var client=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(8)};
        try{
            var healthUrl=new UriBuilder(settings.ApiBaseUrl){Path="/health/ready"};if(healthUrl.Host=="localhost")healthUrl.Host="127.0.0.1";
            using var response=await client.GetAsync(healthUrl.Uri);
            if(response.StatusCode!=System.Net.HttpStatusCode.OK&&response.StatusCode!=System.Net.HttpStatusCode.ServiceUnavailable)throw new InvalidOperationException("Địa chỉ đang trỏ tới dịch vụ không có health check Phone Store đúng phiên bản.");
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var root=json.RootElement;
            if(root.GetProperty("application").GetString()!="PhoneStore"||root.GetProperty("environment").GetString()!=settings.Environment||!string.Equals(root.GetProperty("target").GetString(),settings.TargetFingerprint,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Backend không đúng môi trường hoặc CSDL đã chọn. Không tiếp tục đăng nhập.");
            if(!response.IsSuccessStatusCode)throw new InvalidOperationException("Backend đã chạy nhưng CSDL không sẵn sàng hoặc chưa có schema. Kiểm tra SQL Server và thực hiện thiết lập CSDL ban đầu riêng.");
            return root.GetProperty("ready").GetBoolean();
        }catch(HttpRequestException){return false;}catch(TaskCanceledException){throw new InvalidOperationException("Health check hết thời gian chờ. Kiểm tra backend/SQL Server; đây không phải lỗi mật khẩu.");}
        catch(JsonException){throw new InvalidOperationException("Cổng đang trả dữ liệu của dịch vụ khác; không kết nối.");}
        catch(KeyNotFoundException){throw new InvalidOperationException("Phản hồi health check không hợp lệ; không kết nối dịch vụ này.");}
    }
}
