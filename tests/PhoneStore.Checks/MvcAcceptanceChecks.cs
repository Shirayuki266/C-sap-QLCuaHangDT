using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class MvcAcceptanceChecks
{
    public static async Task Run()
    {
        await using var database=await TestDatabase.Create();var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!File.Exists(Path.Combine(root.FullName,"PhoneStore.slnx")))root=root.Parent;
        var faults=new MvcServiceFault();await using var factory=new MvcFoundationFactory(database.ConnectionString,root!.FullName,faults:faults);MvcOrdersFixture data;
        await using(var scope=factory.Services.CreateAsyncScope()){await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync();data=await MvcOrdersFixture.Seed(scope.ServiceProvider);await scope.ServiceProvider.GetRequiredService<IStoreService>().UpdateCartAsync(data.Catalog.Customer,data.Catalog.Black128,1);}
        using var guest=factory.Browser();using var customer=factory.Browser();using var admin=factory.Browser();
        async Task<string> Token(HttpClient client){using var response=await client.GetAsync("/api/security/token");return(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;}
        async Task<HttpResponseMessage> Post(HttpClient client,string path,Dictionary<string,string> values){values=new(values){["__RequestVerificationToken"]=await Token(client)};return await client.PostAsync(path,new FormUrlEncodedContent(values));}
        using(var login=await Post(customer,"/Account/Login",new(){["Username"]="catalog-qa-customer",["Password"]=MvcCatalogFixture.Password}))Check.True(login.StatusCode==HttpStatusCode.Redirect,"Acceptance login failed.");
        using(var request=new HttpRequestMessage(HttpMethod.Post,"/api/auth/login"){Content=JsonContent.Create(new LoginRequest("catalog-qa-admin",MvcCatalogFixture.Password,"admin"))}){request.Headers.Add("X-CSRF-TOKEN",await Token(admin));using var response=await admin.SendAsync(request);Check.True(response.IsSuccessStatusCode,"Real admin API login failed.");}
        async Task<Dictionary<string,string>> Checkout(){using var response=await customer.GetAsync("/Cart/Checkout");return MvcCheckoutChecks.Shipping(MvcCheckoutChecks.Fields(await response.Content.ReadAsStringAsync()));}
        var before=await MvcCheckoutSql.Read(database.ConnectionString);var personal=new[]{"/Account/Profile","/Account/Password","/Cart/Index","/Cart/Checkout","/Orders/Index","/Orders/Details/"+data.CancelId};
        await Check.Run("WEB.BASE.07 all personal GET routes no-store no ETag no304 and no read mutations",async()=>
        {
            foreach(var path in personal){using var request=new HttpRequestMessage(HttpMethod.Get,path);request.Headers.TryAddWithoutValidation("If-None-Match","\"qa-cache-probe\"");using var response=await customer.SendAsync(request);Check.True(response.StatusCode==HttpStatusCode.OK&&response.Headers.CacheControl?.NoStore==true&&response.Headers.ETag is null&&response.Content.Headers.LastModified is null,"Personal response cached: "+path);}
            Check.True(before.Snapshot()==(await MvcCheckoutSql.Read(database.ConnectionString)).Snapshot(),"Personal GET changed commerce SQL.");
        });
        await Check.Run("WEB.BASE.08 real ADMIN cookie cannot use any protected customer GET or mutation",async()=>
        {
            foreach(var path in personal){using var response=await admin.GetAsync(path);var roleHtml=await response.Content.ReadAsStringAsync();Check.True(path=="/Cart/Index"?response.IsSuccessStatusCode&&roleHtml.Contains("data-guest=\"true\""):response.StatusCode==HttpStatusCode.Forbidden,"Admin entered protected customer GET: "+path);}
            foreach(var path in new[]{"/Account/Profile","/Account/Password","/Account/Logout","/Cart/Add","/Cart/Update","/Cart/Remove","/Cart/Merge","/Cart/Checkout","/Orders/Cancel"}){using var response=await Post(admin,path,new(){["id"]=data.CancelId.ToString(),["customerId"]=data.Catalog.Customer.AccountId.ToString()});Check.True(response.StatusCode==HttpStatusCode.Forbidden,"Admin entered customer POST: "+path);}
            using var home=await customer.GetAsync("/Home/Index");var html=await home.Content.ReadAsStringAsync();Check.True(!Regex.IsMatch(html,"(?:href|action)=\"/api/admin",RegexOptions.IgnoreCase)&&before.Snapshot()==(await MvcCheckoutSql.Read(database.ConnectionString)).Snapshot(),"Customer UI links to admin or role rejection wrote SQL.");
        });
        await Check.Run("WEB.BASE.13 all form families retain safe attempted data and never passwords on failure",async()=>
        {
            var retained="Giữ dữ liệu QA";using(var response=await Post(guest,"/Account/Register",new(){["Username"]="catalog-qa-customer",["Password"]=MvcCatalogFixture.Password,["FullName"]=retained,["Phone"]="0938123511",["Email"]="keep@example.test",["Address"]=retained}))
            {var html=await response.Content.ReadAsStringAsync();Check.True(response.StatusCode==HttpStatusCode.OK&&WebUtility.HtmlDecode(html).Contains(retained)&&!html.Contains(MvcCatalogFixture.Password),"Registration failure lost safe data or returned password.");}
            using(var response=await Post(guest,"/Account/Login",new(){["Username"]="missing-qa-username",["Password"]=MvcCatalogFixture.Password})){var html=await response.Content.ReadAsStringAsync();Check.True(html.Contains("missing-qa-username")&&!html.Contains(MvcCatalogFixture.Password),"Login form retention failed.");}
            foreach(var (path,values) in new[]{("/Account/Profile",new Dictionary<string,string>{["FullName"]=retained,["Phone"]="bad",["Address"]=retained}), ("/Account/Password",new Dictionary<string,string>{["CurrentPassword"]=MvcCatalogFixture.Password,["NewPassword"]="bad"})})
            {using var response=await Post(customer,path,values);var html=WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());Check.True(response.StatusCode==HttpStatusCode.OK&&html.Contains("validation-")&&!html.Contains(MvcCatalogFixture.Password)&&(path.EndsWith("Password")||html.Contains(retained)),"Profile/password form retention failed.");}
            var checkout=await Checkout();checkout["Recipient"]=retained;checkout["Address"]="";using(var response=await Post(customer,"/Cart/Checkout",checkout))Check.True(WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()).Contains(retained),"Checkout lost shipping values.");
            using(var response=await Post(customer,"/Cart/Update",new(){["VariantId"]=data.Catalog.Black128.ToString(),["Quantity"]="999"}))Check.True((await response.Content.ReadAsStringAsync()).Contains("value=\"999\""),"Cart lost attempted quantity.");
            using(var response=await Post(customer,"/Orders/Cancel",new(){["Id"]=data.CancelId.ToString(),["Version"]="bad",["Reason"]=retained}))Check.True(WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()).Contains(retained),"Cancel lost reason.");
            Check.True(before.Snapshot()==(await MvcCheckoutSql.Read(database.ConnectionString)).Snapshot(),"Invalid forms changed commerce SQL.");
        });
        await Check.Run("WEB.BASE.11 actual MVC controllers map all proposal and backend aliases without accidental500",async()=>
        {
            foreach(var code in new[]{"VALIDATION_FAILED","DUPLICATE_VALUE"}){faults.Arm(nameof(IStoreService.RegisterAsync),code);using var response=await Post(guest,"/Account/Register",new(){["Username"]="new-qa-fault",["Password"]=MvcCatalogFixture.Password,["FullName"]="Giữ tên QA",["Phone"]="0938123513"});Check.True(response.StatusCode==HttpStatusCode.OK&&(await response.Content.ReadAsStringAsync()).Contains("validation-summary-errors"),"Form alias mapping failed: "+code);}
            foreach(var code in new[]{"INVALID_CREDENTIALS","ACCOUNT_LOCKED","UNAUTHENTICATED"}){faults.Arm(nameof(IStoreService.LoginAsync),code);using var response=await Post(guest,"/Account/Login",new(){["Username"]="alias-qa",["Password"]=MvcCatalogFixture.Password});Check.True(response.StatusCode==(code=="UNAUTHENTICATED"?HttpStatusCode.Redirect:HttpStatusCode.OK),"Login alias failed: "+code);}
            foreach(var (code,status) in new[]{("FORBIDDEN",403),("NOT_FOUND",404),("NEW_BUSINESS_CODE",400),("INTERNAL_ERROR",500)}){faults.Arm(nameof(IStoreService.ReadCatalogAsync),code);using var response=await guest.GetAsync("/Catalog/Index");var html=await response.Content.ReadAsStringAsync();Check.True((int)response.StatusCode==status&&(status!=500||html.Contains("Mã tra cứu")&&!html.Contains("Injected service validation QA")),"Page alias mapping failed: "+code);}
            using var details=await customer.GetAsync("/Orders/Details/"+data.CancelId);var cancel=MvcCheckoutChecks.Fields(await details.Content.ReadAsStringAsync());cancel["Reason"]="Lý do QA";
            foreach(var code in new[]{"STALE_VERSION","INVALID_STATE","CONCURRENT_UPDATE"}){faults.Arm(nameof(IStoreService.CancelCustomerOrderAsync),code);using var response=await Post(customer,"/Orders/Cancel",cancel);Check.True(response.StatusCode==HttpStatusCode.Redirect&&response.Headers.Location!.OriginalString=="/Orders/Details/"+data.CancelId,"State alias failed: "+code);}
            foreach(var code in new[]{"PRICE_CHANGED","INSUFFICIENT_STOCK","VARIANT_INACTIVE","CART_EMPTY","REQUEST_KEY_CONFLICT","PAYMENT_METHOD_UNSUPPORTED"})
            {
                var form=await Checkout();faults.Arm(nameof(IStoreService.PlaceOrderAsync),code);using var response=await Post(customer,"/Cart/Checkout",form);var html=await response.Content.ReadAsStringAsync();
                Check.True(response.StatusCode==HttpStatusCode.OK&&html.Contains("validation-summary-errors")&&WebUtility.HtmlDecode(html).Contains(form["Recipient"]),"Checkout alias failed: "+code);
                if(code is "REQUEST_KEY_CONFLICT" or "PAYMENT_METHOD_UNSUPPORTED")Check.True(MvcCheckoutChecks.Fields(html)["RequestKey"]!=form["RequestKey"],"Conflicting request key not renewed.");
                if(code=="PRICE_CHANGED")Check.True(WebUtility.HtmlDecode(html).Contains("Xác nhận giá mới"),"Price alias did not request confirmation.");
            }
            foreach(var code in new[]{"NEW_BUSINESS_CODE","INTERNAL_ERROR"}){faults.Arm(nameof(IStoreService.QuoteCartAsync),code);using var request=new HttpRequestMessage(HttpMethod.Post,"/Cart/Quote"){Content=JsonContent.Create(new{lines=new[]{new CartInput(data.Catalog.Black128,1)}})};request.Headers.Add("X-CSRF-TOKEN",await Token(guest));using var response=await guest.SendAsync(request);Check.True((int)response.StatusCode==(code=="INTERNAL_ERROR"?500:400),"JSON alias returned accidental500.");}
            Check.True(before.Snapshot()==(await MvcCheckoutSql.Read(database.ConnectionString)).Snapshot(),"Injected transport faults changed real commerce SQL.");
        });
    }
}
