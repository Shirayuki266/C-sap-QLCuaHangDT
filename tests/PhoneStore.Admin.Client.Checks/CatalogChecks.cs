using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Client.Checks;

internal static class CatalogChecks
{
    private static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static async Task Reject(string code,Func<Task> action)
    {try{await action();throw new InvalidOperationException("Expected "+code);}catch(AdminApiException e){Assert(e.Code==code,"Actual error "+e.Code);}}
    public static async Task Run()
    {
        await using var qa=await QaBackend.Create();var probe=new CatalogProbe();using var api=qa.Api(probe);await api.LoginAsync("admin-w1-qa",qa.Password);var service=new CatalogWorkspace(api);await using var db=qa.Open();
        var trace=new List<object>();var rejectedWrites=new List<object>();var priceChanges=new List<object>();var passed=0;
        async Task<string> Snapshot()=>JsonSerializer.Serialize(new
        {
            brands=await db.Brands.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),categories=await db.Categories.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            products=await db.Products.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),variants=await db.Variants.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),images=await db.ProductImages.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            stock=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync(),orders=await db.Orders.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),lines=await db.OrderLines.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
            payments=await db.Payments.AsNoTracking().OrderBy(x=>x.OrderId).ToListAsync(),history=await db.OrderHistory.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),journal=await db.Movements.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()
        });
        async Task Test(string ids,Func<Task> run){var before=await Snapshot();var offset=probe.Records.Count;await run();trace.Add(new{ids,status="PASS",before=JsonSerializer.Deserialize<JsonElement>(before),after=JsonSerializer.Deserialize<JsonElement>(await Snapshot()),http=probe.Records.Skip(offset).ToArray()});Console.WriteLine("PASS "+ids);passed++;}
        async Task Atomic(string code,Func<Task> run)
        {
            var before=await Snapshot();await Reject(code,run);var after=await Snapshot();Assert(before==after,"Rejected write changed SQL state");
            string Hash(string data)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data)));
            rejectedWrites.Add(new{code,unchanged=true,beforeSha256=Hash(before),afterSha256=Hash(after)});
        }
        var brand=0;var category=0;ProductRecord product=null!;long orderId=0;
        ProductRequest New(string code)=>new(code,"Điện thoại QA "+code,brand,category,"Thông số QA","Mô tả QA","/images/qa-main.png",true,
            [new(0,code+"-B","Đen",128,10000000,20,true,null),new(0,code+"-W","Trắng",256,12000000,0,true,null)],[new("https://example.test/qa.png",0)]);
        await Test("WIN.A08.01 ADMIN lookup POST201/list refresh/PUT204 full name+active, paging includes inactive",async()=>
        {
            var rows=await service.SaveLookup("brands",0,new("Hãng QA",true));brand=rows.Single().Id;
            category=(await service.SaveLookup("categories",0,new("Điện thoại QA",true))).Single().Id;
            await service.SaveLookup("brands",0,new("Hãng đã ngừng QA",false));
            Assert((await service.Lookups("brands")).Count==2,"Inactive ADMIN lookup lost");
            var paged=await api.GetAsync<PageResult<LookupRow>>("/api/admin/lookups/brands?page=1&pageSize=1");Assert(paged.Items.Count==1&&paged.Total==2&&paged.PageSize==1,"Lookup paging shape wrong");
            await service.SaveLookup("brands",brand,new("Hãng QA sửa tên",false));await service.SaveLookup("brands",brand,new("Hãng QA",true));
        });
        await Test("WIN.A05.01 create product POST201 then GET, formula server price and zero H/R; WIN.A04.02 detail fields",async()=>
        {
            product=await service.Save(0,New("QA-W2-ONE"));Assert(product.Variants.Count==2&&product.Images.Count==1,"Aggregate missing fields");
            Assert(product.Variants[0].Price==12000000&&product.Variants[1].Price==12000000,"Server formula incorrect");
            Assert(product.Variants.All(v=>v.OnHand==0&&v.Reserved==0&&v.Version.Length>0),"Initial stock/version incorrect");
            Assert(await db.Inventory.CountAsync()==2&&await db.Movements.CountAsync()==0,"Creation fabricated inventory movement");
        });
        await Test("WIN.A05.02 duplicate code/SKU/combination, invalid cost/rate/image rollback entire aggregate",async()=>
        {
            await Atomic("INVALID_INPUT",()=>service.Save(0,New(product.Code)));
            var distinct=New("QA-W2-DUPSKU");await Atomic("INVALID_INPUT",()=>service.Save(0,distinct with{Variants=[distinct.Variants[0] with{Sku=product.Variants[0].Sku}]}));
            distinct=New("QA-W2-COMB");await Atomic("INVALID_INPUT",()=>service.Save(0,distinct with{Variants=[distinct.Variants[0],distinct.Variants[0] with{Sku="QA-W2-COMB-NEW"}]}));
            foreach(var rate in new[]{-0.01m,10000.01m,0.001m}){var bad=New("QA-W2-RATE");await Atomic("INVALID_INPUT",()=>service.Save(0,bad with{Variants=[bad.Variants[0] with{ProfitPercent=rate}]}));}
            var noCost=New("QA-W2-COST");await Atomic("INVALID_INPUT",()=>service.Save(0,noCost with{Variants=[noCost.Variants[0] with{Cost=0}]}));
            foreach(var url in new[]{"javascript:alert(1)","data:image/png;base64,AA","//host/a","http://host/a","https://user:pass@host/a","/images/../a",""})
            {var bad=New("QA-W2-IMAGE");await Atomic("INVALID_INPUT",()=>service.Save(0,bad with{Images=[new(url,0)]}));}
            var longImage=New("QA-W2-LONG");await Atomic("INVALID_INPUT",()=>service.Save(0,longImage with{ImageUrl="https://example.test/"+new string('a',1001)}));
        });
        await Test("WIN.A05.03/05 client validation supports forms; server accepts image policies and rate boundaries",async()=>
        {
            var good=New("QA-W2-BOUND");Assert(CatalogForms.Validate(good,[1,2]) is null,"Valid draft blocked");
            Assert(CatalogForms.Validate(good with{Variants=[good.Variants[0] with{Sku=""}]},[1]) is not null&&CatalogForms.Validate(good with{Name=""},[1]) is not null,"Required SKU/name unguarded");
            Assert(CatalogForms.Validate(good with{Code=""},[1]) is not null&&CatalogForms.Validate(good,[0]) is not null,"Required fields/quantity unguarded");
            Assert(CatalogForms.Validate(good with{Variants=[good.Variants[0] with{StorageGb=0}]},[1]) is not null,"Storage validation missing");
            foreach(var url in new[]{"javascript:x","data:x","//host/a","http://host/a","/images/../x","https://u:p@host/a"})Assert(!CatalogForms.ImageAllowed(url),"Unsafe form image accepted");
            Assert(CatalogForms.ImageAllowed("")&&!CatalogForms.ImageAllowed("",false),"Optional main/list image rule wrong");
            var maximum=await service.Save(0,good with{ImageUrl="",Variants=[good.Variants[0] with{ProfitPercent=10000}]});Assert(maximum.Variants.Single().Price==1010000000m,"10000% boundary rejected/wrong");
            Assert(CatalogForms.ImageAllowed("https://example.test/a")&&CatalogForms.ImageAllowed("/images/a.png"),"Valid image rejected");
        });
        await Test("WIN.A04.01/03 query brand active paging, names resolve inactive ADMIN lists;404/no-match",async()=>
        {
            var rows=await service.Products("QA-W2",brand,null,1,1);Assert(rows.Items.Count==1&&rows.Total==2&&rows.Page==1,"Product paging/filter failed");
            var next=await service.Products("QA-W2",brand,null,2,1);Assert(next.Items.Single().Id!=rows.Items.Single().Id,"Paging repeated first row");
            Assert((await service.Products("QA-W2",(await service.Lookups("brands")).Single(b=>!b.Active).Id,null)).Total==0,"Brand filter ignored");
            Assert((await service.Products("no-match-"+Guid.NewGuid(),null,null)).Total==0,"No-match not empty");await Reject("NOT_FOUND",()=>service.Product(long.MaxValue));
            Assert((await service.Lookups("categories")).Any(c=>c.Id==category),"Category labels missing");
        });
        await Test("WIN.A06.01/04 missing retained variant rejected, complete old id/version preserved, cost immutable/new stock0",async()=>
        {
            var input=CatalogWorkspace.Request(product);await Atomic("INVALID_INPUT",()=>api.PutAsync($"/api/admin/products/{product.Id}",input with{Variants=[input.Variants[0]]}));
            probe.StripToken=true;await Atomic("INVALID_INPUT",()=>api.PutAsync($"/api/admin/products/{product.Id}",input));probe.StripToken=false;
            var drafts=CatalogForms.Drafts(product);drafts[0].Cost=999;Assert(drafts[0].Request().Cost==product.Variants[0].Cost,"Client changed old cost");
            product=await service.Save(product.Id,input with{Variants=[..input.Variants,new(0,"QA-W2-NEW","Xanh",512,15000000,10,true,null)]});
            Assert(product.Variants.Count==3&&product.Variants.Last().OnHand==0&&product.Variants[0].Cost==10000000,"Old variants lost/new stock nonzero");
            Assert(probe.Records.Where(r=>r.method=="PUT"&&r.path.Contains("products")).All(r=>!r.body.Contains("\"price\"")&&!r.body.Contains("onHand")&&!r.body.Contains("reserved")),"Response DTO leaked into write");
        });
        await Test("WIN.A06.02 stale aggregate PUT409 no overwrite; server409 maps reload message",async()=>
        {
            var stale=CatalogWorkspace.Request(product);product=await service.Save(product.Id,stale with{Name="Đã sửa bởi phiên khác"});
            await Atomic("CONCURRENT_UPDATE",()=>service.Save(product.Id,stale with{Name="Không được ghi đè"}));
            Assert(new AdminApiException("CONCURRENT_UPDATE",409).Message.Contains("vui lòng tải lại"),"Stale message wrong");
        });
        await Test("WIN.A06.03 rate PUT changes server price but real checkout order/payment/history/H/R snapshots stay fixed",async()=>
        {
            // Explicit QA stock setup only. The historical order is then created through real customer HTTP APIs.
            var stock=await db.Inventory.SingleAsync(i=>i.VariantId==product.Variants[0].Id);stock.OnHand=5;await db.SaveChangesAsync();db.ChangeTracker.Clear();
            using var customer=qa.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
            async Task<HttpResponseMessage> Post(string path,object body){var token=await customer.GetFromJsonAsync<TokenResult>("/api/security/token");using var request=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(body)};request.Headers.Add("X-CSRF-TOKEN",token!.Token);var response=await customer.SendAsync(request);Assert(response.IsSuccessStatusCode,"Customer fixture API failed: "+path+" "+response.StatusCode);return response;}
            using(var response=await Post("/api/auth/login",new{username="customer-w1-qa",password=qa.Password,surface="customer"})){}
            using(var response=await Post("/api/cart",new{variantId=product.Variants[0].Id,quantity=1})){}
            using(var response=await Post("/api/orders",new{requestKey=Guid.NewGuid(),recipient="Khách QA",phone="0938123591",address="QA only",lines=new[]{new{variantId=product.Variants[0].Id,quantity=1,price=product.Variants[0].Price}},paymentMethod="COD"})){using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());orderId=json.RootElement.GetProperty("id").GetInt64();}
            async Task<string> OrderSnapshot()=>JsonSerializer.Serialize(new{order=await db.Orders.AsNoTracking().SingleAsync(o=>o.Id==orderId),lines=await db.OrderLines.AsNoTracking().ToListAsync(),payments=await db.Payments.AsNoTracking().ToListAsync(),history=await db.OrderHistory.AsNoTracking().ToListAsync(),stock=await db.Inventory.AsNoTracking().OrderBy(i=>i.VariantId).ToListAsync(),journal=await db.Movements.AsNoTracking().ToListAsync()});
            var before=await OrderSnapshot();var request=CatalogWorkspace.Request(await service.Product(product.Id));request.Variants[0]=request.Variants[0] with{ProfitPercent=25};product=await service.Save(product.Id,request);
            var after=await OrderSnapshot();Assert(product.Variants[0].Price==12500000&&before==after,"Rate edit altered historical money/stock/history");
            priceChanges.Add(new{serverPrice=product.Variants[0].Price,before=JsonSerializer.Deserialize<JsonElement>(before),after=JsonSerializer.Deserialize<JsonElement>(after),unchanged=true});
        });
        await Test("WIN.A07.01/02 full active PUT retains aggregate/history; WIN.A08.02/03 lookup stop hides public only, unique name atomic",async()=>
        {
            var history=await db.OrderHistory.CountAsync();var orders=await db.Orders.CountAsync();
            product=await service.Save(product.Id,CatalogWorkspace.Request(product) with{Active=false});Assert(!product.Active&&(await service.Products(null,null,false)).Items.Any(p=>p.Id==product.Id),"Inactive product missing");
            Assert((await service.Products(null,null,true)).Items.All(p=>p.Id!=product.Id),"Active product filter ignored");
            product=await service.Save(product.Id,CatalogWorkspace.Request(product) with{Active=true});
            await Atomic("INVALID_INPUT",()=>service.SaveLookup("brands",0,new("Hãng QA",true)));await Atomic("INVALID_INPUT",()=>service.SaveLookup("categories",0,new("Điện thoại QA",true)));
            var inactive=(await service.Lookups("brands")).Single(b=>!b.Active).Id;await Atomic("INVALID_INPUT",()=>service.SaveLookup("brands",inactive,new("Hãng QA",false)));
            using var publicClient=qa.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
            foreach(var kind in new[]{"brands","categories"}){var id=kind=="brands"?brand:category;var name=kind=="brands"?"Hãng QA":"Điện thoại QA";await service.SaveLookup(kind,id,new(name,false));using(var response=await publicClient.GetAsync($"/api/catalog/{product.Id}"))Assert(response.StatusCode==HttpStatusCode.NotFound,"Inactive lookup still public");Assert((await service.Lookups(kind)).Any(l=>l.Id==id&&!l.Active),"Inactive ADMIN lookup lost");await service.SaveLookup(kind,id,new(name,true));}
            Assert(await db.OrderHistory.CountAsync()==history&&await db.Orders.CountAsync()==orders&&await db.Products.CountAsync()==2,"Stop/delete damaged data");
        });
        await Test("WIN.BASE.08 W2 uncertain create reconciles exact code before another write; every POST/PUT fresh CSRF",async()=>
        {
            probe.LoseCreate=true;await Reject("UNCERTAIN",()=>service.Save(0,New("QA-W2-LOST")));var writes=probe.Records.Count(r=>r.method is "POST" or "PUT");
            await Reject("UNCERTAIN",()=>service.Save(0,New("QA-W2-LOST")));Assert(writes==probe.Records.Count(r=>r.method is "POST" or "PUT"),"Blind retry sent");
            probe.LoseCreate=false;var found=await service.ReconcileProduct(0,"QA-W2-LOST");Assert(found is not null&&!api.NeedsReconciliation&&await db.Products.CountAsync(p=>p.Code=="QA-W2-LOST")==1,"Reconciliation failed");
            probe.LosePut=true;await Reject("UNCERTAIN",()=>service.Save(found!.Id,CatalogWorkspace.Request(found) with{Name="Đã lưu dù mất phản hồi QA"}));writes=probe.Records.Count(r=>r.method is "POST" or "PUT");
            await Reject("UNCERTAIN",()=>service.Save(found!.Id,CatalogWorkspace.Request(found)));Assert(writes==probe.Records.Count(r=>r.method is "POST" or "PUT"),"Blind PUT retry sent");probe.LosePut=false;
            Assert((await service.ReconcileProduct(found!.Id,found.Code))!.Name=="Đã lưu dù mất phản hồi QA","PUT reconciliation failed");
            Assert(probe.WritesWithoutToken==0&&probe.TokenGets==probe.Records.Count(r=>r.method is "POST" or "PUT")+1,"Missing fresh CSRF per catalog write");
        });
        await Test("WIN.BASE.08 successful201/204 but readback503 retains known id, blocks repeat until GET/list",async()=>
        {
            probe.FailReadback=true;await Reject("READBACK_REQUIRED",()=>service.Save(0,New("QA-W2-READBACK")));Assert(service.PendingProductId.HasValue,"Lost known created id");var writes=probe.Records.Count;
            await Reject("READBACK_REQUIRED",()=>service.Save(0,New("QA-W2-READBACK")));Assert(probe.Records.Count==writes,"Repeated known successful create");
            var recovered=await service.Product(service.PendingProductId!.Value);Assert(recovered.Code=="QA-W2-READBACK"&&!service.PendingProductId.HasValue,"Readback reconciliation failed");
            probe.FailLookupReadback=true;await Reject("READBACK_REQUIRED",()=>service.SaveLookup("brands",0,new("Hãng QA readback",true)));writes=probe.Records.Count;
            await Reject("READBACK_REQUIRED",()=>service.SaveLookup("brands",0,new("Hãng QA readback",true)));Assert(probe.Records.Count==writes,"Repeated lookup creation without authoritative list");
            Assert((await service.Lookups("brands")).Count(b=>b.Name=="Hãng QA readback")==1,"Lookup recovery duplicated data");
        });
        var output=Path.Combine(QaBackend.Root,".artifacts","admin-w2");Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output,"sql-results.json"),JsonSerializer.Serialize(new{qaOnly=true,checks=passed,rejectedWrites,priceChanges,records=trace},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"{passed} admin W2 client checks passed.");
    }
    private sealed record HttpProof(string method,string path,int status,string body);
    private sealed class CatalogProbe:DelegatingHandler
    {
        public List<HttpProof> Records { get; }=[];
        public int WritesWithoutToken,TokenGets;
        public bool LoseCreate,LosePut,FailReadback,FailLookupReadback,StripToken;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.PathAndQuery;var catalog=path.StartsWith("/api/admin/products")||path.StartsWith("/api/admin/lookups");
            if(path=="/api/security/token")TokenGets++;
            var body=catalog&&request.Content is not null?await request.Content.ReadAsStringAsync(ct):"";
            if(catalog&&(request.Method==HttpMethod.Post||request.Method==HttpMethod.Put)&&!request.Headers.Contains("X-CSRF-TOKEN"))WritesWithoutToken++;
            if(StripToken)request.Headers.Remove("X-CSRF-TOKEN");
            var response=await base.SendAsync(request,ct);if(catalog)Records.Add(new(request.Method.Method,path,(int)response.StatusCode,body));
            if((LoseCreate&&path=="/api/admin/products"&&request.Method==HttpMethod.Post||LosePut&&path.StartsWith("/api/admin/products/")&&request.Method==HttpMethod.Put)&&response.IsSuccessStatusCode){response.Dispose();throw new HttpRequestException("QA response lost");}
            if(request.Method==HttpMethod.Get&&((FailReadback&&path.StartsWith("/api/admin/products/"))||(FailLookupReadback&&path.StartsWith("/api/admin/lookups/brands?")))){FailReadback=FailLookupReadback=false;response.Dispose();return new(HttpStatusCode.ServiceUnavailable){Content=JsonContent.Create(new{code="DATABASE_UNAVAILABLE"})};}return response;
        }
    }
}
