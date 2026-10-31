using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Checks;

internal static class MvcCheckoutChecks
{
    internal static Dictionary<string,string> Fields(string html) => Regex.Matches(html, "<input[^>]*>")
        .Select(match=>match.Value).Where(input=>Regex.IsMatch(input,"type=\"hidden\""))
        .GroupBy(input=>WebUtility.HtmlDecode(Regex.Match(input,"name=\"([^\"]+)\"").Groups[1].Value))
        .ToDictionary(group=>group.Key, group=>WebUtility.HtmlDecode(Regex.Match(group.First(),"value=\"([^\"]*)\"").Groups[1].Value));
    internal static Dictionary<string,string> Shipping(Dictionary<string,string> fields) => new(fields) { ["Recipient"]="Người nhận <script>QA</script>", ["Phone"]="0938123501", ["Address"]="Địa chỉ QA <script>alert(1)</script>", ["PaymentMethod"]="COD" };
    public static async Task Run()
    {
        await using var database=await TestDatabase.Create();
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!File.Exists(Path.Combine(root.FullName,"PhoneStore.slnx")))root=root.Parent;
        var gate=new CheckoutRaceGate();await using var factory=new MvcFoundationFactory(database.ConnectionString,root!.FullName,extraProbe:gate);
        MvcCatalogFixture data;Actor second;
        await using(var scope=factory.Services.CreateAsyncScope())
        {
            var services=scope.ServiceProvider;await services.GetRequiredService<DatabaseMigrator>().MigrateAsync();data=await MvcCatalogFixture.Seed(services);
            await services.GetRequiredService<IAccountRegistration>().RegisterAsync(new("checkout-other-qa",MvcCatalogFixture.Password,"Khách đặt hàng QA khác","0938123598"));
            var session=await services.GetRequiredService<IAccountSessions>().LoginAsync(new("checkout-other-qa",MvcCatalogFixture.Password));second=new(session.Account.Id,session.SessionId);
        }
        using var guest=factory.Browser();using var customer=factory.Browser();using var other=factory.Browser();string oldCookie="";
        async Task Login(HttpClient client,string username="catalog-qa-customer")
        {
            using var form=await client.GetAsync("/Account/Login?returnUrl=%2FCart%2FCheckout");var fields=Fields(await form.Content.ReadAsStringAsync());fields["Username"]=username;fields["Password"]=MvcCatalogFixture.Password;fields["ReturnUrl"]="/Cart/Checkout";
            using var response=await client.PostAsync("/Account/Login",new FormUrlEncodedContent(fields));Check.True(response.StatusCode==HttpStatusCode.Redirect&&response.Headers.Location!.OriginalString=="/Cart/Checkout","Checkout login return lost.");
            if(username=="catalog-qa-customer")oldCookie=response.Headers.GetValues("Set-Cookie").Single(x=>x.StartsWith("PhoneStore.Session=")).Split(';')[0];
        }
        async Task SetCart(Actor actor,long variant,int quantity)
        {
            await using var scope=factory.Services.CreateAsyncScope();var store=scope.ServiceProvider.GetRequiredService<IStoreService>();
            foreach(var line in (await store.ReadCartAsync(actor)).Lines)await store.RemoveCartAsync(actor,line.VariantId);
            if(quantity>0)await store.UpdateCartAsync(actor,variant,quantity);
        }
        async Task<(string Html,Dictionary<string,string> Form)> Checkout(HttpClient client)
        {using var response=await client.GetAsync("/Cart/Checkout");response.EnsureSuccessStatusCode();var html=await response.Content.ReadAsStringAsync();return(html,Shipping(Fields(html)));}
        Task<HttpResponseMessage> Post(HttpClient client,Dictionary<string,string> fields)=>client.PostAsync("/Cart/Checkout",new FormUrlEncodedContent(fields));
        Task<MvcCheckoutSql> Snapshot()=>MvcCheckoutSql.Read(database.ConnectionString);
        static long OrderId(HttpResponseMessage response)
        {Check.True(response.StatusCode==HttpStatusCode.Redirect&&Regex.IsMatch(response.Headers.Location?.OriginalString??"","^/Orders/Details/[0-9]+$"),"Checkout success did not PRG to receipt.");return long.Parse(response.Headers.Location!.OriginalString.Split('/').Last());}
        await Check.Run("WEB.K12.01 guest challenge empty redirect fresh GUID quote and editable profile defaults are read-only",async()=>
        {
            var before=await Snapshot();using var anonymous=await guest.GetAsync("/Cart/Checkout");Check.True(anonymous.StatusCode==HttpStatusCode.Redirect&&anonymous.Headers.Location!.OriginalString.StartsWith("/Account/Login?returnUrl="),"Guest checkout not protected.");
            await Login(customer);await Login(other,"checkout-other-qa");using(var empty=await customer.GetAsync("/Cart/Checkout"))Check.True(empty.StatusCode==HttpStatusCode.Redirect&&empty.Headers.Location!.OriginalString=="/Cart/Index","Empty checkout not redirected.");
            Check.True(before.Snapshot()==(await Snapshot()).Snapshot(),"GET/login changed commerce SQL.");await SetCart(data.Customer,data.Black128,2);await SetCart(second,data.Black128,1);
            before=await Snapshot();var first=await Checkout(customer);var next=await Checkout(customer);
            Check.True(Guid.TryParse(first.Form["RequestKey"],out var key)&&key!=Guid.Empty&&first.Form["RequestKey"]!=next.Form["RequestKey"]&&first.Form["Confirmation"].Length>0&&
                WebUtility.HtmlDecode(first.Html).Contains("Khách fixture QA")&&first.Html.Contains("0938123501")&&before.Snapshot()==(await Snapshot()).Snapshot(),"Fresh GUID/default/quote GET changed SQL.");
        });
        await Check.Run("WEB.K12.07 WEB.K12.08 CSRF tampered quote COD-only and field validation keep safe values without SQL effects",async()=>
        {
            var page=await Checkout(customer);var before=await Snapshot();
            foreach(var invalid in new[]{"","wrong"}){var fields=new Dictionary<string,string>(page.Form){["__RequestVerificationToken"]=invalid};using var response=await Post(customer,fields);Check.True(response.StatusCode==HttpStatusCode.BadRequest,"Checkout bypassed CSRF.");}
            foreach(var field in new[]{"Recipient","Phone","Address"})
            {
                var fields=new Dictionary<string,string>(page.Form){[field]=""};using var response=await Post(customer,fields);var html=WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
                Check.True(response.StatusCode==HttpStatusCode.OK&&html.Contains("data-valmsg-for=\""+field+"\"")&&html.Contains("field-validation-error")&&html.Contains(field=="Recipient"?page.Form["Address"]:page.Form["Recipient"]),"Field error lost attempted values: "+field);
            }
            foreach(var method in new[]{"QR","cod",""}){using var response=await Post(customer,new(page.Form){["PaymentMethod"]=method});Check.True(response.StatusCode==HttpStatusCode.OK&&WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()).Contains("Chỉ hỗ trợ thanh toán COD"),"Non-COD request reached checkout.");}
            foreach(var fields in new[]{new Dictionary<string,string>(page.Form){["Confirmation"]="forged"},new Dictionary<string,string>(page.Form){["RequestKey"]=Guid.NewGuid().ToString()}})
            {using var response=await Post(customer,fields);Check.True(response.StatusCode==HttpStatusCode.OK&&WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()).Contains("Phiên xác nhận không hợp lệ"),"Protected quote/key tampering accepted.");}
            var foreign=await Checkout(other);using(var response=await Post(other,new(page.Form){["__RequestVerificationToken"]=foreign.Form["__RequestVerificationToken"]}))Check.True(response.StatusCode==HttpStatusCode.OK&&WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()).Contains("Phiên xác nhận không hợp lệ"),"Another customer reused protected quote.");
            Check.True(before.Snapshot()==(await Snapshot()).Snapshot(),"Rejected checkout changed cart/H/R/order/payment/history/journal.");
        });
        long created=0;Dictionary<string,string> original=[];
        await Check.Run("WEB.K12.02 WEB.K12.07 successful COD ignores forged money identity/status and commits exact SQL deltas",async()=>
        {
            var page=await Checkout(customer);original=new(page.Form){["total"]="1",["price"]="1",["paymentStatus"]="DA_THU",["customerId"]=second.AccountId.ToString(),["Lines[0].Price"]="1",["role"]="ADMIN"};
            var before=await Snapshot();using var response=await Post(customer,original);created=OrderId(response);var after=await Snapshot();
            Check.True(MvcCheckoutSql.AssertOne(before,after,Guid.Parse(original["RequestKey"]),data.Customer.AccountId,data.Black128,2,12000000m)==created,"Wrong receipt ID.");
            using var receipt=await customer.GetAsync(response.Headers.Location);var html=await receipt.Content.ReadAsStringAsync();
            Check.True(receipt.IsSuccessStatusCode&&receipt.Headers.CacheControl?.NoStore==true&&WebUtility.HtmlDecode(html).Contains("Chờ xác nhận")&&WebUtility.HtmlDecode(html).Contains("Chưa thu tiền")&&html.Contains("COD")&&WebUtility.HtmlDecode(html).Contains("24.000.000 đ")&&!html.Contains("<script>QA</script>")&&!html.Contains("<script>alert(1)</script>"),"Receipt omitted states/server total or failed encoding.");
            foreach(var field in new[]{"OnHand","Reserved","Cost","ActorId","OperationKey","PasswordHash","catalog-qa-admin","customerId","RequestKey"})Check.True(!html.Contains(field,StringComparison.OrdinalIgnoreCase),"Receipt leaked forbidden data.");
        });
        await Check.Run("WEB.K12.05 concurrent replay and reload return same order with identical commerce SQL",async()=>
        {
            var before=await Snapshot();var replies=await Task.WhenAll(Post(customer,original),Post(customer,original));
            foreach(var response in replies){using(response)Check.True(OrderId(response)==created,"Repeated request created a different order.");}
            for(var i=0;i<2;i++){using var read=await customer.GetAsync("/Orders/Details/"+created);Check.True(read.IsSuccessStatusCode,"Receipt reload failed.");}
            Check.True(before.Snapshot()==(await Snapshot()).Snapshot(),"Replay/F5 changed H/R/cart/order/payment/history/journal.");
        });
        await Check.Run("WEB.K14.01 foreign and missing order return identical404 HTML and no SQL writes",async()=>
        {
            var before=await Snapshot();using var foreign=await other.GetAsync("/Orders/Details/"+created);using var missing=await other.GetAsync("/Orders/Details/"+long.MaxValue);
            Check.True(foreign.StatusCode==HttpStatusCode.NotFound&&missing.StatusCode==HttpStatusCode.NotFound&&await foreign.Content.ReadAsStringAsync()==await missing.Content.ReadAsStringAsync()&&before.Snapshot()==(await Snapshot()).Snapshot(),"Ownership404 leaks existence or writes SQL.");
        });
        await Check.Run("WEB.K14.02 catalog/profile edits cannot change receipt snapshots or timeline",async()=>
        {
            CustomerOrderSnapshot receiptBefore;await using(var scope=factory.Services.CreateAsyncScope())receiptBefore=await scope.ServiceProvider.GetRequiredService<IStoreService>().ReadCustomerOrderAsync(data.Customer,created);
            var sqlBefore=await Snapshot();
            await using(var db=MvcCheckoutSql.Open(database.ConnectionString))
            {await db.Products.Where(x=>x.Id==data.MainId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Name,"Tên mới QA"));await db.Variants.Where(x=>x.Id==data.Black128).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Price,13000000m).SetProperty(x=>x.Color,"Màu mới QA"));await db.Customers.Where(x=>x.Id==data.Customer.AccountId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.FullName,"Hồ sơ mới QA").SetProperty(x=>x.Address,"Địa chỉ hồ sơ mới QA"));}
            using var again=await customer.GetAsync("/Orders/Details/"+created);var body=await again.Content.ReadAsStringAsync();
            CustomerOrderSnapshot receiptAfter;await using(var scope=factory.Services.CreateAsyncScope())receiptAfter=await scope.ServiceProvider.GetRequiredService<IStoreService>().ReadCustomerOrderAsync(data.Customer,created);
            Check.True(JsonSerializer.Serialize(receiptBefore)==JsonSerializer.Serialize(receiptAfter)&&body.Contains("QA Aurora One")&&!body.Contains("Tên mới QA")&&!body.Contains("Hồ sơ mới QA")&&!body.Contains("Màu mới QA")&&body.Contains("12.000.000")&&WebUtility.HtmlDecode(body).Contains(original["Address"])&&sqlBefore.Snapshot()==(await Snapshot()).Snapshot(),"Receipt snapshot/timeline read changed or followed mutable catalog/profile.");
            await using(var db=MvcCheckoutSql.Open(database.ConnectionString)){await db.Products.Where(x=>x.Id==data.MainId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Name,"QA Aurora One"));await db.Variants.Where(x=>x.Id==data.Black128).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Price,12000000m).SetProperty(x=>x.Color,"Đen"));}
        });
        await Check.Run("WEB.K12.03 changed price returns fresh protected confirmation without reservation then explicit retry orders new price",async()=>
        {
            await SetCart(data.Customer,data.Black256,2);var page=await Checkout(customer);
            await using(var db=MvcCheckoutSql.Open(database.ConnectionString))await db.Variants.Where(x=>x.Id==data.Black256).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Price,25000000m));
            var before=await Snapshot();using var changed=await Post(customer,page.Form);var html=await changed.Content.ReadAsStringAsync();
            Check.True(changed.StatusCode==HttpStatusCode.OK&&WebUtility.HtmlDecode(html).Contains("Chưa tạo đơn")&&WebUtility.HtmlDecode(html).Contains("50.000.000 đ")&&html.Contains("value=\""+page.Form["RequestKey"]+"\"")&&before.Snapshot()==(await Snapshot()).Snapshot(),"Reprice created an order/reservation or omitted fresh price.");
            var revised=Shipping(Fields(html));Check.True(revised["Confirmation"]!=page.Form["Confirmation"],"Reprice kept obsolete quote token.");using var confirmed=await Post(customer,revised);var id=OrderId(confirmed);Check.True(MvcCheckoutSql.AssertOne(before,await Snapshot(),Guid.Parse(revised["RequestKey"]),data.Customer.AccountId,data.Black256,2,25000000m)==id,"New price confirmation not authoritative.");
            await using(var db=MvcCheckoutSql.Open(database.ConnectionString))await db.Variants.Where(x=>x.Id==data.Black256).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Price,24000000m));
        });
        await Check.Run("WEB.K12.04 empty inactive and shortage branches retain form/line feedback and leave no partial SQL",async()=>
        {
            foreach(var scenario in new[]{"empty","inactive","shortage"})
            {
                await SetCart(data.Customer,data.Black128,1);var page=await Checkout(customer);int oldOnHand=0;
                await using(var db=MvcCheckoutSql.Open(database.ConnectionString))
                {if(scenario=="empty")await db.Cart.Where(x=>x.CustomerId==data.Customer.AccountId).ExecuteDeleteAsync();if(scenario=="inactive")await db.Products.Where(x=>x.Id==data.MainId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Active,false));if(scenario=="shortage"){var stock=await db.Inventory.SingleAsync(x=>x.VariantId==data.Black128);oldOnHand=stock.OnHand;stock.OnHand=stock.Reserved;await db.SaveChangesAsync();}}
                var before=await Snapshot();using var response=await Post(customer,page.Form);var html=WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
                Check.True(response.StatusCode==HttpStatusCode.OK&&html.Contains("validation-summary-errors")&&!html.Contains("Đặt hàng COD thành công")&&html.Contains(page.Form["Recipient"])&&before.Snapshot()==(await Snapshot()).Snapshot(),"Failed checkout left partial SQL or lost form: "+scenario);
                if(scenario!="empty")Check.True(html.Contains("data-variant-id=\""+data.Black128+"\"")&&html.Contains(scenario=="inactive"?"không còn bán":"Thiếu hàng"),"Failure lacks affected line.");
                await using(var db=MvcCheckoutSql.Open(database.ConnectionString)){if(scenario=="inactive")await db.Products.Where(x=>x.Id==data.MainId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Active,true));if(scenario=="shortage")await db.Inventory.Where(x=>x.VariantId==data.Black128).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.OnHand,oldOnHand));}
            }
        });
        await Check.Run("WEB.K12.04 forced late payment constraint failure rolls back H/R cart order lines payment history and journal",async()=>
        {
            await SetCart(data.Customer,data.Black128,1);var page=await Checkout(customer);var before=await Snapshot();
            await using var db=MvcCheckoutSql.Open(database.ConnectionString);var max=before.Orders.Max(x=>x.Id).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var faultSql="ALTER TABLE dbo.THANH_TOAN ADD CONSTRAINT CK_MvcCheckout_QA_Failure CHECK (OrderId<="+max+")";await db.Database.ExecuteSqlRawAsync(faultSql);
            try{using var response=await Post(customer,page.Form);Check.True(response.StatusCode==HttpStatusCode.OK&&(await response.Content.ReadAsStringAsync()).Contains("validation-summary-errors")&&before.Snapshot()==(await Snapshot()).Snapshot(),"Late failure exposed success or partial order/reservation.");}
            finally{await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.THANH_TOAN DROP CONSTRAINT CK_MvcCheckout_QA_Failure");}
        });
        await Check.Run("WEB.K12.09 expired checkout session cannot order and relogin returns with persisted server cart",async()=>
        {
            var page=await Checkout(customer);using var stale=factory.Browser();stale.DefaultRequestHeaders.Add("Cookie",oldCookie);
            await using(var db=MvcCheckoutSql.Open(database.ConnectionString))await db.Sessions.Where(x=>x.AccountId==data.Customer.AccountId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ExpiresAt,DateTime.UtcNow.AddMinutes(-1)));
            var before=await Snapshot();using(var expired=await Post(customer,page.Form))Check.True(expired.StatusCode==HttpStatusCode.Redirect&&expired.Headers.Location!.OriginalString.Contains("/Account/Login?returnUrl="),"Expired checkout not challenged.");
            await Login(customer);var resumed=await Checkout(customer);Check.True(resumed.Html.Contains("data-variant-id=\""+data.Black128+"\"")&&resumed.Form["RequestKey"]!=page.Form["RequestKey"],"Relogin lost cart or reused GET key.");
            using var oldAttempt=await Post(stale,page.Form);Check.True(oldAttempt.StatusCode==HttpStatusCode.Redirect&&oldAttempt.Headers.Location!.OriginalString.Contains("/Account/Login")&&before.Snapshot()==(await Snapshot()).Snapshot(),"Old expired cookie created order after relogin.");
        });
        await Check.Run("WEB.K12.06 gated independent HTTP buyers overlap at SQL write lock and only one reserves final unit",async()=>
        {
            Actor freshActor;await using(var scope=factory.Services.CreateAsyncScope()){var fresh=await scope.ServiceProvider.GetRequiredService<IAccountSessions>().LoginAsync(new("catalog-qa-customer",MvcCatalogFixture.Password));freshActor=new(fresh.Account.Id,fresh.SessionId);}
            await SetCart(freshActor,data.Black256,1);await SetCart(second,data.Black256,1);
            await using(var db=MvcCheckoutSql.Open(database.ConnectionString))await db.Inventory.Where(x=>x.VariantId==data.Black256).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.OnHand,x=>x.Reserved+1));
            var a=await Checkout(customer);var b=await Checkout(other);var before=await Snapshot();gate.Enabled=true;
            var first=Post(customer,a.Form);var next=Post(other,b.Form);
            try{await gate.Ready.Task.WaitAsync(TimeSpan.FromSeconds(12));Check.True(gate.Arrivals==2&&gate.Connections.Count==2&&!first.IsCompleted&&!next.IsCompleted,"Requests did not overlap at real SQL gate.");}
            finally{gate.Release.TrySetResult();}
            var replies=await Task.WhenAll(first,next);gate.Enabled=false;
            try
            {
                Check.True(replies.Count(x=>x.StatusCode==HttpStatusCode.Redirect)==1&&replies.Count(x=>x.StatusCode==HttpStatusCode.OK)==1,"Last-unit race did not have exactly one winner.");
                var winner=replies[0].StatusCode==HttpStatusCode.Redirect?0:1;var loser=1-winner;var winnerForm=winner==0?a.Form:b.Form;var winnerCustomer=winner==0?data.Customer.AccountId:second.AccountId;
                var after=await Snapshot();var id=OrderId(replies[winner]);Check.True(MvcCheckoutSql.AssertOne(before,after,Guid.Parse(winnerForm["RequestKey"]),winnerCustomer,data.Black256,1,24000000m)==id,"Race SQL delta incorrect.");
                var loserHtml=WebUtility.HtmlDecode(await replies[loser].Content.ReadAsStringAsync());Check.True(loserHtml.Contains("Thiếu hàng")&&after.Cart.Any(x=>x.CustomerId!=(winnerCustomer)&&x.VariantId==data.Black256&&x.Quantity==1),"Losing cart was cleared or stock error hidden.");
                var output=Path.Combine(root.FullName,".artifacts","mvc-checkout");Directory.CreateDirectory(output);var oldStock=before.Inventory.Single(x=>x.VariantId==data.Black256);var stock=after.Inventory.Single(x=>x.VariantId==data.Black256);
                await File.WriteAllTextAsync(Path.Combine(output,"race-results.json"),JsonSerializer.Serialize(new{qaOnly=true,gatedArrivals=gate.Arrivals,independentSqlConnections=gate.Connections.Count,overlapProved=true,winners=1,losers=1,beforeH=oldStock.OnHand,beforeR=oldStock.Reserved,afterH=stock.OnHand,afterR=stock.Reserved,ordersDelta=1,paymentsDelta=1,historyDelta=1,journalDelta=1,loserCartPreserved=true}));
            }
            finally{foreach(var response in replies)response.Dispose();}
        });
    }
}
