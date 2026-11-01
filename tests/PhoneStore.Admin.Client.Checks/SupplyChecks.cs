using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Client.Checks;

internal static class SupplyChecks
{
    private static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static async Task<AdminApiException> Reject(string code,Func<Task> action)
    {try{await action();throw new InvalidOperationException("Expected "+code);}catch(AdminApiException e){Assert(e.Code==code,"Unexpected "+e.Code);return e;}}
    public static async Task Run()
    {
        await using var qa=await QaBackend.Create();var gate=new SupplyRaceGate();qa.CommandInterceptor=gate;var probe=new SupplyProbe();using var api=qa.Api(probe);await api.LoginAsync("admin-w1-qa",qa.Password);using var second=qa.Api();await second.LoginAsync("admin-w1-qa",qa.Password);
        var supply=new SupplyWorkspace(api);var catalog=new CatalogWorkspace(api);var contacts=new ContactsWorkspace(api);await using var db=qa.Open();
        var states=new Dictionary<string,JsonElement>();var records=new List<object>();var rejected=new List<object>();var facts=new List<object>();var passed=0;
        async Task<string> Snapshot()
        {
            var text=JsonSerializer.Serialize(new{purchases=await db.Purchases.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),lines=await db.PurchaseLines.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),variants=await db.Variants.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),stock=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync(),journal=await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),suppliers=await db.Suppliers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),orders=await db.Orders.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),orderLines=await db.OrderLines.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.OrderId).ToListAsync(),history=await db.OrderHistory.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
            var key=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));states.TryAdd(key,JsonSerializer.Deserialize<JsonElement>(text));return key;
        }
        async Task Test(string ids,Func<Task> run){var before=await Snapshot();var offset=probe.Records.Count;await run();records.Add(new{ids,status="PASS",before,after=await Snapshot(),http=probe.Records.Skip(offset).ToArray()});Console.WriteLine("PASS "+ids);passed++;}
        async Task Atomic(string code,Func<Task> run){var before=await Snapshot();var error=await Reject(code,run);var after=await Snapshot();Assert(before==after,"Rejected supply write changed SQL");rejected.Add(new{code,status=error.Status,before,after,unchanged=true});}
        async Task<string> Balances()=>JsonSerializer.Serialize(new{stock=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync(),variants=await db.Variants.AsNoTracking().OrderBy(x=>x.Id).Select(v=>new{v.Id,v.Cost,v.Price,v.ProfitPercent,v.Version}).ToListAsync(),journal=await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),payments=await db.Payments.AsNoTracking().ToListAsync(),orders=await db.Orders.AsNoTracking().ToListAsync(),history=await db.OrderHistory.AsNoTracking().ToListAsync()});
        var brand=(await catalog.SaveLookup("brands",0,new("Hãng kho QA",true))).Single().Id;var category=(await catalog.SaveLookup("categories",0,new("Điện thoại kho QA",true))).Single().Id;
        var supplier=await contacts.SaveSupplier(0,new("SUP-W4","NCC kho QA","0938123593","QA only",true));
        ProductRequest Product(string code)=>new(code,"Điện thoại "+code,brand,category,"QA","QA","",true,[new(0,code+"-A","Đen",128,10000000,20,true,null),new(0,code+"-B","Trắng",256,12000000,25,true,null)],[]);
        await catalog.Save(0,Product("W4-DUMMY"));var product=await catalog.Save(0,Product("W4-TARGET"));var a=product.Variants[0].Id;var b=product.Variants[1].Id;Assert(a!=product.Id,"Fixture must distinguish productId/variantId");
        var session=new PurchaseDraftSession(api);PurchaseRecord receipt=null!;
        await Test("WIN.A14.01/03 draft POST requestKey,201/GET; server metadata/total and no stock change; same-key replay201 same id",async()=>
        {
            var before=await Balances();var read=await session.Save(supplier.Id,[new(a,10,8000000),new(b,5,9000000)]);receipt=read.Purchase;
            Assert(receipt.Status=="NHAP"&&receipt.Total==125000000&&receipt.CreatorId==qa.AdminId&&receipt.CreatedAt!=default,"Authoritative draft fields wrong");
            var input=new PurchaseRequest(supplier.Id,[new(a,10,8000000),new(b,5,9000000)],RequestKey:session.RequestKey);var replay=await api.CreateAsync("/api/admin/purchases",input);
            Assert(replay==receipt.Id&&await db.Purchases.CountAsync()==1&&before==await Balances(),"Same key duplicated purchase or changed stock");
            await Atomic("INVALID_STATE",()=>api.CreateAsync("/api/admin/purchases",input with{Lines=[new(a,11,8000000),new(b,5,9000000)]}));facts.Add(new{sameKeyReplayStatus=201,sameId=true,purchases=1,stockUnchanged=true});
        });
        await Test("WIN.A14.02 stale PUT rejects atomically, complete versioned edit; validation no partial rows",async()=>
        {
            var old=receipt;receipt=(await session.Save(supplier.Id,[new(a,10,8000000),new(b,5,9000000)])).Purchase;
            await Atomic("CONCURRENT_UPDATE",()=>api.PutAsync($"/api/admin/purchases/{receipt.Id}",new PurchaseRequest(supplier.Id,[new(a,1,1)],old.Version)));
            await Atomic("INVALID_INPUT",()=>api.CreateAsync("/api/admin/purchases",new PurchaseRequest(supplier.Id,[new(a,1,1),new(a,2,1)],RequestKey:Guid.NewGuid())));
            foreach(var line in new[]{new PurchaseLineRequest(a,0,1),new PurchaseLineRequest(a,1,0)})await Atomic("INVALID_INPUT",()=>api.CreateAsync("/api/admin/purchases",new PurchaseRequest(supplier.Id,[line],RequestKey:Guid.NewGuid())));
        });
        await Test("WIN.A15.01/03 RACE two gated real confirmations on distinct connections: one204 one409, one stock/price/journal effect",async()=>
        {
            var firstCommand=new PurchaseTransition(receipt.Version,Guid.NewGuid());var secondCommand=new PurchaseTransition(receipt.Version,Guid.NewGuid());
            async Task<string> Attempt(IAdminApi client,PurchaseTransition command){try{await client.CommandAsync($"/api/admin/purchases/{receipt.Id}/confirm",command,new());return "SUCCESS";}catch(AdminApiException e){return e.Code;}}
            gate.Enabled=true;var one=Attempt(api,firstCommand);var two=Attempt(second,secondCommand);string[] outcomes;
            try{await gate.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));Assert(!one.IsCompleted&&!two.IsCompleted&&gate.Arrivals==2&&gate.Connections.Count==2,"Requests did not contend on separate SQL connections");gate.Release.TrySetResult();outcomes=await Task.WhenAll(one,two);}finally{gate.Release.TrySetResult();gate.Enabled=false;}
            Assert(outcomes.Count(x=>x=="SUCCESS")==1&&outcomes.Count(x=>x=="CONCURRENT_UPDATE")==1,"Confirmation race outcomes wrong");
            var read=await supply.Readback(receipt.Id);receipt=read.Purchase;Assert(receipt.Status=="DA_XAC_NHAN"&&read.CurrentStock.Single(x=>x.Stock.VariantId==a) is{Cost:8000000,Price:9600000,Stock.OnHand:10}&&read.CurrentStock.Single(x=>x.Stock.VariantId==b) is{Cost:9000000,Price:11250000,Stock.OnHand:5},"Confirm did not use server balances/cost/price");
            Assert(await db.Movements.CountAsync(m=>m.PurchaseId==receipt.Id)==2,"Confirm wrote twice");await Atomic("CONCURRENT_UPDATE",()=>api.CommandAsync($"/api/admin/purchases/{receipt.Id}/confirm",firstCommand,new()));
            await Atomic("INVALID_STATE",()=>api.CommandAsync($"/api/admin/purchases/{receipt.Id}/confirm",new PurchaseTransition(receipt.Version,Guid.NewGuid()),new()));
            await Atomic("INVALID_STATE",()=>api.PutAsync($"/api/admin/purchases/{receipt.Id}",new PurchaseRequest(supplier.Id,[],receipt.Version)));
            facts.Add(new{raceArrivals=gate.Arrivals,distinctConnections=gate.Connections.Count,outcomes,stockA=10,stockB=5,receiptJournals=2});
        });
        await Test("WIN.A15.02 cancel nonempty/empty drafts sends version+operationKey, no stock/price changes",async()=>
        {
            foreach(var lines in new[]{new[]{new PurchaseLineRequest(a,3,7000000)},Array.Empty<PurchaseLineRequest>()})
            {
                var draft=new PurchaseDraftSession(api);var saved=await draft.Save(supplier.Id,lines);var before=await Balances();
                var missingKey=await Reject("INVALID_INPUT",()=>api.CommandAsync($"/api/admin/purchases/{saved.Purchase.Id}/cancel",new PurchaseTransition(saved.Purchase.Version,Guid.Empty),new()));Assert(missingKey.Status==400,"Cancel missing key wrong");
                var action=new PurchaseOperation(saved.Purchase,"cancel");var canceled=await supply.Execute(action);Assert(canceled.Purchase.Status=="DA_HUY"&&before==await Balances(),"Cancel changed stock or price");
                await Atomic("CONCURRENT_UPDATE",()=>api.PutAsync($"/api/admin/purchases/{saved.Purchase.Id}",new PurchaseRequest(supplier.Id,lines,saved.Purchase.Version)));
                await Atomic("INVALID_STATE",()=>api.PutAsync($"/api/admin/purchases/{saved.Purchase.Id}",new PurchaseRequest(supplier.Id,lines,canceled.Purchase.Version)));
            }
        });
        await Test("WIN.A14.04/A15.04 empty/stopped supplier/overflow-price confirm400, draft and prior line stock rollback",async()=>
        {
            var empty=(await new PurchaseDraftSession(api).Save(supplier.Id,[])).Purchase;
            await Atomic("INVALID_INPUT",()=>api.CommandAsync($"/api/admin/purchases/{empty.Id}/confirm",new PurchaseTransition(empty.Version,Guid.NewGuid()),new()));
            var stopped=(await new PurchaseDraftSession(api).Save(supplier.Id,[new(a,1,1)])).Purchase;await contacts.SaveSupplier(supplier.Id,new(supplier.Code,supplier.Name,supplier.Phone,supplier.Address,false));
            var err=await Reject("INVALID_INPUT",()=>api.CommandAsync($"/api/admin/purchases/{stopped.Id}/confirm",new PurchaseTransition(stopped.Version,Guid.NewGuid()),new()));Assert(err.PublicValidation?.Contains("nhà cung cấp")==true,"Public business reason not available");
            await Atomic("INVALID_INPUT",()=>api.CreateAsync("/api/admin/purchases",new PurchaseRequest(supplier.Id,[],RequestKey:Guid.NewGuid())));await contacts.SaveSupplier(supplier.Id,new(supplier.Code,supplier.Name,supplier.Phone,supplier.Address,true));
            var bad=(await new PurchaseDraftSession(api).Save(supplier.Id,[new(a,1,4000000),new(b,1,8500000000000000m)])).Purchase;
            var before=await Snapshot();err=await Reject("INVALID_INPUT",()=>api.CommandAsync($"/api/admin/purchases/{bad.Id}/confirm",new PurchaseTransition(bad.Version,Guid.NewGuid()),new()));Assert(err.PublicValidation?.Contains("Số tiền")==true&&before==await Snapshot()&&(await supply.Purchase(bad.Id)).Status=="NHAP","Partial confirm leaked stock/price or hid price-limit reason");
        });
        await Test("WIN.A14.05 purchase query/status/paging only; empty draft metadata remains authoritative",async()=>
        {
            for(var i=0;i<21;i++)await new PurchaseDraftSession(api).Save(supplier.Id,[]);
            var before=await Snapshot();var first=await supply.Purchases(null,"NHAP");var next=await supply.Purchases(null,"NHAP",2);Assert(first.Items.Count==20&&next.Items.Count>0&&first.Items.All(p=>p.Status=="NHAP")&&next.Items[0].Id!=first.Items[0].Id,"Purchase paging/state wrong");
            Assert((await supply.Purchases(receipt.Code,null)).Items.Single().Id==receipt.Id&&(await supply.Purchases("no-match-"+Guid.NewGuid(),null)).Total==0&&before==await Snapshot(),"Purchase query/read changed DB");
        });
        using var customer=qa.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
        async Task Post(string path,object body){var token=await customer.GetFromJsonAsync<TokenResult>("/api/security/token");using var request=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(body)};request.Headers.Add("X-CSRF-TOKEN",token!.Token);using var response=await customer.SendAsync(request);Assert(response.IsSuccessStatusCode,"Customer stock-reservation fixture failed");}
        await Post("/api/auth/login",new{username="customer-w1-qa",password=qa.Password,surface="customer"});await Post("/api/cart",new{variantId=a,quantity=2});await Post("/api/orders",new{requestKey=Guid.NewGuid(),recipient="Khách QA kho",phone="0938123591",address="QA only",lines=new[]{new{variantId=a,quantity=2,price=9600000}},paymentMethod="COD"});
        StockRecord stock=null!;
        await Test("WIN.A16.01/02 stock query/brand/explicit below/page/no-match; actual injected SQL failure is error, never zero",async()=>
        {
            stock=await supply.StockRow(a,product.Variants[0].Sku);Assert(stock.OnHand==10&&stock.Reserved==2&&stock.Available==8,"Available not server H-R");
            Assert((await supply.Stock(stock.Sku,brand,8)).Items.Single().VariantId==a&&(await supply.Stock(stock.Sku,brand,7)).Total==0,"Explicit threshold boundary wrong");var first=await supply.Stock(null,null,null,1,1);var next=await supply.Stock(null,null,null,2,1);Assert(first.Total==4&&next.Items.Single().VariantId!=first.Items.Single().VariantId,"Stock paging wrong");Assert((await supply.Stock("no-match",null,null)).Total==0,"Stock no-match wrong");
            var before=await Snapshot();gate.FailStockReads=true;try{var err=await Reject("SERVER_ERROR",()=>supply.Stock(null,null,null));Assert(err.Status==500&&err.TraceId is not null&&err.PublicValidation is null,"SQL read failure hidden as stock0");}finally{gate.FailStockReads=false;}Assert(gate.StockFailureSeen&&before==await Snapshot(),"SQL fault was not exercised/read mutated data");
        });
        await Test("WIN.A17.01/02/04 absolute count uses variantId and stock version; below reserved/negative/stale reject",async()=>
        {
            foreach(var counted in new[]{-1,1,10})await Atomic("INVALID_INPUT",()=>api.CommandAsync($"/api/admin/stock/{a}/adjust",new StockAdjustment(counted,"QA reject",stock.Version,Guid.NewGuid()),new()));
            var variant=(await catalog.Product(product.Id)).Variants.Single(v=>v.Id==a);Assert(variant.Version!=stock.Version,"Fixture must distinguish product/stock versions");await Atomic("CONCURRENT_UPDATE",()=>api.CommandAsync($"/api/admin/stock/{a}/adjust",new StockAdjustment(12,"Wrong version QA",variant.Version,Guid.NewGuid()),new()));
            var intent=new CountOperation(stock,12,"  Kiểm kê QA  ");stock=await supply.Adjust(intent);Assert(stock.VariantId==a&&stock.OnHand==12&&stock.Reserved==2&&intent.Request().Reason=="Kiểm kê QA","Absolute count/version/path wrong");facts.Add(new{variantId=a,productId=product.Id,stockVersion=intent.Request().Version,variantVersion=variant.Version,operationKey=intent.Intent.Key,counted=12,deltaH=2,reserved=2});
            await Atomic("CONCURRENT_UPDATE",()=>api.CommandAsync($"/api/admin/stock/{a}/adjust",intent.Request(),intent.Intent));await Atomic("INVALID_INPUT",()=>api.CommandAsync($"/api/admin/stock/{a}/adjust",intent.Request() with{Version=stock.Version},intent.Intent));
            await Atomic("INVALID_STATE",()=>api.CommandAsync($"/api/admin/stock/{a}/adjust",intent.Request() with{Version=stock.Version,Counted=13},intent.Intent));
        });
        await Test("WIN.A17.03 lost count response reconciles journal operationKey, no retry effect; new intention new key",async()=>
        {
            var intent=new CountOperation(stock,13,"QA lost stock reply");probe.LosePath=$"/api/admin/stock/{a}/adjust";await Reject("UNCERTAIN",()=>supply.Adjust(intent));var writes=probe.Writes;await Reject("READBACK_REQUIRED",()=>supply.Adjust(intent));Assert(writes==probe.Writes,"Blind stock retry");probe.LosePath=null;
            var resolved=await supply.Resolve(intent);stock=resolved.Stock;Assert(resolved.Applied&&!resolved.RetryAllowed&&stock.OnHand==13&&(await supply.Movements(a,intent.Intent.Key)).Total==1,"Journal-key reconciliation failed");
            var next=new CountOperation(stock,14,"QA new stock intention");Assert(next.Intent.Key!=intent.Intent.Key,"New count reused key");stock=await supply.Adjust(next);Assert(stock.OnHand==14&&stock.Reserved==2,"New count failed");
        });
        await Test("WIN.A16.03 movement variant/key paging, actor/document/time and read-only SQL",async()=>
        {
            for(var i=0;i<21;i++)stock=await supply.Adjust(new(stock,stock.OnHand+1,"QA journal paging"));var before=await Snapshot();var first=await supply.Movements(a);var next=await supply.Movements(a,page:2);Assert(first.Items.Count==20&&next.Items.Count>0&&first.Items.All(x=>x.VariantId==a)&&first.Items.All(x=>x.Actor=="admin-w1-qa"||x.Actor=="customer-w1-qa")&&first.Items[0].Id!=next.Items[0].Id,"Journal paging/actor/variant wrong");Assert((await supply.Movements(b)).Items.Any(x=>x.PurchaseId==receipt.Id)&&before==await Snapshot(),"Journal missing source or mutated DB");
        });
        await Test("WIN.A14.01 same requestKey uncertain creation retry: changed body blocked,201 same id, readback failures guarded",async()=>
        {
            var draft=new PurchaseDraftSession(api);probe.LosePath="/api/admin/purchases";await Reject("UNCERTAIN",()=>draft.Save(supplier.Id,[new(a,1,2)]));var key=draft.RequestKey;var writes=probe.Writes;await Reject("READBACK_REQUIRED",()=>draft.Save(supplier.Id,[new(a,1,2)]));
            await Reject("UNCERTAIN",()=>api.RetryPurchaseCreateAsync(draft.PendingCreation! with{Lines=[new(a,2,2)]}));Assert(writes==probe.Writes,"Changed/unknown create blindly retried");probe.LosePath=null;var read=await draft.ResolveCreation();Assert(draft.RequestKey==key&&read.Purchase.Lines.Single().Quantity==1&&await db.Purchases.CountAsync(p=>p.Code==read.Purchase.Code)==1,"Same-key recovery duplicated creation");
            var known=new PurchaseDraftSession(api);probe.FailReadPrefix="/api/admin/purchases/";await Reject("DATABASE_UNAVAILABLE",()=>known.Save(supplier.Id,[]));writes=probe.Writes;await Reject("READBACK_REQUIRED",()=>known.Save(supplier.Id,[]));Assert(writes==probe.Writes&&known.Id>0,"Known created id lost or repeated");await known.Load();
        });
        await Test("WIN.A15.01/02 uncertain confirm/cancel reconciles GET; same operationKey frozen, no second stock effect",async()=>
        {
            foreach(var action in new[]{"confirm","cancel"})
            {
                var saved=(await new PurchaseDraftSession(api).Save(supplier.Id,[new(b,1,9000000)])).Purchase;var operation=new PurchaseOperation(saved,action);probe.LosePath=$"/api/admin/purchases/{saved.Id}/{action}";await Reject("UNCERTAIN",()=>supply.Execute(operation));var writes=probe.Writes;await Reject("READBACK_REQUIRED",()=>supply.Execute(operation));Assert(writes==probe.Writes,"Blind purchase transition retry");probe.LosePath=null;var before=await Snapshot();var resolved=await supply.Resolve(operation);Assert(operation.Finished&&resolved.Purchase.Status==(action=="confirm"?"DA_XAC_NHAN":"DA_HUY")&&before==await Snapshot(),"Transition readback altered data");
            }
            Assert(probe.MissingTokens==0&&probe.Tokens==probe.Writes,"Fresh CSRF missing on write");Assert(!probe.Records.Any(r=>r.path.Contains("by-request")||r.method=="DELETE"),"Invented requestKey/delete route");
        });
        await Test("WIN.A05.04 handoff reads persisted variant/cost, hints local until explicit draft save, stock unchanged",async()=>
        {
            var before=await Snapshot();var hand=new PurchaseHandoff(product.Id,[new(a,"ignored-draft-SKU",7),new(b,"ignored",3)]);var lines=await supply.FromProduct(hand);Assert(lines[0].Sku==product.Variants[0].Sku&&lines[0].Quantity==7&&lines[0].Cost==8000000&&before==await Snapshot(),"Handoff used unpersisted data or created purchase");
            var balance=await Balances();var draft=await new PurchaseDraftSession(api).Save(supplier.Id,lines.Select(l=>l.Request()).ToArray());Assert(draft.Purchase.Lines.Single(l=>l.VariantId==a).Quantity==7&&balance==await Balances(),"Hints did not persist only in draft or changed stock");
        });
        var output=Path.Combine(QaBackend.Root,".artifacts","admin-w4");Directory.CreateDirectory(output);await File.WriteAllTextAsync(Path.Combine(output,"sql-states.json"),JsonSerializer.Serialize(states));await File.WriteAllTextAsync(Path.Combine(output,"sql-results.json"),JsonSerializer.Serialize(new{qaOnly=true,checks=passed,rejectedWrites=rejected,facts,records,snapshotFile="sql-states.json",sqlSnapshotCount=states.Count},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"{passed} admin W4 client checks passed.");
    }
    private sealed record HttpProof(string method,string path,int status,string body);
    private sealed class SupplyProbe:DelegatingHandler
    {
        public List<HttpProof> Records { get; }=[];public int Writes,Tokens,MissingTokens;public string? LosePath,FailReadPrefix;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.PathAndQuery;var relevant=path.StartsWith("/api/admin/purchases")||path.StartsWith("/api/admin/stock");if(path=="/api/security/token")Tokens++;
            if(request.Method==HttpMethod.Post||request.Method==HttpMethod.Put){Writes++;if(!request.Headers.Contains("X-CSRF-TOKEN"))MissingTokens++;}
            var body=relevant&&request.Content is not null?await request.Content.ReadAsStringAsync(ct):"";var response=await base.SendAsync(request,ct);if(relevant)Records.Add(new(request.Method.Method,path,(int)response.StatusCode,body));
            if(path==LosePath&&request.Method!=HttpMethod.Get&&response.IsSuccessStatusCode){response.Dispose();throw new HttpRequestException("QA committed response lost");}
            if(FailReadPrefix is not null&&request.Method==HttpMethod.Get&&path.StartsWith(FailReadPrefix)){FailReadPrefix=null;response.Dispose();return new(HttpStatusCode.ServiceUnavailable){Content=JsonContent.Create(new{code="DATABASE_UNAVAILABLE"})};}return response;
        }
    }
}
