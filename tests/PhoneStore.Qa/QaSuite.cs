using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PhoneStore.Core.Contracts;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

internal record QaResult(string Id,string Module,string Title,string Expected,string Actual,string Status,string Kind,string Evidence,string? Bug=null,double Ms=0);
internal sealed class QaClient : IDisposable
{
    public static readonly AsyncLocal<string?> CaseId=new();
    public CookieContainer Cookies {get;}=new(); readonly HttpClient http;readonly List<object> requests;readonly string role;
    public QaClient(string url,string role,List<object> requests){this.role=role;this.requests=requests;http=new(new HttpClientHandler{CookieContainer=Cookies,AllowAutoRedirect=false,UseProxy=false}){BaseAddress=new(url),Timeout=TimeSpan.FromSeconds(40)};}
    public async Task<HttpResponseMessage> Get(string path)=>await Request(HttpMethod.Get,path,null,false);
    public async Task<T> Read<T>(string path){using var r=await Get(path);Require(r.IsSuccessStatusCode,$"GET {path.Split('?')[0]} returned {(int)r.StatusCode}");return(await r.Content.ReadFromJsonAsync<T>())!;}
    public async Task<HttpResponseMessage> Send(string path,object? body=null,HttpMethod? method=null,bool csrf=true)=>await Request(method??HttpMethod.Post,path,JsonContent.Create(body??new{}),csrf);
    public async Task<HttpResponseMessage> Form(string path,Dictionary<string,string> body)=>await Request(HttpMethod.Post,path,new FormUrlEncodedContent(body),true);
    public async Task<HttpResponseMessage> Raw(string path,string body,string contentType="application/json")=>await Request(HttpMethod.Post,path,new StringContent(body,System.Text.Encoding.UTF8,contentType),true);
    public async Task<HttpResponseMessage> Request(HttpMethod method,string path,HttpContent? content,bool csrf){
        using var q=new HttpRequestMessage(method,path){Content=content};
        if(csrf){var token=await Read<JsonElement>("/api/security/token");q.Headers.Add("X-CSRF-TOKEN",token.GetProperty("token").GetString());}
        var sw=Stopwatch.StartNew();var r=await http.SendAsync(q);lock(requests)requests.Add(new{testId=CaseId.Value??"fixture",method=method.Method,route=path.Split('?')[0],role,status=(int)r.StatusCode,ms=Math.Round(sw.Elapsed.TotalMilliseconds,2)});return r;
    }
    public static void Require(bool c,string m){if(!c)throw new Exception(m);}
    public async Task Login(string name,string password,bool admin=false){using var r=await Send("/api/auth/login?admin="+admin.ToString().ToLowerInvariant(),new LoginRequest(name,password));Require(r.IsSuccessStatusCode,"Fixture login failed, status "+(int)r.StatusCode);}
    public void Dispose()=>http.Dispose();
}
internal sealed partial class QaSuite
{
    readonly string root,output;readonly List<QaResult> results=[];readonly List<object> requests=[];readonly JsonSerializerOptions json=new(){WriteIndented=true,PropertyNameCaseInsensitive=true};
    DbContextOptions<StoreDbContext> options=null!;QaClient admin=null!,admin2=null!,a=null!,b=null!,anon=null!;long adminId,aId,bId;int brand,category;long supplier;string adminPassword="",customerPassword="",url="";Process? server;int nextFixture;
    public QaSuite(string root){this.root=root;output=Path.Combine(root,".artifacts/qa");Directory.CreateDirectory(output);}
    StoreDbContext Open()=>new(options);
    async Task<T> Use<T>(Func<StoreService,Task<T>> f){await using var d=Open();return await f(new(d));}
    static void Assert(bool c,string m="Assertion failed")=>QaClient.Require(c,m);
    async Task Check(string id,string title,string expected,Func<Task> f,string? bug=null,string kind="HTTP + SQL"){
        QaClient.CaseId.Value=id;var watch=Stopwatch.StartNew();try{await f();results.Add(new(id,id.Split('-')[0],title,expected,"Các assertion đã đạt.","PASS",kind,"docs/qa/results.json#"+id,bug,watch.Elapsed.TotalMilliseconds));Console.WriteLine("PASS "+id+" "+title);}
        catch(Exception ex){Environment.ExitCode=1;var message=ex is Microsoft.Data.SqlClient.SqlException?"SQL exception (details omitted)":ex.Message;results.Add(new(id,id.Split('-')[0],title,expected,message,"FAIL",kind,"docs/qa/results.json#"+id,bug,watch.Elapsed.TotalMilliseconds));Console.WriteLine("FAIL "+id+" "+message);}
        QaClient.CaseId.Value=null;await Save();
    }
    async Task Save(){await File.WriteAllTextAsync(Path.Combine(output,"results.json"),JsonSerializer.Serialize(results,json));await File.WriteAllTextAsync(Path.Combine(output,"requests.json"),JsonSerializer.Serialize(requests,json));}
    static async Task Status(HttpResponseMessage response,params int[] expected){using(response){Assert(expected.Contains((int)response.StatusCode),$"Expected HTTP {string.Join('/',expected)}, actual {(int)response.StatusCode}");if((int)response.StatusCode>=400&&response.RequestMessage?.RequestUri?.AbsolutePath.StartsWith("/api")==true){var text=await response.Content.ReadAsStringAsync();using var error=JsonDocument.Parse(text);Assert(error.RootElement.TryGetProperty("code",out _)&&error.RootElement.TryGetProperty("message",out _),"API error response thiếu code/message");}}}
    async Task<List<VariantDto>> Fixture(int stock=5,int count=1,decimal price=1000000m){
        var n=++nextFixture;var id=await Use(s=>s.SaveProduct(adminId,0,new("QA"+n,"Điện thoại kiểm thử "+n,brand,category,"Cấu hình kiểm thử","Mô tả tiếng Việt",null,true,Enumerable.Range(1,count).Select(i=>new VariantInput(0,$"QA{n}-{i}","Màu "+i,128*i,price*i,true)).ToList())));
        var p=await Use(s=>s.ProductDetail(id,true,adminId));if(stock>0){var receipt=await Use(s=>s.SavePurchase(adminId,0,new(supplier,p.Variants.Select(v=>new PurchaseLineInput(v.Id,stock,100m)).ToList())));var r=await Use(s=>s.GetPurchase(adminId,receipt));await Use(s=>s.ConfirmPurchase(adminId,receipt,new(r.Version,Guid.NewGuid())));}return p.Variants;
    }
    async Task Clear(QaClient client){var cart=await client.Read<CartDto>("/api/cart");foreach(var l in cart.Lines)await Status(await client.Send("/api/cart",new CartInput(l.VariantId,0),HttpMethod.Put),200);}
    async Task<CheckoutRequest> Cart(QaClient client,IEnumerable<VariantDto> lines,int quantity=1){await Clear(client);foreach(var v in lines)await Status(await client.Send("/api/cart",new CartInput(v.Id,quantity)),200);var cart=await client.Read<CartDto>("/api/cart");return new(Guid.NewGuid(),"Người nhận QA","0909999999","123 Nguyễn Trãi, Quận 1",cart.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price)).ToList());}
    async Task<OrderDto> Place(QaClient client,CheckoutRequest request){using var r=await client.Send("/api/orders",request);Assert(r.IsSuccessStatusCode,"Place status "+(int)r.StatusCode);return(await r.Content.ReadFromJsonAsync<OrderDto>())!;}
    async Task<OrderDto> Move(long id,string action,string? reason="Lý do kiểm thử"){var o=await admin.Read<OrderDto>($"/api/admin/orders/{id}");await Status(await admin.Send($"/api/admin/orders/{id}/{action}",new TransitionRequest(o.Version,Guid.NewGuid(),reason)),200);return await admin.Read<OrderDto>($"/api/admin/orders/{id}");}
    async Task<StockDto> Stock(long id)=>(await admin.Read<List<StockDto>>("/api/admin/stock")).Single(x=>x.VariantId==id);
    async Task<ProductDto> ProductFor(long v){await using var db=Open();var p=(await db.Variants.FindAsync(v))!.ProductId;return await admin.Read<ProductDto>($"/api/admin/products/{p}");}
    static ProductInput Input(ProductDto p)=>new(p.Code,p.Name,p.BrandId,p.CategoryId,p.Specifications,p.Description,p.ImageUrl,p.Active,p.Variants.Select(x=>new VariantInput(x.Id,x.Sku,x.Color,x.StorageGb,x.Price,x.Active,x.Version)).ToList());
    public async Task Run(bool children,bool browserOnly=false){
        var cs=QaEnvironment.Connection(root,QaEnvironment.NewDatabase());
        options=new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(cs.ConnectionString).Options;
        adminPassword="QaA1!"+Guid.NewGuid().ToString("N");customerPassword="QaC1!"+Guid.NewGuid().ToString("N");
        var lease=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);lease.Start();var port=((System.Net.IPEndPoint)lease.LocalEndpoint).Port;lease.Stop();url="http://localhost:"+port;
        await File.WriteAllTextAsync(Path.Combine(output,"environment.json"),JsonSerializer.Serialize(new{database=cs.InitialCatalog,url,sdk="10.0.401",os=Environment.OSVersion.VersionString,configuration="Release",baselineCommit="ccd7b0e + uncommitted workspace; see source hashes"},json));
        try{
            await Check("ENV-009","Migration tạo DB QA","Tạo mới thành công",async()=>{await using var db=Open();await DatabaseSetup.Migrate(db);Assert(await db.Database.CanConnectAsync());},kind:"SQL runtime");
            await Check("ENV-008","Kết nối DB riêng","Kết nối được",async()=>{await using var db=Open();Assert(await db.Database.CanConnectAsync());},kind:"SQL runtime");
            await Check("ENV-011","Seed và seed lại","Không nhân đôi tài khoản/sản phẩm",async()=>{Assert(await Use(s=>s.Seed(adminPassword,customerPassword)));Assert(!await Use(s=>s.Seed(adminPassword,customerPassword)));await using var db=Open();Assert(await db.Accounts.CountAsync()==2&&await db.Products.CountAsync()==3);},kind:"SQL runtime");
            await Check("DB-014","Chạy lại migration giữ dữ liệu","Không mất 3 sản phẩm",async()=>{await using var db=Open();await DatabaseSetup.Migrate(db);Assert(await db.Products.CountAsync()==3);},kind:"SQL runtime");
            await using(var db=Open()){adminId=(await db.Accounts.SingleAsync(x=>x.Role=="ADMIN")).Id;brand=(await db.Brands.FirstAsync()).Id;category=(await db.Categories.FirstAsync()).Id;supplier=(await db.Suppliers.FirstAsync()).Id;}
            var info=new ProcessStartInfo("dotnet"){WorkingDirectory=Path.Combine(root,"src/PhoneStore.Web"),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};info.ArgumentList.Add(Path.Combine(root,"src/PhoneStore.Web/bin/Release/net10.0/PhoneStore.Web.dll"));info.ArgumentList.Add("--urls");info.ArgumentList.Add(url);info.Environment["ConnectionStrings__Store"]=cs.ConnectionString;info.Environment["ASPNETCORE_ENVIRONMENT"]="Development";
            info.Environment["ASPNETCORE_ENVIRONMENT"]="QA";info.Environment["QA__Server"]=cs.DataSource;info.Environment["QA__Database"]=cs.InitialCatalog;
            Environment.SetEnvironmentVariable("PHONESTORE_QA_SERVER",cs.DataSource);Environment.SetEnvironmentVariable("PHONESTORE_QA_DATABASE",cs.InitialCatalog);Environment.SetEnvironmentVariable("PHONESTORE_TEST_CONNECTION",cs.ConnectionString);
            server=Process.Start(info)!;server.OutputDataReceived+=(_,_)=>{};server.ErrorDataReceived+=(_,_)=>{};server.BeginOutputReadLine();server.BeginErrorReadLine();
            admin=new(url,"ADMIN",requests);a=new(url,"CUSTOMER_A",requests);b=new(url,"CUSTOMER_B",requests);anon=new(url,"ANONYMOUS",requests);
            await Check("ENV-012","Khởi động backend QA","GET catalog HTTP 200",async()=>{for(int i=0;i<50;i++){try{using var response=await anon.Get("/api/catalog");if(response.IsSuccessStatusCode)return;}catch(HttpRequestException){}await Task.Delay(200);}throw new Exception("QA server did not become ready");});
            await admin.Login("admin",adminPassword,true);
            await Check("UC01-001","Đăng ký hai khách QA","Hai tài khoản CUSTOMER khác ID",async()=>{using var ra=await anon.Send("/api/auth/register",new RegisterRequest("qa-a",customerPassword,"Khách A","0911111111","qa-a@example.test","123 địa chỉ QA"));Assert(ra.IsSuccessStatusCode,"registration A");aId=(await ra.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();using var rb=await anon.Send("/api/auth/register",new RegisterRequest("qa-b",customerPassword,"Khách B","0922222222",null,null));Assert(rb.IsSuccessStatusCode,"registration B");bId=(await rb.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();Assert(aId!=bId);});
            await a.Login("qa-a",customerPassword);await b.Login("qa-b",customerPassword);if(browserOnly){await BrowserChild();return;}
            await Accounts();await DiscoveryHomeBeforeSales();await Catalog();await Carts();await Orders();await Administration();await Concurrency();await EndToEnd();await SafetyAndRiskCases();await Discovery();
            if(children)await Children();await EndpointMatrix();await Database();
        }finally{if(server is{HasExited:false}){server.Kill(true);await server.WaitForExitAsync();}admin?.Dispose();admin2?.Dispose();a?.Dispose();b?.Dispose();anon?.Dispose();await Save();}
        Environment.ExitCode=results.Any(x=>x.Status=="FAIL")?1:0;
    }
}









