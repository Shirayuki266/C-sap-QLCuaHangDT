using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Core.Commerce;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class MvcOrdersChecks
{
    private static long[] Rows(string html)=>Regex.Matches(html,"data-order-id=\"(\\d+)\"").Select(m=>long.Parse(m.Groups[1].Value)).ToArray();
    public static async Task Run()
    {
        await using var database=await TestDatabase.Create();var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!File.Exists(Path.Combine(root.FullName,"PhoneStore.slnx")))root=root.Parent;
        var gate=new OrderCancelRaceGate();await using var factory=new MvcFoundationFactory(database.ConnectionString,root!.FullName,extraProbe:gate);
        MvcOrdersFixture data;await using(var scope=factory.Services.CreateAsyncScope()){await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync();data=await MvcOrdersFixture.Seed(scope.ServiceProvider);}
        gate.Accessor=factory.Services.GetRequiredService<IHttpContextAccessor>();
        using var guest=factory.Browser();using var customer=factory.Browser();using var other=factory.Browser();using var admin=factory.Browser();
        async Task<string> Token(HttpClient client,string path="/Orders/Index")
        {using var response=await client.GetAsync(path);response.EnsureSuccessStatusCode();var html=await response.Content.ReadAsStringAsync();return WebUtility.HtmlDecode(Regex.Match(html,"name=\"csrf-token\" content=\"([^\"]+)\"").Groups[1].Value);}
        async Task Login(HttpClient client,string name)
        {using var response=await client.PostAsync("/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{["Username"]=name,["Password"]=MvcCatalogFixture.Password,["ReturnUrl"]="/Orders/Index",["__RequestVerificationToken"]=await Token(client,"/Account/Login")}));Check.True(response.StatusCode==HttpStatusCode.Redirect,"Order test login failed.");}
        async Task<(HttpResponseMessage Response,string Html)> Get(HttpClient client,string path)
        {var response=await client.GetAsync(path);return(response,await response.Content.ReadAsStringAsync());}
        async Task<Dictionary<string,string>> Form(HttpClient client,long id)
        {var(response,html)=await Get(client,"/Orders/Details/"+id);using(response){response.EnsureSuccessStatusCode();var fields=MvcCheckoutChecks.Fields(html);fields["Reason"]="Lý do hủy QA";return fields;}}
        Task<HttpResponseMessage> Post(HttpClient client,Dictionary<string,string> form)=>client.PostAsync("/Orders/Cancel",new FormUrlEncodedContent(form));
        Task<MvcCheckoutSql> Snapshot()=>MvcCheckoutSql.Read(database.ConnectionString);
        void Redirect(HttpResponseMessage response,long id)=>Check.True(response.StatusCode==HttpStatusCode.Redirect&&response.Headers.Location?.OriginalString=="/Orders/Details/"+id,"Cancel did not PRG to details.");
        await Check.Run("WEB.K13.01 own-only order list combined filters UTC7 boundaries and disjoint pagination",async()=>
        {
            using var anonymous=await guest.GetAsync("/Orders/Index");Check.True(anonymous.StatusCode==HttpStatusCode.Redirect&&anonymous.Headers.Location!.OriginalString.StartsWith("/Account/Login?returnUrl="),"Guest order list unprotected.");
            await Login(customer,"catalog-qa-customer");await Login(other,"orders-other-qa");var before=await Snapshot();var expected=before.Orders.Where(x=>x.CustomerId==data.Catalog.Customer.AccountId).OrderByDescending(x=>x.Id).Select(x=>x.Id).ToArray();var seen=new List<long>();
            for(var page=1;page<=6;page++){var(response,html)=await Get(customer,"/Orders/Index?pageSize=2&page="+page+"&customerId="+data.Other.AccountId);using(response){Check.True(response.IsSuccessStatusCode&&response.Headers.CacheControl?.NoStore==true&&html.Contains("data-result-total=\"11\""),"Own list count/cache wrong.");seen.AddRange(Rows(html));}}
            Check.True(seen.SequenceEqual(expected)&&seen.Distinct().Count()==11&&!seen.Contains(data.OtherOrder),"Pagination leaked owner, overlapped or omitted rows.");
            var code=before.Orders.Single(x=>x.Id==data.Created[2]).Code;var filtered=await Get(customer,"/Orders/Index?status=DA_XAC_NHAN&from=2025-01-12&to=2025-01-12&code="+code);using(filtered.Response)Check.True(Rows(filtered.Html).SequenceEqual(new[]{data.Created[2]}),"Combined filters failed.");
            var boundary=await Get(customer,"/Orders/Index?from=2025-01-12&to=2025-01-12");using(boundary.Response)Check.True(Rows(boundary.Html).Order().SequenceEqual(new[]{data.Created[1],data.Created[2]}.Order()),"UTC7 inclusive-day/exclusive-end boundaries incorrect.");
            var foreignCode=before.Orders.Single(x=>x.Id==data.OtherOrder).Code;var hidden=await Get(customer,"/Orders/Index?code="+foreignCode);using(hidden.Response)Check.True(Rows(hidden.Html).Length==0,"Code search leaked foreign order.");
            Check.True(before.Snapshot()==(await Snapshot()).Snapshot(),"List filters changed commerce SQL.");
        });
        await Check.Run("WEB.K13.02 WEB.K13.03 invalid dates/status/pages and no-match are gentle and all commerce snapshots unchanged",async()=>
        {
            var before=await Snapshot();foreach(var query in new[]{"from=2025-01-13&to=2025-01-12","from=bad","status=FAKE","page=0","page=-1","page=abc","page=2147483647&pageSize=100","pageSize=0","pageSize=101"})
            {var(response,html)=await Get(customer,"/Orders/Index?"+query);using(response)Check.True(response.StatusCode==HttpStatusCode.OK&&html.Contains("validation-summary-errors")&&Rows(html).Length==0,"Invalid filter not gentle: "+query);}
            foreach(var query in new[]{"code=no-such-order-qa","page=999"}){var(response,html)=await Get(customer,"/Orders/Index?"+query);using(response)Check.True(response.IsSuccessStatusCode&&Rows(html).Length==0&&WebUtility.HtmlDecode(html).Contains("Chưa có đơn phù hợp"),"No-match not friendly.");}
            foreach(var value in new[]{"<script>alert(1)</script>","' OR 1=1 --"}){var(response,html)=await Get(customer,"/Orders/Index?code="+Uri.EscapeDataString(value));using(response)Check.True(response.IsSuccessStatusCode&&!html.Contains("<script>alert(1)</script>")&&WebUtility.HtmlDecode(html).Contains(value),"Search failed encoding/literal query.");}
            Check.True(before.Snapshot()==(await Snapshot()).Snapshot(),"Reading invalid/empty lists mutated status/completion/payment/H/R.");
        });
        await Check.Run("WEB.K15.02 foreign missing and malformed cancellation IDs return identical404 with zero writes",async()=>
        {
            var form=await Form(customer,data.CancelId);var before=await Snapshot();string? expected=null;
            foreach(var id in new[]{data.OtherOrder,long.MaxValue,0,-1}){using var response=await Post(customer,new(form){["Id"]=id.ToString(),["Reason"]=""});var html=await response.Content.ReadAsStringAsync();Check.True(response.StatusCode==HttpStatusCode.NotFound&&(expected is null||expected==html),"Cancellation404 leaked existence/validation.");expected=html;}
            Check.True(before.Snapshot()==(await Snapshot()).Snapshot(),"Forbidden cancellation changed SQL.");
        });
        await Check.Run("WEB.K15.05 cancellation CSRF missing reason and malformed version preserve form without SQL writes",async()=>
        {
            var form=await Form(customer,data.CancelId);var before=await Snapshot();
            foreach(var token in new[]{"","bad"}){using var response=await Post(customer,new(form){["__RequestVerificationToken"]=token});Check.True(response.StatusCode==HttpStatusCode.BadRequest,"Cancel bypassed antiforgery.");}
            foreach(var reason in new[]{"","   ",new string('x',1001)}){using var response=await Post(customer,new(form){["Reason"]=reason});var html=await response.Content.ReadAsStringAsync();Check.True(response.StatusCode==HttpStatusCode.OK&&html.Contains("data-valmsg-for=\"Reason\"")&&html.Contains("field-validation-error")&&html.Contains("name=\"Version\"")&&html.Contains("value=\""+form["Version"]+"\""),"Reason field error lost cancellation form/version.");}
            using(var response=await Post(customer,new(form){["Version"]="malformed",["Reason"]="Giữ lại lý do QA"})){var html=WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());Check.True(response.StatusCode==HttpStatusCode.OK&&html.Contains("validation-summary-errors")&&html.Contains("Giữ lại lý do QA"),"Invalid version500/lost form.");}
            Check.True(before.Snapshot()==(await Snapshot()).Snapshot(),"Rejected cancel changed commerce SQL.");
        });
        Dictionary<string,string> cancelledForm=[];
        await Check.Run("WEB.K15.01 successful cancellation releases exact R leaves H and unpaid COD unchanged and ignores forged command fields",async()=>
        {
            var reason="  Muốn đổi máy QA <script>alert(1)</script>  ";cancelledForm=await Form(customer,data.CancelId);cancelledForm["Reason"]=reason;cancelledForm["customerId"]=data.Other.AccountId.ToString();cancelledForm["OperationKey"]=Guid.Empty.ToString();cancelledForm["status"]="DA_XAC_NHAN";
            var before=await Snapshot();using var response=await Post(customer,cancelledForm);Redirect(response,data.CancelId);var after=await Snapshot();MvcOrdersAssertions.Cancel(before,after,data.CancelId,data.Catalog.Customer.AccountId,reason);
            var stockBefore=before.Inventory.Single(x=>x.VariantId==data.Catalog.Black128);var stockAfter=after.Inventory.Single(x=>x.VariantId==data.Catalog.Black128);
            var output=Path.Combine(root.FullName,".artifacts","mvc-orders");Directory.CreateDirectory(output);await File.WriteAllTextAsync(Path.Combine(output,"cancel-sql-results.json"),JsonSerializer.Serialize(new{qaOnly=true,status="DA_HUY",beforeH=stockBefore.OnHand,afterH=stockAfter.OnHand,beforeR=stockBefore.Reserved,afterR=stockAfter.Reserved,payment="CHUA_THU",historyDelta=1,journalDelta=1,allOtherRowsUnchanged=true}));
        });
        await Check.Run("WEB.K15.04 stale replay fresh-version repeat and all post-confirmation states reject with identical SQL",async()=>
        {
            var before=await Snapshot();for(var i=0;i<2;i++){using var response=await Post(customer,cancelledForm);Redirect(response,data.CancelId);}
            foreach(var order in before.Orders.Where(x=>x.CustomerId==data.Catalog.Customer.AccountId&&x.Status!="CHO_XAC_NHAN").GroupBy(x=>x.Status).Select(g=>g.First()))
            {using var response=await Post(customer,new(){["Id"]=order.Id.ToString(),["Version"]=Convert.ToBase64String(order.Version),["Reason"]="Không được hủy trạng thái này",["__RequestVerificationToken"]=await Token(customer)});Redirect(response,order.Id);}
            Check.True(before.Snapshot()==(await Snapshot()).Snapshot(),"Repeat/post-confirm cancel released R twice or changed SQL.");
        });
        await Check.Run("WEB.K14.03 WEB.K14.04 timeline all states is actor-free encoded and cancel form exists only pending",async()=>
        {
            var before=await Snapshot();foreach(var order in before.Orders.Where(x=>x.CustomerId==data.Catalog.Customer.AccountId).GroupBy(x=>x.Status).Select(g=>g.First()))
            {
                var(response,html)=await Get(customer,"/Orders/Details/"+order.Id);using(response)
                {
                    Check.True(response.IsSuccessStatusCode&&html.Contains("id=\"cancel-panel\"")==(order.Status=="CHO_XAC_NHAN")&&html.Contains("Lịch sử đơn hàng"),"Pending-only cancel control/timeline failed.");
                    foreach(var forbidden in new[]{"ActorId","OperationKey","catalog-qa-admin","PasswordHash","OnHand","Reserved","Cost"})Check.True(!html.Contains(forbidden,StringComparison.OrdinalIgnoreCase),"Order page leaked internal field/admin identity.");
                    Check.True(!html.Contains("<script>alert(1)</script>"),"Timeline reason not encoded.");
                }
            }
            Check.True(before.Snapshot()==(await Snapshot()).Snapshot(),"Timeline read changed SQL.");
        });
        await Check.Run("WEB.K15.03 true admin API confirmation holds SQL lock while stale MVC cancellation waits then cannot cancel",async()=>
        {
            using(var request=new HttpRequestMessage(HttpMethod.Post,"/api/auth/login"){Content=JsonContent.Create(new LoginRequest("catalog-qa-admin",MvcCatalogFixture.Password,"admin"))}){request.Headers.Add("X-CSRF-TOKEN",await Token(admin,"/Account/Login"));using var login=await admin.SendAsync(request);Check.True(login.IsSuccessStatusCode,"Real admin API login failed.");}
            using var tokenResponse=await admin.GetAsync("/api/security/token");var adminToken=(await tokenResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
            var form=await Form(customer,data.RaceId);using var apiRead=await admin.GetAsync("/api/admin/orders/"+data.RaceId);var apiOrder=(await apiRead.Content.ReadFromJsonAsync<OrderView>())!;
            var before=await Snapshot();gate.Enabled=true;
            using var command=new HttpRequestMessage(HttpMethod.Post,"/api/admin/orders/"+data.RaceId+"/confirm"){Content=JsonContent.Create(new VersionedCommand(apiOrder.Version,Guid.NewGuid()))};command.Headers.Add("X-CSRF-TOKEN",adminToken);
            var confirming=admin.SendAsync(command);var cancelling=Post(customer,form);bool observed=false;
            try
            {
                await gate.Ready.Task.WaitAsync(TimeSpan.FromSeconds(12));Check.True(gate.Arrivals==2&&gate.AdminSpid!=gate.CustomerSpid&&!confirming.IsCompleted&&!cancelling.IsCompleted,"Requests did not overlap.");
                gate.ReleaseAdmin.TrySetResult();await gate.AdminLockHeld.Task.WaitAsync(TimeSpan.FromSeconds(5));gate.ReleaseCustomer.TrySetResult();observed=await gate.ObserveSqlWait(database.ConnectionString);Check.True(observed&&!cancelling.IsCompleted,"No real customer SQL lock wait observed.");
            }
            finally{gate.Unblock();}
            using var confirmed=await confirming;using var stale=await cancelling;gate.Enabled=false;Check.True(confirmed.StatusCode==HttpStatusCode.NoContent,"Real admin confirm failed.");Redirect(stale,data.RaceId);
            var after=await Snapshot();var target=after.Orders.Single(x=>x.Id==data.RaceId);Check.True(target.Status=="DA_XAC_NHAN"&&!target.Version.SequenceEqual(before.Orders.Single(x=>x.Id==data.RaceId).Version)&&after.History.Count==before.History.Count+1&&after.History.Last().ActorId==data.Catalog.Admin.AccountId&&after.History.Last().To=="DA_XAC_NHAN","Race target/history wrong.");
            Check.True(JsonSerializer.Serialize(before.Inventory)==JsonSerializer.Serialize(after.Inventory)&&JsonSerializer.Serialize(before.Payments)==JsonSerializer.Serialize(after.Payments)&&JsonSerializer.Serialize(before.Journal)==JsonSerializer.Serialize(after.Journal)&&JsonSerializer.Serialize(before.Lines)==JsonSerializer.Serialize(after.Lines)&&JsonSerializer.Serialize(before.Cart)==JsonSerializer.Serialize(after.Cart)&&JsonSerializer.Serialize(before.Orders.Where(x=>x.Id!=data.RaceId))==JsonSerializer.Serialize(after.Orders.Where(x=>x.Id!=data.RaceId)),"Stale loser changed protected SQL.");
            var(page,html)=await Get(customer,"/Orders/Details/"+data.RaceId);using(page)Check.True(WebUtility.HtmlDecode(html).Contains("Đơn đã thay đổi, vui lòng tải lại")&&!html.Contains("id=\"cancel-panel\"")&&!html.Contains("catalog-qa-admin"),"Stale message/control/privacy wrong.");
            var output=Path.Combine(root.FullName,".artifacts","mvc-orders");await File.WriteAllTextAsync(Path.Combine(output,"race-results.json"),JsonSerializer.Serialize(new{qaOnly=true,adminTransport="real /api/admin/orders/{id}/confirm",gatedArrivals=2,independentSqlConnections=2,actualSqlLockWaitObserved=observed,adminStatus=204,customerStatus=302,finalState="DA_XAC_NHAN",inventoryUnchanged=true,paymentUnchanged=true,cancelJournalRows=0,historyDelta=1}));
            var unchanged=await Snapshot();using var forbidden=await Post(admin,new(form){["__RequestVerificationToken"]=adminToken!});Check.True(forbidden.StatusCode==HttpStatusCode.Forbidden&&unchanged.Snapshot()==(await Snapshot()).Snapshot(),"Admin cookie entered customer cancel.");
        });
    }
}
