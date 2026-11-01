using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Admin.Client;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.Admin.Client.Checks;

internal static class ContactChecks
{
    private static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static async Task Reject(string code,Func<Task> run){try{await run();throw new InvalidOperationException("Expected "+code);}catch(AdminApiException e){Assert(e.Code==code,"Unexpected error "+e.Code);}}
    public static async Task Run()
    {
        await using var qa=await QaBackend.Create();var probe=new ContactsProbe();using var api=qa.Api(probe);await api.LoginAsync("admin-w1-qa",qa.Password);var service=new ContactsWorkspace(api);await using var db=qa.Open();
        var first=await db.Customers.SingleAsync();first.FullName="Khách W3 một";first.Email="w3-one@example.test";await db.SaveChangesAsync();
        var peer=new Account{Username="customer-w3-peer",Role="CUSTOMER",Active=true};peer.PasswordHash=new PasswordHasher<Account>().HashPassword(peer,qa.Password);db.Accounts.Add(peer);await db.SaveChangesAsync();
        db.Customers.Add(new Customer{Id=peer.Id,FullName="Khách W3 hai",Phone="0938123592",Email="w3-two@example.test",Address="QA peer only"});await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var customerId=first.Id;var originalHash=await db.Accounts.Where(a=>a.Id==customerId).Select(a=>a.PasswordHash).SingleAsync();
        var trace=new List<object>();var rejected=new List<object>();var profileSnapshots=new List<object>();var sessionProof=new List<object>();var passed=0;
        HttpClient CustomerClient()=>qa.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
        async Task<HttpResponseMessage> Post(HttpClient client,string path,object body)
        {var token=await client.GetFromJsonAsync<TokenResult>("/api/security/token");using var request=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(body)};request.Headers.Add("X-CSRF-TOKEN",token!.Token);return await client.SendAsync(request);}
        async Task Login(HttpClient client){using var response=await Post(client,"/api/auth/login",new{username="customer-w1-qa",password=qa.Password,surface="customer"});Assert(response.IsSuccessStatusCode,"Real customer fixture login failed");}
        async Task<string> MoneyStock()=>JsonSerializer.Serialize(new{orders=await db.Orders.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),lines=await db.OrderLines.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.OrderId).ToListAsync(),history=await db.OrderHistory.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),stock=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync(),journal=await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
        async Task<string> Snapshot()=>JsonSerializer.Serialize(new{accounts=await db.Accounts.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Username,x.Role,x.Active}).ToListAsync(),customers=await db.Customers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),suppliers=await db.Suppliers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),sessions=await db.Sessions.AsNoTracking().GroupBy(x=>x.AccountId).Select(g=>new{accountId=g.Key,count=g.Count()}).OrderBy(x=>x.accountId).ToListAsync(),commerce=JsonSerializer.Deserialize<JsonElement>(await MoneyStock())});
        async Task Test(string ids,Func<Task> run){var before=await Snapshot();var offset=probe.Records.Count;await run();trace.Add(new{ids,status="PASS",before=JsonSerializer.Deserialize<JsonElement>(before),after=JsonSerializer.Deserialize<JsonElement>(await Snapshot()),http=probe.Records.Skip(offset).ToArray()});Console.WriteLine("PASS "+ids);passed++;}
        async Task Atomic(string code,Func<Task> run){var before=await Snapshot();await Reject(code,run);var after=await Snapshot();Assert(before==after,"Rejected contact write changed SQL");string Hash(string value)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));rejected.Add(new{code,unchanged=true,beforeSha256=Hash(before),afterSha256=Hash(after)});}
        using var customerA=CustomerClient();using var customerB=CustomerClient();await Login(customerA);await Login(customerB);
        var catalog=new CatalogWorkspace(api);var brand=(await catalog.SaveLookup("brands",0,new("Hãng W3 QA",true))).Single().Id;var category=(await catalog.SaveLookup("categories",0,new("Danh mục W3 QA",true))).Single().Id;
        var product=await catalog.Save(0,new("QA-W3-PHONE","Điện thoại W3 QA",brand,category,"QA","QA","",true,[new(0,"QA-W3-B","Đen",128,10000000,20,true,null)],[]));
        var inventory=await db.Inventory.SingleAsync();inventory.OnHand=50;await db.SaveChangesAsync();db.ChangeTracker.Clear();
        for(var i=0;i<21;i++)
        {
            using(var cart=await Post(customerA,"/api/cart",new{variantId=product.Variants.Single().Id,quantity=1}))Assert(cart.IsSuccessStatusCode,"Cart fixture failed");
            using var order=await Post(customerA,"/api/orders",new{requestKey=Guid.NewGuid(),recipient="Người nhận W3 cũ",phone="0938123591",address="Địa chỉ snapshot W3 cũ",lines=new[]{new{variantId=product.Variants.Single().Id,quantity=1,price=12000000}},paymentMethod="COD"});Assert(order.StatusCode==HttpStatusCode.Created,"Order fixture failed");
        }
        CustomerRecord current=null!;
        await Test("WIN.A09.01/02 query/state/paging/detail; narrow safe DTOs exclude secrets and ADMIN",async()=>
        {
            var a=await service.Customers("Khách W3",true,1,1);var b=await service.Customers("Khách W3",true,2,1);Assert(a.Total==2&&a.Items.Count==1&&b.Items.Single().Id!=a.Items.Single().Id,"Customer paging wrong");
            Assert((await service.Customers("0938123591",null)).Items.Single().Id==customerId&&(await service.Customers("w3-two@example.test",null)).Items.Single().Id==peer.Id,"Phone/email query failed");
            Assert((await service.Customers(null,false)).Total==0&&(await service.Customers("no-match-"+Guid.NewGuid(),null)).Total==0,"State/no-match wrong");current=await service.Customer(customerId);
            Assert(typeof(CustomerRecord).GetProperties().All(p=>p.Name is not("Password" or "PasswordHash" or "Role")),"Unsafe customer DTO");
            Assert((await service.Customers(null,null)).Items.All(c=>c.Id!=qa.AdminId),"ADMIN in customer list");
        });
        await Test("WIN.A12.01 real customer orders page20+1, empty peer and working read-only detail",async()=>
        {
            var before=await Snapshot();var a=await service.Orders(customerId);var b=await service.Orders(customerId,2);Assert(a.Total==21&&a.Items.Count==20&&b.Items.Count==1&&a.Items.All(o=>o.CustomerId==customerId),"Customer order paging wrong");
            await Reject("INVALID_INPUT",()=>service.Orders(customerId,1,101));
            Assert((await service.Orders(peer.Id)).Total==0,"Empty customer has orders");var detail=await service.Order(a.Items[0].Id);Assert(detail.Id==a.Items[0].Id&&detail.Lines.Count==1&&detail.Total==12000000&&detail.Recipient=="Người nhận W3 cũ","Order handoff detail wrong");Assert(before==await Snapshot(),"Read-only customer/order screens mutated DB");
        });
        await Test("WIN.A10.01 duplicate phone/email and invalid profile atomic; exact four-field request",async()=>
        {
            var input=new CustomerProfileRequest("Không được ghi dở",current.Phone,current.Email,"Địa chỉ mới QA");
            await Atomic("INVALID_INPUT",()=>service.SaveProfile(customerId,input with{Phone="0938123592"}));await Atomic("INVALID_INPUT",()=>service.SaveProfile(customerId,input with{Email="W3-TWO@EXAMPLE.TEST"}));
            await Atomic("INVALID_INPUT",()=>service.SaveProfile(customerId,input with{Phone="bad-phone"}));
            probe.StripToken=true;await Atomic("INVALID_INPUT",()=>service.SaveProfile(customerId,input));probe.StripToken=false;
            Assert(typeof(CustomerProfileRequest).GetProperties().Select(p=>p.Name).Order().SequenceEqual(new[]{"Address","Email","FullName","Phone"}),"Profile request accepts privilege/state fields");
        });
        await Test("WIN.A10.02 own contacts accepted; PUT204 then GET;21 old order snapshots/stock/payment/history unchanged",async()=>
        {
            var before=await MoneyStock();current=await service.SaveProfile(customerId,new(current.FullName,current.Phone,current.Email,current.Address));
            current=await service.SaveProfile(customerId,new("Khách W3 đã sửa","0938123599","w3-new@example.test","Địa chỉ hồ sơ mới QA"));
            var after=await MoneyStock();Assert(current.FullName=="Khách W3 đã sửa"&&current.Phone=="0938123599"&&before==after,"Profile edit mutated old snapshot or failed readback");
            Assert(await db.Accounts.Where(a=>a.Id==customerId).Select(a=>a.PasswordHash).SingleAsync()==originalHash,"Profile changed credential hash");
            profileSnapshots.Add(new{unchanged=true,before=JsonSerializer.Deserialize<JsonElement>(before),after=JsonSerializer.Deserialize<JsonElement>(after)});
        });
        await Test("WIN.A11.01 confirmed state command revokes both real sessions; unlock restores none; fresh login required",async()=>
        {
            Assert(await db.Sessions.CountAsync(s=>s.AccountId==customerId)==2,"Two real customer sessions missing");var before=await MoneyStock();
            current=await service.SetCustomerState(current,false);Assert(!current.Active&&await db.Sessions.CountAsync(s=>s.AccountId==customerId)==0,"Lock did not revoke all sessions");
            foreach(var client in new[]{customerA,customerB}){using var response=await client.GetAsync("/api/auth/session");Assert(response.StatusCode==HttpStatusCode.Unauthorized,"Locked old session still works");}
            using(var write=await Post(customerA,"/api/cart",new{variantId=product.Variants.Single().Id,quantity=1}))Assert(write.StatusCode==HttpStatusCode.Unauthorized,"Old session wrote after lock");
            Assert((await service.Customers(null,false)).Items.Single().Id==customerId,"Locked filter missing");current=await service.SetCustomerState(current,true);
            foreach(var client in new[]{customerA,customerB}){using var response=await client.GetAsync("/api/auth/session");Assert(response.StatusCode==HttpStatusCode.Unauthorized,"Unlock resurrected old session");}
            Assert(await db.Sessions.CountAsync(s=>s.AccountId==customerId)==0,"Unlock recreated sessions");using var fresh=CustomerClient();await Login(fresh);using(var response=await fresh.GetAsync("/api/auth/session"))Assert(response.IsSuccessStatusCode,"Fresh login after unlock failed");
            Assert(before==await MoneyStock(),"Lock/unlock changed order/stock/money/history");sessionProof.Add(new{sessionsBefore=2,afterLock=0,oldSessionsAfterUnlock="401/401",afterUnlockBeforeLogin=0,freshLogin="200",commerceUnchanged=true});
        });
        await Test("WIN.A11.02/03 stale expectedActive409 no overwrite; ADMIN target404 no lock/profile capability",async()=>
        {
            using var competitor=qa.Api();await competitor.LoginAsync("admin-w1-qa",qa.Password);var competingService=new ContactsWorkspace(competitor);
            var stale=current;current=await competingService.SetCustomerState(current,false);await Atomic("CONCURRENT_UPDATE",()=>service.SetCustomerState(stale,false));Assert(new AdminApiException("CONCURRENT_UPDATE",409).Message.Contains("vui lòng tải lại"),"Stale message missing");current=await service.SetCustomerState(current,true);
            await Reject("NOT_FOUND",()=>service.Customer(qa.AdminId));await Atomic("NOT_FOUND",()=>api.CommandAsync($"/api/admin/customers/{qa.AdminId}/state",new CustomerStateRequest(false,true),new()));
            Assert(await db.Accounts.Where(a=>a.Id==qa.AdminId).Select(a=>a.Active).SingleAsync(),"ADMIN locked through customer surface");
        });
        SupplierRecord supplier=null!;
        await Test("WIN.A13.01/02 supplier POST201/query, PUT204/query, duplicate create/update atomic and no by-id/delete",async()=>
        {
            supplier=await service.SaveSupplier(0,new("W3-SUP-A","NCC W3 A","0938123593","Địa chỉ NCC QA",true));
            var other=await service.SaveSupplier(0,new("W3-SUP-B","NCC W3 B","0938123594","Địa chỉ NCC QA B",false));
            await Atomic("INVALID_INPUT",()=>service.SaveSupplier(0,new(supplier.Code,"Tên trùng QA","0938123595","QA",true)));
            await Atomic("INVALID_INPUT",()=>service.SaveSupplier(other.Id,new(supplier.Code,"Không ghi dở","0938123596","QA mới",false)));
            supplier=await service.SaveSupplier(supplier.Id,new("W3-SUP-EDIT","NCC W3 đã sửa",supplier.Phone,"Địa chỉ đã sửa",true));Assert(supplier.Code=="W3-SUP-EDIT"&&supplier.Name=="NCC W3 đã sửa","Supplier readback wrong");
            supplier=await service.SaveSupplier(supplier.Id,new(supplier.Code,supplier.Name,supplier.Phone,supplier.Address,false));Assert(!supplier.Active,"Supplier stop not read back");
            supplier=await service.SaveSupplier(supplier.Id,new(supplier.Code,supplier.Name,supplier.Phone,supplier.Address,true));Assert(supplier.Active,"Supplier reopen not read back");
            Assert(!probe.Records.Any(r=>r.method=="DELETE"||r.method=="GET"&&r.path.StartsWith("/api/admin/suppliers/")),"Invented supplier by-id/delete route");
        });
        await Test("WIN.A13.03 active-only picker reads all bounded pages; list queries/page; no stock/order mutation",async()=>
        {
            // Explicit additional QA rows prove the picker crosses the server's maximum100 boundary.
            db.Suppliers.AddRange(Enumerable.Range(1,102).Select(i=>new Supplier{Code=$"W3-POOL-{i:000}",Name=$"NCC pool {i:000}",Phone="0938123593",Address="QA paging only",Active=true}));await db.SaveChangesAsync();db.ChangeTracker.Clear();
            var before=await Snapshot();var rows=await service.ActiveSuppliers();Assert(rows.Count==103&&rows.All(s=>s.Active),"Picker included stopped supplier or lost later pages");
            var a=await service.Suppliers("W3-POOL",null);var b=await service.Suppliers("W3-POOL",null,2);Assert(a.Total==102&&a.Items.Count==20&&b.Items[0].Id!=a.Items[0].Id,"Supplier query/paging wrong");
            Assert((await service.Suppliers(null,false)).Items.Single().Code=="W3-SUP-B"&&(await service.Suppliers("missing-w3",null)).Total==0,"Supplier state/empty query wrong");Assert(before==await Snapshot(),"Picker/list mutated database");
        });
        await Test("WIN.BASE.08 W3 lost response/readback guard reconciles customer ID and supplier list, no blind resend",async()=>
        {
            probe.LosePath=$"/api/admin/customers/{customerId}";await Reject("UNCERTAIN",()=>service.SaveProfile(customerId,new("Khách W3 đã commit",current.Phone,current.Email,current.Address)));var writes=probe.Writes;
            await Reject("UNCERTAIN",()=>service.SaveProfile(customerId,new("Không gửi lại",current.Phone,current.Email,current.Address)));Assert(writes==probe.Writes,"Blind customer retry");probe.LosePath=null;current=await service.ReconcileCustomer(customerId);Assert(current.FullName=="Khách W3 đã commit","Customer reconciliation failed");
            probe.LosePath=$"/api/admin/customers/{customerId}/state";await Reject("UNCERTAIN",()=>service.SetCustomerState(current,false));writes=probe.Writes;
            await Reject("UNCERTAIN",()=>service.SetCustomerState(current,false));Assert(writes==probe.Writes,"Blind customer state retry");probe.LosePath=null;current=await service.ReconcileCustomer(customerId);Assert(!current.Active&&await db.Sessions.CountAsync(s=>s.AccountId==customerId)==0,"Lost lock response did not reconcile");current=await service.SetCustomerState(current,true);
            probe.LosePath="/api/admin/suppliers";var input=new SupplierRequest("W3-LOST","NCC mất phản hồi QA","0938123597","QA",true);await Reject("UNCERTAIN",()=>service.SaveSupplier(0,input));writes=probe.Writes;
            await Reject("UNCERTAIN",()=>service.SaveSupplier(0,input));Assert(writes==probe.Writes,"Blind supplier retry");probe.LosePath=null;var recovered=await service.ReconcileSupplier(0,input.Code);Assert(recovered is not null&&await db.Suppliers.CountAsync(s=>s.Code==input.Code)==1,"Supplier recovery duplicated data");
            probe.FailReadPath=$"/api/admin/customers/{customerId}";await Reject("READBACK_REQUIRED",()=>service.SaveProfile(customerId,new(current.FullName,current.Phone,current.Email,current.Address)));writes=probe.Writes;await Reject("READBACK_REQUIRED",()=>service.SaveProfile(customerId,new(current.FullName,current.Phone,current.Email,current.Address)));Assert(writes==probe.Writes,"Repeated known customer PUT");current=await service.Customer(customerId);
            probe.FailReadPath="/api/admin/suppliers?";await Reject("READBACK_REQUIRED",()=>service.SaveSupplier(0,input with{Code="W3-READBACK"}));writes=probe.Writes;await Reject("READBACK_REQUIRED",()=>service.SaveSupplier(0,input with{Code="W3-READBACK"}));Assert(writes==probe.Writes,"Repeated known supplier create");await service.FindSupplier(service.PendingSupplier!.Value.Id,"W3-READBACK");
            Assert(probe.MissingTokens==0&&probe.Tokens==probe.Writes,"No fresh CSRF for every client write");
        });
        var output=Path.Combine(QaBackend.Root,".artifacts","admin-w3");Directory.CreateDirectory(output);await File.WriteAllTextAsync(Path.Combine(output,"sql-results.json"),JsonSerializer.Serialize(new{qaOnly=true,checks=passed,rejectedWrites=rejected,profileSnapshots,sessionProof,records=trace},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"{passed} admin W3 client checks passed.");
    }
    private sealed record HttpProof(string method,string path,int status,string body);
    private sealed class ContactsProbe:DelegatingHandler
    {
        public List<HttpProof> Records { get; }=[];public int Tokens,Writes,MissingTokens;public bool StripToken;public string? LosePath,FailReadPath;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.PathAndQuery;var people=path.StartsWith("/api/admin/customers")||path.StartsWith("/api/admin/suppliers");
            if(path=="/api/security/token")Tokens++;if(request.Method==HttpMethod.Post||request.Method==HttpMethod.Put){Writes++;if(!request.Headers.Contains("X-CSRF-TOKEN"))MissingTokens++;}
            var body=people&&request.Content is not null?await request.Content.ReadAsStringAsync(ct):"";if(StripToken&&people)request.Headers.Remove("X-CSRF-TOKEN");
            var response=await base.SendAsync(request,ct);if(people)Records.Add(new(request.Method.Method,path,(int)response.StatusCode,body));
            if(path==LosePath&&request.Method!=HttpMethod.Get&&response.IsSuccessStatusCode){response.Dispose();throw new HttpRequestException("QA response lost after commit");}
            if(FailReadPath is not null&&request.Method==HttpMethod.Get&&path.StartsWith(FailReadPath)){FailReadPath=null;response.Dispose();return new(HttpStatusCode.ServiceUnavailable){Content=JsonContent.Create(new{code="DATABASE_UNAVAILABLE"})};}return response;
        }
    }
}
