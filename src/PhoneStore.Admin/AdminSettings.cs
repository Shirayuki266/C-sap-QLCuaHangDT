using System.Text.Json;
namespace PhoneStore.Admin;
public sealed record AdminSettings(string ApiBaseUrl,string Environment,string TargetFingerprint,bool StartLocalBackend,string BackendExecutable,string BackendConfiguration)
{
    public static string DataDirectory=>EnvironmentPath();
    static string EnvironmentPath()=>System.Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_DATA")??Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),"PhoneStore","Admin");
    public static string FilePath=>Path.Combine(DataDirectory,"connection.json");
    public static AdminSettings Load(){
        var path=File.Exists(FilePath)?FilePath:Path.Combine(AppContext.BaseDirectory,"adminsettings.json");
        return JsonSerializer.Deserialize<AdminSettings>(File.ReadAllText(path),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??throw new InvalidOperationException("Chưa có cấu hình. Hãy chạy bước thiết lập ban đầu.");
    }
    public void Validate(){
        if(!Uri.TryCreate(ApiBaseUrl,UriKind.Absolute,out var uri)||uri.Scheme is not ("http" or "https")||uri.AbsolutePath!="/"||!string.IsNullOrEmpty(uri.Query)||!string.IsNullOrEmpty(uri.UserInfo))throw new InvalidOperationException("URL API phải là địa chỉ gốc HTTP/HTTPS hợp lệ.");
        if(!uri.IsLoopback&&uri.Scheme!="https")throw new InvalidOperationException("Backend máy chủ từ xa phải dùng HTTPS.");
        if(StartLocalBackend&&!uri.IsLoopback)throw new InvalidOperationException("Chỉ tự khởi động backend với địa chỉ loopback trên máy này.");
        if(string.IsNullOrWhiteSpace(Environment)||!System.Text.RegularExpressions.Regex.IsMatch(TargetFingerprint??"","^[A-Fa-f0-9]{64}$"))throw new InvalidOperationException("Thiếu môi trường hoặc mã xác minh CSDL. Hãy chạy thiết lập ban đầu.");
    }
    public void Save(){Validate();Directory.CreateDirectory(DataDirectory);File.WriteAllText(FilePath,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));}
}
