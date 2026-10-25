using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using PhoneStore.Core.Contracts;
internal static class ApiChecks
{
    private sealed class Client:IDisposable {
        private readonly HttpClient http;
        public Client(string url){http=new(new HttpClientHandler{CookieContainer=new(),AllowAutoRedirect=false,UseProxy=false}){BaseAddress=new Uri(url),Timeout=TimeSpan.FromSeconds(45)};}
        public async Task<HttpResponseMessage> Get(string path)=>await http.GetAsync(path);
        public async Task<T> Read<T>(string path){using var response=await http.GetAsync(path);await Okay(response);return(await response.Content.ReadFromJsonAsync<T>())!;}
        private async Task<string> Token(){var token=await Read<JsonElement>("/api/security/token");return token.GetProperty("token").GetString()!;}
        public async Task<HttpResponseMessage> Send(string path,object body,HttpMethod? method=null,bool csrf=true){using var request=new HttpRequestMessage(method??HttpMethod.Post,path){Content=JsonContent.Create(body)};if(csrf)request.Headers.Add("X-CSRF-TOKEN",await Token());return await http.SendAsync(request);}
        public async Task<HttpResponseMessage> Form(string path,Dictionary<string,string> fields){using var request=new HttpRequestMessage(HttpMethod.Post,path){Content=new FormUrlEncodedContent(fields)};request.Headers.Add("X-CSRF-TOKEN",await Token());return await http.SendAsync(request);}
        public void Dispose()=>http.Dispose();
    }
    private static async Task Okay(HttpResponseMessage r){if(!r.IsSuccessStatusCode)throw new Exception($"HTTP {(int)r.StatusCode}: "+await r.Content.ReadAsStringAsync());}
    private static void Assert(bool c,string message){if(!c)throw new Exception(message);}
    public static async Task Run(string url)
    {
        await PhoneStore.Infrastructure.Data.QaTarget.VerifyHttp(url);
        using var customer=new Client(url);using var admin=new Client(url);using var anonymous=new Client(url);var passed=0;
        async Task Check(string name,Func<Task> f){await f();passed++;Console.WriteLine("PASS HTTP "+name);}
        var stamp=DateTime.UtcNow.ToString("MMddHHmmss");var username="verify"+stamp;var phone="09"+stamp[..9];
        await Check("anonymous catalog and rendered account pages",async()=>{
            foreach(var path in new[]{"/","/Account/Register","/Account/Login","/Cart"}){using var r=await anonymous.Get(path);await Okay(r);Assert((await r.Content.ReadAsStringAsync()).Contains("lang=\"vi\""),"Missing Vietnamese layout");}
            using var denied=await anonymous.Get("/api/admin/stock");Assert(denied.StatusCode==HttpStatusCode.Unauthorized,"Anonymous admin access");
            var catalog=await anonymous.Read<Page<ProductDto>>("/api/catalog?Query=iphone&StorageGb=128");Assert(catalog.Items.Count>0,"Missing SQL catalog");
        });
        await Check("CSRF blocks unauthenticated mutation without token",async()=>{using var r=await anonymous.Send("/api/auth/register",new RegisterRequest("blocked","Password12345","Tên","0911111111",null,null),csrf:false);Assert(r.StatusCode==HttpStatusCode.BadRequest,"CSRF not rejected");});
        await Check("MVC registration and login with persisted account",async()=>{
            using var r=await customer.Form("/Account/Register",new(){{"Username",username},{"Password","Verify@12345"},{"FullName","Khách nghiệm thu"},{"Phone",phone},{"Email",username+"@example.test"},{"Address","123 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh"}});Assert(r.StatusCode==HttpStatusCode.Redirect,"MVC registration failed: "+await r.Content.ReadAsStringAsync());
            using var login=await customer.Form("/Account/Login",new(){{"Username",username},{"Password","Verify@12345"}});Assert(login.StatusCode==HttpStatusCode.Redirect,"MVC login failed");
            var profile=await customer.Read<UserDto>("/api/profile");Assert(profile.Username==username&&profile.Role=="CUSTOMER","Incorrect user/role");
            using var denied=await customer.Get("/api/admin/stock");Assert(denied.StatusCode==HttpStatusCode.Forbidden,"Customer received admin access");
        });
        ProductDto? product=null;OrderDto? order=null;
        await Check("MVC choose variant, add/update cart, checkout and order detail",async()=>{
            product=(await customer.Read<Page<ProductDto>>("/api/catalog?Query=iphone")).Items.First(x=>x.Variants.Any(v=>v.Available>0));var variant=product.Variants.First(v=>v.Available>0);
            using(var details=await customer.Get($"/Catalog/Details/{product.Id}")){await Okay(details);Assert((await details.Content.ReadAsStringAsync()).Contains(variant.Sku)||product.Variants.Count>0,"Missing variants");}
            using(var add=await customer.Form("/Cart/Add",new(){{"VariantId",variant.Id.ToString()},{"Quantity","1"}}))Assert(add.StatusCode==HttpStatusCode.Redirect,"Cart add failed");
            var cart=await customer.Read<CartDto>("/api/cart");Assert(cart.Lines.Single().Quantity==1,"Cart not persisted");
            using(var update=await customer.Form("/Cart/Update",new(){{"VariantId",variant.Id.ToString()},{"Quantity","1"}}))Assert(update.StatusCode==HttpStatusCode.Redirect,"Cart update failed");
            using var checkout=await customer.Get("/Cart/Checkout");await Okay(checkout);var html=await checkout.Content.ReadAsStringAsync();var fields=new Dictionary<string,string>();
            foreach(Match tag in Regex.Matches(html,"<input[^>]+>")){var name=Regex.Match(tag.Value,"name=\"([^\"]+)\"");var value=Regex.Match(tag.Value,"value=\"([^\"]*)\"");if(name.Success&&value.Success)fields[WebUtility.HtmlDecode(name.Groups[1].Value)]=WebUtility.HtmlDecode(value.Groups[1].Value);}
            fields["Recipient"]="Khách nghiệm thu";fields["Phone"]=phone;fields["Address"]="123 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh";
            Assert(fields.ContainsKey("Lines[0].Price"),"Missing price confirmation");
            using var place=await customer.Form("/Cart/Checkout",fields);Assert(place.StatusCode==HttpStatusCode.Redirect,"MVC checkout failed: "+await place.Content.ReadAsStringAsync());
            var redirect=place.Headers.Location!.ToString();Assert(redirect.Contains("/Orders/Details/"),"Incorrect checkout redirect: "+redirect);var id=long.Parse(redirect.Split('/').Last());order=await customer.Read<OrderDto>($"/api/orders/{id}");Assert(order.Status=="CHO_XAC_NHAN"&&order.Total==variant.Price,"Incorrect placed order");
            using var page=await customer.Get(redirect);await Okay(page);Assert((await page.Content.ReadAsStringAsync()).Contains(order.Code),"Order details failed");
        });
        await Check("admin login, confirm, dispatch and complete COD through API",async()=>{
            using var login=await admin.Send("/api/auth/login?admin=true",new LoginRequest("admin",Environment.GetEnvironmentVariable("PHONESTORE_ADMIN_PASSWORD")??throw new InvalidOperationException("Thiếu credential Admin QA.")));await Okay(login);
            var v=product!.Variants.First(x=>x.Id==order!.Lines[0].VariantId);var stockBefore=(await admin.Read<List<StockDto>>("/api/admin/stock")).Single(x=>x.VariantId==v.Id);Assert(stockBefore.Reserved>=1,"Reservation not created");
            foreach(var action in new[]{"confirm","dispatch","complete"}){order=await admin.Read<OrderDto>($"/api/admin/orders/{order!.Id}");using var r=await admin.Send($"/api/admin/orders/{order.Id}/{action}",new TransitionRequest(order.Version,Guid.NewGuid()));await Okay(r);}
            order=await customer.Read<OrderDto>($"/api/orders/{order!.Id}");Assert(order.Status=="HOAN_TAT"&&order.Payment=="DA_THU","COD not collected");
            var stockAfter=(await admin.Read<List<StockDto>>("/api/admin/stock")).Single(x=>x.VariantId==v.Id);Assert(stockAfter.OnHand==stockBefore.OnHand-1&&stockAfter.Reserved==stockBefore.Reserved-1,"Dispatch balance mismatch");
        });
        await Check("reports, ownership, invalid inputs and lock invalidates existing cookie",async()=>{
            var report=await admin.Read<ReportResult>("/api/admin/reports?kind=revenue");Assert(report.Total>=order!.Total,"Completed COD missing from revenue");
            using var invalid=await admin.Send("/api/admin/stock/"+order.Lines[0].VariantId+"/adjust",new AdjustmentInput(-1,"", "invalid",Guid.NewGuid()));Assert(invalid.StatusCode==HttpStatusCode.Conflict||invalid.StatusCode==HttpStatusCode.BadRequest,"Invalid stock accepted");
            using var anonymousOrder=await anonymous.Get($"/api/orders/{order.Id}");Assert(anonymousOrder.StatusCode==HttpStatusCode.Unauthorized,"Order not protected");
            var user=await customer.Read<UserDto>("/api/profile");using(var locked=await admin.Send($"/api/admin/customers/{user.Id}",new CustomerInput(user.FullName,user.Phone,user.Email,user.Address,false),HttpMethod.Put))await Okay(locked);
            using var oldCookie=await customer.Get("/api/profile");Assert(oldCookie.StatusCode==HttpStatusCode.Unauthorized,"Locked session still works");
            using(var unlocked=await admin.Send($"/api/admin/customers/{user.Id}",new CustomerInput(user.FullName,user.Phone,user.Email,user.Address,true),HttpMethod.Put))await Okay(unlocked);
            using var logout=await admin.Send("/api/auth/logout",new{});await Okay(logout);using var after=await admin.Get("/api/admin/stock");Assert(after.StatusCode==HttpStatusCode.Unauthorized,"Logout failed");
        });
        Console.WriteLine($"{passed} HTTP acceptance checks passed. Retained sample user: {username}, completed order: {order!.Code}");
    }
}
