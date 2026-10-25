using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
namespace PhoneStore.Admin;
public sealed class ApiClient:IDisposable
{
    private HttpClient? http;
    public string BaseUrl {get;private set;}="";
    public void Connect(string url) {
        if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme is not ("http" or "https")) throw new InvalidOperationException("Địa chỉ máy chủ không hợp lệ.");
        if(uri.Scheme=="http"&&!uri.IsLoopback) throw new InvalidOperationException("Máy chủ từ xa cần HTTPS để bảo vệ tài khoản.");
        http?.Dispose(); http=new(new HttpClientHandler{CookieContainer=new CookieContainer(),UseCookies=true}){BaseAddress=uri,Timeout=TimeSpan.FromSeconds(40)}; BaseUrl=url;
    }
    public async Task<T> Get<T>(string path) {
        if(http is null)throw new InvalidOperationException("Chưa cấu hình máy chủ API.");
        try{using var response=await http.GetAsync(path);return await Read<T>(response);}
        catch(HttpRequestException ex){throw new InvalidOperationException("Không thể kết nối máy chủ. Vui lòng kiểm tra kết nối và thử lại.",ex);}
        catch(TaskCanceledException ex){throw new InvalidOperationException("Yêu cầu hết thời gian chờ. Vui lòng tải lại trạng thái trước khi thử lại.",ex);}
    }
    public async Task<T> Send<T>(string path,object data,HttpMethod? method=null) {
        var token=await Get<JsonElement>("/api/security/token");using var request=new HttpRequestMessage(method??HttpMethod.Post,path){Content=JsonContent.Create(data)};request.Headers.Add("X-CSRF-TOKEN",token.GetProperty("token").GetString());
        try{using var response=await http!.SendAsync(request);return await Read<T>(response);}
        catch(HttpRequestException ex){throw new InvalidOperationException("Không thể kết nối máy chủ. Vui lòng tải lại dữ liệu trước khi gửi lại thao tác.",ex);}
        catch(TaskCanceledException ex){throw new InvalidOperationException("Yêu cầu hết thời gian chờ. Vui lòng tải lại trạng thái trước khi thử lại.",ex);}
    }
    private static async Task<T> Read<T>(HttpResponseMessage response) {
        if(!response.IsSuccessStatusCode) {var message=(int)response.StatusCode>=500?"Máy chủ gặp lỗi. Vui lòng thử lại sau hoặc liên hệ quản trị; đây không phải lỗi mật khẩu.":response.StatusCode switch{HttpStatusCode.Unauthorized=>"Phiên đã hết hạn hoặc thông tin đăng nhập không đúng. Vui lòng đăng nhập lại.",HttpStatusCode.Forbidden=>"Bạn không có quyền thực hiện chức năng này.",_=>"Yêu cầu không thành công. Vui lòng kiểm tra dữ liệu."};try{var e=await response.Content.ReadFromJsonAsync<JsonElement>();if(e.TryGetProperty("message",out var value))message=value.GetString()??message;else if(e.TryGetProperty("errors",out var errors))message=string.Join(Environment.NewLine,errors.EnumerateObject().SelectMany(x=>x.Value.EnumerateArray()).Select(x=>x.GetString()));}catch(JsonException){}throw new InvalidOperationException(message);}
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
    public void Dispose()=>http?.Dispose();
}
