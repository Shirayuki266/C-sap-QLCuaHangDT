using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;

internal static class RemainingP0Checks
{
    // Snapshots remain in memory; assertions never print profiles, credentials or cookies.
    private static async Task<string> Business(CommerceContext c)
    {
        await using var db=c.Open();
        return JsonSerializer.Serialize(new{
            Accounts=await db.Accounts.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Username,x.Role,x.CreatedAt}).ToListAsync(),
            Profiles=await db.Customers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            Cart=await db.Cart.AsNoTracking().OrderBy(x=>x.CustomerId).ThenBy(x=>x.VariantId).ToListAsync(),
            Orders=await db.Orders.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            Lines=await db.OrderLines.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            Payments=await db.Payments.AsNoTracking().OrderBy(x=>x.OrderId).ToListAsync(),
            Stock=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync(),
            History=await db.OrderHistory.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            Movements=await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            Purchases=await db.Purchases.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            PurchaseLines=await db.PurchaseLines.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
    }
    private static async Task Login(CommerceContext c,ApiTestClient client,bool admin)
    {
        await using var db=c.Open();var id=admin?c.Admin.AccountId:c.Customer.AccountId;
        var name=await db.Accounts.Where(x=>x.Id==id).Select(x=>x.Username).SingleAsync();
        using var response=await client.Send("/api/auth/login",new LoginRequest(name,CommerceContext.Password,admin?"admin":"customer"));
        Check.True(response.StatusCode==HttpStatusCode.OK,"P0 fixture login failed.");
    }
    private static async Task Replay(Uri address,string cookie,HttpStatusCode expected)
    {
        using var client=new ApiTestClient(address);client.Replay(cookie);using var result=await client.Http.GetAsync("/api/auth/session");
        Check.True(result.StatusCode==expected,"Captured session has unexpected server validity.");
    }
    private sealed class LoseReply(HttpMessageHandler inner):DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            using var response=await base.SendAsync(request,token);
            Check.True(response.StatusCode==HttpStatusCode.NoContent,"Logout failed before reply-loss injection.");
            throw new HttpRequestException("Simulated loss of committed logout response.");
        }
    }
    private static async Task Offline(string route="logout")
    {
        using var socket=new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback,0));var port=((IPEndPoint)socket.LocalEndPoint!).Port;
        using var http=new HttpClient(new HttpClientHandler{UseProxy=false}){Timeout=TimeSpan.FromSeconds(2)};
        try{using var reply=await http.PostAsJsonAsync($"http://127.0.0.1:{port}/api/auth/{route}",new{});}
        catch(HttpRequestException){return;}
        catch(TaskCanceledException){return;}
        throw new InvalidOperationException("Offline request unexpectedly reached a server.");
    }
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"remaining-p0");using var other=new CommerceContext(connection,address,"remaining-p0-other");
        await c.Initialize();await other.Initialize();var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),200);
        async Task<OrderView> Create(string state="CHO_XAC_NHAN")
        {
            var input=await CheckoutChecks.Cart(c,data);await using var db=c.Open();var k=c.Kernel(db);var order=await new CheckoutService(k).Place(c.Customer,input);
            var flow=new OrderWorkflow(k);var query=new OrderQueryService(k);
            if(state=="DA_HUY")await flow.Cancel(c.Admin,order.Id,new(order.Version,Guid.NewGuid(),"Fixture cancellation"),true);
            else if(state!="CHO_XAC_NHAN")
            {
                await flow.Confirm(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));order=await query.Read(c.Admin,order.Id,true);
                if(state!="DA_XAC_NHAN")
                {
                    await flow.Dispatch(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));order=await query.Read(c.Admin,order.Id,true);
                    if(state=="HOAN_TAT")await flow.Complete(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));
                    if(state=="GIAO_THAT_BAI")await flow.Fail(c.Admin,order.Id,new(order.Version,Guid.NewGuid(),"Fixture failure"));
                }
            }
            return await query.Read(c.Admin,order.Id,true);
        }
        await Create();await using(var db=c.Open())await new CartService(c.Kernel(db)).Set(c.Customer,new(data.Product.Variants[0].Id,1));
        await Check.Run("K03.04 CUSTOMER logout preserves profiles carts orders payments inventory and history",async()=>
        {
            var before=await Business(c);var cookie=c.CustomerClient.SessionCookie();await using var db=c.Open();var sessions=await db.Sessions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync();
            using var result=await c.CustomerClient.Send("/api/auth/logout",new{});Check.True(result.StatusCode==HttpStatusCode.NoContent,"Customer logout failed.");
            await Replay(address,cookie,HttpStatusCode.Unauthorized);
            var after=await db.Sessions.AsNoTracking().ToListAsync();Check.True(after.Count==sessions.Count-1&&after.All(x=>sessions.Any(y=>y.Id==x.Id&&y.AccountId==x.AccountId&&y.ExpiresAt==x.ExpiresAt)),"Logout altered unrelated sessions.");
            Check.True(before==await Business(c),"Customer logout changed business data.");await Login(c,c.CustomerClient,false);
        });
        await Check.Run("A02.04 ADMIN logout revokes only its session and preserves all business records",async()=>
        {
            using var second=new ApiTestClient(address);await Login(c,second,true);var secondCookie=second.SessionCookie();var foreignCookie=other.AdminClient.SessionCookie();
            var before=await Business(c);var cookie=c.AdminClient.SessionCookie();await using var db=c.Open();var count=await db.Sessions.CountAsync();
            using var result=await c.AdminClient.Send("/api/auth/logout",new{});Check.True(result.StatusCode==HttpStatusCode.NoContent,"ADMIN logout failed.");
            await Replay(address,cookie,HttpStatusCode.Unauthorized);await Replay(address,secondCookie,HttpStatusCode.OK);await Replay(address,foreignCookie,HttpStatusCode.OK);
            Check.True(await db.Sessions.CountAsync()==count-1&&before==await Business(c),"ADMIN logout changed business data or foreign sessions.");await Login(c,c.AdminClient,true);
        });
        await Check.Run("A11.04 locking a customer with held orders preserves business data and allows ADMIN management",async()=>
        {
            var order=await Create();var before=await Business(c);var cookie=c.CustomerClient.SessionCookie();
            using(var locked=await c.AdminClient.Send($"/api/admin/customers/{c.Customer.AccountId}/state",new{active=false,expectedActive=true}))Check.True(locked.StatusCode==HttpStatusCode.NoContent,"Lock failed.");
            await Replay(address,cookie,HttpStatusCode.Unauthorized);
            await using(var db=c.Open())Check.True(!await db.Sessions.AnyAsync(x=>x.AccountId==c.Customer.AccountId),"Lock left a customer session.");
            Check.True(before==await Business(c),"Lock changed pending orders, holds or customer data.");
            using(var unlocked=await c.AdminClient.Send($"/api/admin/customers/{c.Customer.AccountId}/state",new{active=true,expectedActive=false}))Check.True(unlocked.StatusCode==HttpStatusCode.NoContent,"Unlock failed.");
            Check.True(before==await Business(c),"Unlock changed business data.");await Replay(address,cookie,HttpStatusCode.Unauthorized);await Login(c,c.CustomerClient,false);await c.RefreshCustomerSession();
            await using var d=c.Open();await new OrderWorkflow(c.Kernel(d)).Confirm(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));
            Check.True((await new OrderQueryService(c.Kernel(d)).Read(c.Admin,order.Id,true)).Status=="DA_XAC_NHAN","Retained held order cannot be managed.");
        });
        await Check.Run("A11.05 missing invalid and stale customer state requests are rejected atomically",async()=>
        {
            var before=await Business(c);await using var db=c.Open();var sessions=await db.Sessions.CountAsync();
            foreach(var item in new[]{(long.MaxValue,false,true,HttpStatusCode.NotFound),(c.Admin.AccountId,false,true,HttpStatusCode.NotFound),(c.Customer.AccountId,false,false,HttpStatusCode.Conflict),(c.Customer.AccountId,true,true,HttpStatusCode.Conflict)})
            {using var r=await c.AdminClient.Send($"/api/admin/customers/{item.Item1}/state",new{active=item.Item2,expectedActive=item.Item3});Check.True(r.StatusCode==item.Item4,"Invalid state guard returned unexpected status.");}
            using var missing=await c.AdminClient.Send($"/api/admin/customers/{c.Customer.AccountId}/state",new{active=false});Check.True(missing.StatusCode==HttpStatusCode.BadRequest,"Missing expectedActive accepted.");
            Check.True(await db.Accounts.Where(x=>x.Id==c.Customer.AccountId).AllAsync(x=>x.Active)&&await db.Sessions.CountAsync()==sessions&&before==await Business(c),"Rejected state request mutated data.");
        });
        await Check.Run("RACE.09 separate phone and email registration and profile races have one winner and no partial loser",async()=>
        {
            for(var mode=0;mode<2;mode++)
            {
                var tag="p0-race-"+mode;var phone="097700000"+mode;var email=tag+"@example.test";
                async Task<bool> Register(int n)
                {
                    await using var db=c.Open();try{await new AccountRegistration(db,new PasswordHasher<PhoneStore.Infrastructure.Data.Account>()).RegisterAsync(new(tag+"-"+n,CommerceContext.Password,"Race registration",mode==0?phone:"097710000"+n,Email:mode==1?email:tag+n+"@example.test"));return true;}catch(BusinessException e)when(e.Code=="INVALID_INPUT"){return false;}
                }
                var results=await Task.WhenAll(Register(0),Register(1));await using var check=c.Open();
                Check.True(results.Count(x=>x)==1&&await check.Accounts.CountAsync(x=>x.Username.StartsWith(tag+"-"))==1&&await check.Customers.CountAsync(x=>mode==0?x.Phone==phone:x.Email==email)==1,"Registration race duplicated identity or left orphan account.");
                var first=await check.Customers.AsNoTracking().SingleAsync(x=>x.Id==c.Customer.AccountId);var second=await check.Customers.AsNoTracking().SingleAsync(x=>x.Id==other.Customer.AccountId);
                var claimPhone="097720000"+mode;var claimEmail="profile-race-"+mode+"@example.test";
                async Task<bool> Update(CommerceContext owner,PhoneStore.Infrastructure.Data.Customer old,int n)
                {
                    using var response=await owner.CustomerClient.Send("/api/account/profile",new ProfileChangeRequest("Race profile "+n,mode==0?claimPhone:old.Phone,mode==1?claimEmail:old.Email,old.Address),HttpMethod.Put);
                    Check.True(response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.BadRequest,"Profile race returned unexpected error.");return response.StatusCode==HttpStatusCode.NoContent;
                }
                var updates=await Task.WhenAll(Update(c,first,0),Update(other,second,1));var loser=updates[0]?second:first;
                var persisted=await check.Customers.AsNoTracking().SingleAsync(x=>x.Id==loser.Id);
                Check.True(updates.Count(x=>x)==1&&JsonSerializer.Serialize(loser)==JsonSerializer.Serialize(persisted)&&await check.Customers.CountAsync(x=>mode==0?x.Phone==claimPhone:x.Email==claimEmail)==1,"Profile race duplicated identity or partially changed loser.");
            }
        });
        await Check.Run("A20.05 full six-state seven-action matrix rejects wrong order stale versions missing reasons and repeats",async()=>
        {
            var states=new[]{"CHO_XAC_NHAN","DA_XAC_NHAN","DANG_GIAO","HOAN_TAT","DA_HUY","GIAO_THAT_BAI"};
            var actions=new[]{"customer-cancel","confirm","cancel","dispatch","complete","fail","receive-return"};
            async Task Apply(OrderView order,string action,VersionedCommand command)
            {
                await using var db=c.Open();var flow=new OrderWorkflow(c.Kernel(db));
                switch(action){case "customer-cancel":await flow.Cancel(c.Customer,order.Id,command);break;case "confirm":await flow.Confirm(c.Admin,order.Id,command);break;case "cancel":await flow.Cancel(c.Admin,order.Id,command,true);break;case "dispatch":await flow.Dispatch(c.Admin,order.Id,command);break;case "complete":await flow.Complete(c.Admin,order.Id,command);break;case "fail":await flow.Fail(c.Admin,order.Id,command);break;case "receive-return":await flow.ReceiveReturn(c.Admin,order.Id,command);break;}
            }
            foreach(var state in states)foreach(var action in actions)
            {
                var order=await Create(state);var allowed=action switch{"customer-cancel" or "confirm"=>state==states[0],"cancel"=>state==states[0]||state==states[1],"dispatch"=>state==states[1],"complete" or "fail"=>state==states[2],_=>state==states[5]};
                var before=await Business(c);var command=new VersionedCommand(order.Version,Guid.NewGuid(),"Matrix reason");
                if(!allowed){await CommerceContext.Reject("INVALID_STATE",()=>Apply(order,action,command));Check.True(before==await Business(c),"Forbidden matrix action had side effects.");continue;}
                await using var db=c.Open();var ids=order.Lines.Select(x=>x.VariantId).ToArray();var stock=await db.Inventory.AsNoTracking().Where(x=>ids.Contains(x.VariantId)).ToListAsync();var history=await db.OrderHistory.CountAsync(x=>x.OrderId==order.Id);var movements=await db.Movements.CountAsync(x=>x.OrderId==order.Id);
                await Apply(order,action,command);var result=await new OrderQueryService(c.Kernel(db)).Read(c.Admin,order.Id,true);var payment=await db.Payments.AsNoTracking().SingleAsync(x=>x.OrderId==order.Id);
                var expected=action switch{"customer-cancel" or "cancel"=>"DA_HUY","confirm"=>"DA_XAC_NHAN","dispatch"=>"DANG_GIAO","complete"=>"HOAN_TAT",_=>"GIAO_THAT_BAI"};
                Check.True(result.Status==expected&&result.Version!=order.Version&&await db.OrderHistory.CountAsync(x=>x.OrderId==order.Id)==history+1&&payment.Status==(action=="complete"?"DA_THU":"CHUA_THU")&&payment.Amount==(action=="complete"?order.Total:0),"Allowed matrix transition/payment/history wrong.");
                if(action=="complete")Check.True(payment.CollectedAt==result.CompletedAt!.Value.UtcDateTime,"COD timestamp differs from completion.");
                var changes=action is "customer-cancel" or "cancel" or "dispatch" or "receive-return";
                Check.True(await db.Movements.CountAsync(x=>x.OrderId==order.Id)==movements+(changes?order.Lines.Count:0),"Matrix movement count wrong.");
                foreach(var line in order.Lines){var old=stock.Single(x=>x.VariantId==line.VariantId);var now=await db.Inventory.AsNoTracking().SingleAsync(x=>x.VariantId==line.VariantId);Check.True(now.OnHand==old.OnHand+(action=="dispatch"?-line.Quantity:action=="receive-return"?line.Quantity:0)&&now.Reserved==old.Reserved+(action is "customer-cancel" or "cancel" or "dispatch"?-line.Quantity:0),"Matrix stock deltas wrong.");}
                var committed=await Business(c);await CommerceContext.Reject("CONCURRENT_UPDATE",()=>Apply(order,action,command));Check.True(committed==await Business(c),"Stale retry repeated effect.");
                await CommerceContext.Reject("INVALID_STATE",()=>Apply(result,action,new(result.Version,Guid.NewGuid(),"Repeat")));Check.True(committed==await Business(c),"Fresh-version repeat repeated effect.");
            }
            foreach(var item in new[]{("cancel","CHO_XAC_NHAN"),("fail","DANG_GIAO")})
            {var order=await Create(item.Item2);var before=await Business(c);await CommerceContext.Reject("INVALID_INPUT",()=>Apply(order,item.Item1,new(order.Version,Guid.NewGuid())));Check.True(before==await Business(c),"Missing reason mutated business data.");}
            var unknown=await Create();var snapshot=await Business(c);using var invalid=await c.AdminClient.Send($"/api/admin/orders/{unknown.Id}/unknown",new VersionedCommand(unknown.Version,Guid.NewGuid()));Check.True(invalid.StatusCode==HttpStatusCode.NotFound&&snapshot==await Business(c),"Unknown transition accepted or mutated data.");
        });
        await Check.Run("A01.04 SQL/network login failures create no false success or session; ADMIN lifetime is eight hours",async()=>
        {
            await using var db=c.Open();var account=await db.Accounts.AsNoTracking().SingleAsync(x=>x.Id==c.Admin.AccountId);var sessions=await db.Sessions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync();
            await Offline("login");Check.True(await db.Sessions.CountAsync()==sessions.Count,"Undelivered request created session.");
            using var guest=new ApiTestClient(address);var token=await guest.Token();
            await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_rename N'dbo.PHIEN',N'PHIEN_P0_OFFLINE'");
            try
            {
                using var request=new HttpRequestMessage(HttpMethod.Post,"/api/auth/login"){Content=JsonContent.Create(new LoginRequest(account.Username,CommerceContext.Password,"admin"))};request.Headers.Add("X-CSRF-TOKEN",token);
                using var response=await guest.Http.SendAsync(request);var body=await response.Content.ReadAsStringAsync();
                Check.True(response.StatusCode==HttpStatusCode.InternalServerError&&body.Contains("SERVER_ERROR")&&!response.Headers.Contains("Set-Cookie"),"SQL login failure reported success/created cookie.");
            }
            finally{await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_rename N'dbo.PHIEN_P0_OFFLINE',N'PHIEN'");}
            Check.True(JsonSerializer.Serialize(sessions)==JsonSerializer.Serialize(await db.Sessions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()),"Failed login changed sessions.");
            var start=DateTime.UtcNow;await Login(c,guest,true);var end=DateTime.UtcNow;var fresh=await db.Sessions.AsNoTracking().Where(x=>x.AccountId==c.Admin.AccountId).OrderByDescending(x=>x.ExpiresAt).FirstAsync();
            Check.True(fresh.ExpiresAt>=start.AddHours(8)&&fresh.ExpiresAt<=end.AddHours(8),"ADMIN session lifetime differs from eight hours.");
        });
        await Check.Run("A02.03 lost logout request retains session; lost committed response is reconciled with SQL and captured cookie",async()=>
        {
            using var first=new ApiTestClient(address);using var second=new ApiTestClient(address);await Login(c,first,true);await Login(c,second,true);
            var cookie=first.SessionCookie();var otherCookie=second.SessionCookie();await using var db=c.Open();var count=await db.Sessions.CountAsync();var before=await Business(c);
            await Offline();first.Cookies.Add(address,new Cookie("PhoneStore.Session","","/"){Expires=DateTime.UtcNow.AddDays(-1)});
            await Replay(address,cookie,HttpStatusCode.OK);Check.True(await db.Sessions.CountAsync()==count,"Clearing local credentials was assumed to revoke an undelivered logout.");first.Replay(cookie);
            var token=await first.Token();using var transport=new HttpClient(new LoseReply(new HttpClientHandler{UseProxy=false,CookieContainer=first.Cookies})){BaseAddress=address};
            using var request=new HttpRequestMessage(HttpMethod.Post,"/api/auth/logout"){Content=JsonContent.Create(new{})};request.Headers.Add("X-CSRF-TOKEN",token);var lost=false;
            try{using var response=await transport.SendAsync(request);}catch(HttpRequestException){lost=true;}
            Check.True(lost,"Response-loss injection did not execute.");await Replay(address,cookie,HttpStatusCode.Unauthorized);await Replay(address,otherCookie,HttpStatusCode.OK);
            Check.True(await db.Sessions.CountAsync()==count-1&&before==await Business(c),"Logout reconciliation affected foreign session or business data.");
        });
    }
}
