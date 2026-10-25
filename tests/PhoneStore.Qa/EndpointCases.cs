using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using PhoneStore.Core.Contracts;
using PhoneStore.Web.Controllers;
internal sealed record Endpoint(string Method,string Route,string Role,string Request,string Response);
internal sealed partial class QaSuite
{
    static string TypeName(Type t)=>t.IsGenericType?(t.GetGenericTypeDefinition()==typeof(Task<>)?TypeName(t.GetGenericArguments()[0]):t.Name.Split('`')[0]+"<"+string.Join(",",t.GetGenericArguments().Select(TypeName))+">"):t.Name;
    List<Endpoint> Inventory()=>typeof(StoreApiController).Assembly.GetTypes().Where(t=>typeof(ControllerBase).IsAssignableFrom(t)).SelectMany(t=>t.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly).SelectMany(m=>m.GetCustomAttributes<HttpMethodAttribute>().SelectMany(h=>h.HttpMethods.Select(method=>{
        var prefix=t.GetCustomAttribute<RouteAttribute>()?.Template;var route=(prefix==null?h.Template:prefix+"/"+h.Template)?.TrimEnd('/')??"";
        var role=m.GetCustomAttributes<AuthorizeAttribute>().Concat(t.GetCustomAttributes<AuthorizeAttribute>()).Select(x=>x.Roles??"AUTHENTICATED").FirstOrDefault()??"PUBLIC";
        return new Endpoint(method,"/"+route,role,string.Join(", ",m.GetParameters().Where(p=>p.ParameterType!=typeof(Microsoft.AspNetCore.Antiforgery.IAntiforgery)).Select(p=>p.Name+":"+TypeName(p.ParameterType))),TypeName(m.ReturnType));
    })))).Where(x=>x.Route.StartsWith("/api")).OrderBy(x=>x.Route).ThenBy(x=>x.Method).ToList();
    async Task EndpointMatrix(){
        await Check("QA-LOOKUPS","Hãng/danh mục API CRUD mềm","POST/PUT/GET đều hoạt động",async()=>{foreach(var kind in new[]{"brands","categories"}){var id=await Create("/api/admin/lookups/"+kind,new LookupRequest("Mục QA "+kind,true));await Status(await admin.Send($"/api/admin/lookups/{kind}/{id}",new LookupRequest("Mục QA sửa "+kind,false),HttpMethod.Put),200);Assert((await admin.Read<List<LookupDto>>("/api/admin/lookups/"+kind)).Single(x=>x.Id==id).Active==false);}});
        await Check("QA-MERGE","Hợp nhất giỏ/preview guest","Cộng lượng đúng, guest đọc giá DB, không giữ",async()=>{var v=(await Fixture(5))[0];await Clear(a);await Status(await a.Send("/api/cart/merge",new[]{new CartInput(v.Id,1),new(v.Id,1)}),200);Assert((await a.Read<CartDto>("/api/cart")).Lines.Single().Quantity==2);using var r=await anon.Send("/api/guest-cart/preview",new[]{new CartInput(v.Id,2)});Assert(r.IsSuccessStatusCode);var c=(await r.Content.ReadFromJsonAsync<CartDto>())!;Assert(c.Total==2000000m&&(await Stock(v.Id)).Reserved==0);});
        await Check("QA-PUBLIC-LOOKUPS","Danh mục public","Hai loại lookup trả200",async()=>{foreach(var kind in new[]{"brands","categories"})Assert((await anon.Read<List<LookupDto>>("/api/lookups/"+kind)).Count>0);});
        await Check("QA-ADMIN-ORDERLIST","Tìm/lọc đơn Admin theo khách và mã","Chỉ đơn khớp tiêu chí",async()=>{var list=await admin.Read<Page<OrderDto>>("/api/admin/orders?customerId="+aId+"&status=HOAN_TAT");Assert(list.Total>0&&list.Items.All(x=>x.CustomerId==aId&&x.Status=="HOAN_TAT"));var code=list.Items[0].Code;var searched=await admin.Read<Page<OrderDto>>("/api/admin/orders?query="+code+"&from=2000-01-01&to=2099-01-01");Assert(searched.Total==1&&searched.Items[0].Code==code);});
        await Check("QA-DRAFT-CANCEL","Hủy phiếu nháp hợp lệ","200, DA_HUY, không thay đổi tồn",async()=>{var id=await Create("/api/admin/purchases",new PurchaseInput(supplier,[]));var p=await admin.Read<PurchaseDto>($"/api/admin/purchases/{id}");await Status(await admin.Send($"/api/admin/purchases/{id}/cancel",new TransitionRequest(p.Version,Guid.NewGuid())),200);Assert((await admin.Read<PurchaseDto>($"/api/admin/purchases/{id}")).Status=="DA_HUY");});
        await Check("QA-PURCHASE-LIST","Danh sách phiếu nhập lọc trạng thái/mã và phân trang","200, schema Page và đúng phiếu được lọc",async()=>{var list=await admin.Read<Page<PurchaseDto>>("/api/admin/purchases?status=DA_HUY&page=1");Assert(list.Total>0&&list.Items.All(x=>x.Status=="DA_HUY"));var chosen=list.Items[0];var searched=await admin.Read<Page<PurchaseDto>>("/api/admin/purchases?query="+Uri.EscapeDataString(chosen.Code));Assert(searched.Total==1&&searched.Items.Single().Id==chosen.Id);});
        var endpoints=Inventory();await File.WriteAllTextAsync(Path.Combine(output,"endpoints.json"),JsonSerializer.Serialize(endpoints,json));
        var i=0;
        // Test auth validation with a fresh limiter window rather than treating HTTP 429 as a validation failure.
        if(!childWindowReset)await Task.Delay(TimeSpan.FromSeconds(61));
        foreach(var endpoint in endpoints){i++;var tag="EP"+i.ToString("000");var path=endpoint.Route.Replace("{id:long}","1").Replace("{id:int}","1").Replace("{kind}","brands").Replace("{actionName}","confirm");var method=new HttpMethod(endpoint.Method);
            if(method==HttpMethod.Get){var client=endpoint.Role=="ADMIN"?admin:endpoint.Role=="PUBLIC"?anon:a;
                await Check(tag+"-METHOD",endpoint.Route+" sai HTTP method","405 với JSON error",async()=>await Status(await client.Request(HttpMethod.Delete,path,null,false),405),kind:"HTTP contract");
                if(endpoint.Route.Contains("{id:long}"))await Check(tag+"-MISSING-ID",endpoint.Route+" ID không tồn tại","404 JSON",async()=>await Status(await client.Get(path.Replace("/1","/9223372036854775807")),404),kind:"HTTP contract");
            }
            if(endpoint.Role!="PUBLIC"){
                await Check(tag+"-ANON",endpoint.Method+" "+endpoint.Route+" chưa login","401; không vào xử lý nghiệp vụ",async()=>await Status(await anon.Request(method,path,method==HttpMethod.Get?null:JsonContent.Create(new{}),true),401),kind:"HTTP gate");
                if(endpoint.Role is "ADMIN" or "CUSTOMER"){
                    var wrong=endpoint.Role=="ADMIN"?a:admin;
                    await Check(tag+"-ROLE",endpoint.Method+" "+endpoint.Route+" sai vai trò","403; không thay đổi dữ liệu",async()=>await Status(await wrong.Request(method,path,method==HttpMethod.Get?null:JsonContent.Create(new{}),true),403),kind:"HTTP gate");
                }
            }
            if(method!=HttpMethod.Get&&endpoint.Route!="/api/auth/logout"){
                var client=endpoint.Role=="ADMIN"?admin:endpoint.Role=="PUBLIC"?anon:a;
                await Check(tag+"-JSON",endpoint.Method+" "+endpoint.Route+" JSON lỗi","400 không500",async()=>await Status(await client.Request(method,path,new StringContent("{",System.Text.Encoding.UTF8,"application/json"),true),400),kind:"HTTP validation");
                await Check(tag+"-EMPTY",endpoint.Method+" "+endpoint.Route+" thiếu trường","400",async()=>await Status(await client.Request(method,path,JsonContent.Create(new{}),true),400),kind:"HTTP validation");
                await Check(tag+"-NULL",endpoint.Method+" "+endpoint.Route+" null body","400",async()=>await Status(await client.Request(method,path,new StringContent("null",System.Text.Encoding.UTF8,"application/json"),true),400),kind:"HTTP validation");
                await Check(tag+"-MEDIA",endpoint.Method+" "+endpoint.Route+" Content-Type sai","415 không thực thi nghiệp vụ",async()=>await Status(await client.Request(method,path,new StringContent("hello",System.Text.Encoding.UTF8,"text/plain"),true),415),kind:"HTTP validation");
            }
        }
        await Check("API-014","Đầu vào sort/page an toàn","Clamp page/size, từ khóaSQL không trả sai",async()=>{var r=await anon.Read<Page<ProductDto>>("/api/catalog?Page=-1&PageSize=2147483647&Sort=__invalid__&Query="+Uri.EscapeDataString("%'; DROP TABLE SAN_PHAM;--"));Assert(r.PageNumber==1&&r.PageSize==100&&r.Items.Count==0);});
        await Check("API-022","Đo thời gian request trong bộ QA","Có mẫu ms không âm và thống kê được",()=>{Assert(requests.Count>100);return Task.CompletedTask;},kind:"HTTP timing; không phải load test");
        await BoundaryMatrix(endpoints);await FinalAccounts();
        await Check("SEC-008","Rate limit đăng nhập thật","Trong tối đa30 lần sai phải có429",async()=>{bool limited=false;for(int n=0;n<30;n++){using var r=await anon.Send("/api/auth/login",new LoginRequest("missing-qa-user","invalid-password"));if((int)r.StatusCode==429){limited=true;var error=await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();Assert(error.GetProperty("code").GetString()=="RATE_LIMITED");break;}Assert((int)r.StatusCode==401);}Assert(limited);});
    }
    async Task FinalAccounts(){
        await Check("UC01-008","Hết hạn phiên khách tại DB","Cookie cũ bị401",async()=>{await using(var db=Open())await db.Sessions.Where(x=>x.AccountId==bId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ExpiresAt,DateTime.UtcNow.AddMinutes(-1)));await Status(await b.Get("/api/profile"),401);});
        await Check("UC07-003","Khóa/mở đúng tài khoản","Khóa chặn login; mở cho login lại",async()=>{var user=await a.Read<UserDto>("/api/profile");var input=new CustomerInput(user.FullName,user.Phone,user.Email,user.Address,false);await Status(await admin.Send($"/api/admin/customers/{aId}",input,HttpMethod.Put),200);await Status(await anon.Send("/api/auth/login",new LoginRequest("qa-a",customerPassword)),401);await Status(await a.Get("/api/profile"),401);await Status(await admin.Send($"/api/admin/customers/{aId}",input with{Active=true},HttpMethod.Put),200);await a.Login("qa-a",customerPassword);});
        await Check("UC01-011","Đổi mật khẩu cần mật khẩu cũ; thu hồi phiên","Sai mật khẩu cũ400; đổi đúng200; cookie401",async()=>{await Status(await a.Send("/api/auth/password",new PasswordRequest("incorrect-password",customerPassword+"new")),400);await Status(await a.Send("/api/auth/password",new PasswordRequest(customerPassword,customerPassword+"new")),200);await Status(await a.Get("/api/profile"),401);await Status(await anon.Send("/api/auth/login",new LoginRequest("qa-a",customerPassword)),401);await a.Login("qa-a",customerPassword+"new");});
        await Check("UC01-007","Logout thực sự thu hồi phiên","Sau logout cookie không truy cập profile",async()=>{await Status(await a.Send("/api/auth/logout"),200);await Status(await a.Get("/api/profile"),401);});
        await Check("UC05-003","Admin hết hạn phiên tại DB","Cookie Admin bị401",async()=>{using var temp=new QaClient(url,"ADMIN_EXPIRED",requests);await temp.Login("admin",adminPassword,true);await using(var db=Open()){var latest=await db.Sessions.Where(x=>x.AccountId==adminId).OrderByDescending(x=>x.ExpiresAt).FirstAsync();latest.ExpiresAt=DateTime.UtcNow.AddMinutes(-1);await db.SaveChangesAsync();}await Status(await temp.Get("/api/admin/stock"),401);});
        await Check("UC05-002","Admin sai mật khẩu/không tồn tại/bị khóa","401, cookie Admin khóa không dùng được",async()=>{await Status(await anon.Send("/api/auth/login?admin=true",new LoginRequest("admin","invalid-password")),401);await Status(await anon.Send("/api/auth/login?admin=true",new LoginRequest("missing-admin",adminPassword)),401);await using(var db=Open())await db.Accounts.Where(x=>x.Username=="qa-admin2").ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Active,false));try{await Status(await admin2.Get("/api/admin/stock"),401);using var lockedResponse=await anon.Send("/api/auth/login?admin=true",new LoginRequest("qa-admin2",adminPassword));Assert(JsonDocument.Parse(await lockedResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("message").GetString()!.Contains("b\u1ecb kh\u00f3a"),"Locked account message must be distinct");await Status(lockedResponse,401);}finally{await using var db=Open();await db.Accounts.Where(x=>x.Username=="qa-admin2").ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Active,true));}});
        await Check("UC05-006","Admin đổi mật khẩu và thu hồi phiên","200, cookie cũ401; login mật khẩu mới thành công",async()=>{var updated=adminPassword+"x";await Status(await admin.Send("/api/auth/password",new PasswordRequest(adminPassword,updated)),200);await Status(await admin.Get("/api/admin/stock"),401);await admin.Login("admin",updated,true);adminPassword=updated;});
        await Check("UC05-001","Admin logout","Cookie Admin không còn quyền sau logout",async()=>{await Status(await admin.Send("/api/auth/logout"),200);await Status(await admin.Get("/api/admin/stock"),401);});
    }
}





