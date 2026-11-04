using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Client.Checks;

internal static class OrderChecks
{
    private static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static async Task<AdminApiException> Reject(string code,Func<Task> action){try{await action();throw new InvalidOperationException("Expected "+code);}catch(AdminApiException e){Assert(e.Code==code,"Unexpected error "+e.Code);return e;}}
    public static async Task Run()
    {
        await using var qa=await QaBackend.Create();var gate=new OrderRaceGate();qa.CommandInterceptor=gate;var probe=new OrderProbe();using var api=qa.Api(probe);await api.LoginAsync("admin-w1-qa",qa.Password);var work=new OrdersWorkspace(api);var catalog=new CatalogWorkspace(api);var supply=new SupplyWorkspace(api);await using var db=qa.Open();
        using var customer=qa.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
        async Task<HttpResponseMessage> CustomerPost(string path,object body){var token=await customer.GetFromJsonAsync<TokenResult>("/api/security/token");using var request=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(body)};request.Headers.Add("X-CSRF-TOKEN",token!.Token);return await customer.SendAsync(request);}
        using(var login=await CustomerPost("/api/auth/login",new{username="customer-w1-qa",password=qa.Password,surface="customer"}))Assert(login.StatusCode==HttpStatusCode.OK,"Customer fixture login failed");
        var brand=(await catalog.SaveLookup("brands",0,new("Hãng đơn QA",true))).Single().Id;var category=(await catalog.SaveLookup("categories",0,new("Điện thoại đơn QA",true))).Single().Id;
        var product=await catalog.Save(0,new("W5-PHONE","Điện thoại snapshot QA",brand,category,"QA","QA","",true,[new(0,"W5-A","Đen",128,10000000,20,true,null),new(0,"W5-B","Trắng",256,15000000,20,true,null)],[]));var a=product.Variants[0].Id;var b=product.Variants[1].Id;
        foreach(var variant in product.Variants){var stock=await supply.StockRow(variant.Id,variant.Sku);await supply.Adjust(new(stock,1000,"QA initial stock"));}
        var states=new Dictionary<string,JsonElement>();var matrix=new List<object>();var facts=new List<object>();var passed=0;
        async Task<string> Snapshot(long id)
        {
            var text=JsonSerializer.Serialize(new{order=await db.Orders.AsNoTracking().SingleAsync(o=>o.Id==id),lines=await db.OrderLines.AsNoTracking().Where(o=>o.OrderId==id).OrderBy(o=>o.Id).ToListAsync(),payment=await db.Payments.AsNoTracking().SingleAsync(o=>o.OrderId==id),history=await db.OrderHistory.AsNoTracking().Where(o=>o.OrderId==id).OrderBy(o=>o.Id).ToListAsync(),journal=await db.Movements.AsNoTracking().Where(o=>o.OrderId==id).OrderBy(o=>o.Id).ToListAsync(),stock=await db.Inventory.AsNoTracking().OrderBy(o=>o.VariantId).ToListAsync(),variants=await db.Variants.AsNoTracking().OrderBy(o=>o.Id).ToListAsync(),orderCount=await db.Orders.CountAsync(),paymentCount=await db.Payments.CountAsync(),historyCount=await db.OrderHistory.CountAsync(),journalCount=await db.Movements.CountAsync()});
            var key=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));states.TryAdd(key,JsonSerializer.Deserialize<JsonElement>(text));return key;
        }
        async Task<AdminOrder> New(string state="CHO_XAC_NHAN",string recipient="Người nhận snapshot QA")
        {
            foreach(var line in new[]{new{variantId=a,quantity=2},new{variantId=b,quantity=3}}){using var add=await CustomerPost("/api/cart",line);Assert(add.IsSuccessStatusCode,"QA cart failed");}
            using var created=await CustomerPost("/api/orders",new{requestKey=Guid.NewGuid(),recipient,phone="0938123591",address="Địa chỉ snapshot QA",paymentMethod="COD",lines=new[]{new{variantId=a,quantity=2,price=12000000},new{variantId=b,quantity=3,price=18000000}}});Assert(created.StatusCode==HttpStatusCode.Created,"QA order failed");var id=(await created.Content.ReadFromJsonAsync<Created>())!.Id;var row=await work.Read(id);
            async Task Step(OrderAction action){row=await work.Execute(new(row,action,"QA fixture"));}
            if(state=="DA_HUY")await Step(OrderAction.Cancel);
            else if(state!="CHO_XAC_NHAN"){await Step(OrderAction.Confirm);if(state!="DA_XAC_NHAN"){await Step(OrderAction.Dispatch);if(state=="HOAN_TAT")await Step(OrderAction.Complete);if(state=="GIAO_THAT_BAI")await Step(OrderAction.Fail);}}
            Assert(row.Status==state,"QA state setup wrong");return row;
        }
        async Task Test(string ids,Func<Task> run){await run();passed++;Console.WriteLine("PASS "+ids);}
        // Independent literal oracle from section4, not derived from the UI policy under test.
        var allowed=new Dictionary<string,OrderAction[]>{["CHO_XAC_NHAN"]=[OrderAction.CustomerCancel,OrderAction.Confirm,OrderAction.Cancel],["DA_XAC_NHAN"]=[OrderAction.Cancel,OrderAction.Dispatch,OrderAction.Invoice],["DANG_GIAO"]=[OrderAction.Complete,OrderAction.Fail,OrderAction.Invoice],["HOAN_TAT"]=[OrderAction.Invoice],["DA_HUY"]=[],["GIAO_THAT_BAI"]=[OrderAction.ReceiveReturn]};
        await Test("WIN.A19.01/A20.01/02/04 matrix48 actual API cells; denied SQL unchanged; exact two-variant H/R/COD/history effects",async()=>
        {
            foreach(var(state,actions)in allowed)foreach(var action in Enum.GetValues<OrderAction>())
            {
                var row=await New(state);var yes=actions.Contains(action);Assert(OrderPresentation.Allowed(state,false,action)==yes,"Display matrix differs from section4");var before=await Snapshot(row.Id);var h=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync();var journals=await db.Movements.CountAsync(x=>x.OrderId==row.Id);var history=await db.OrderHistory.CountAsync(x=>x.OrderId==row.Id);var command=new OrderCommand(row.Version,Guid.NewGuid(),"  QA matrix reason  ");var status=0;string? code=null;
                if(action==OrderAction.CustomerCancel){using var response=await CustomerPost($"/api/orders/{row.Id}/cancel",command);status=(int)response.StatusCode;if(!response.IsSuccessStatusCode)code=(await response.Content.ReadFromJsonAsync<Error>())!.Code;}
                else try{if(action==OrderAction.Invoice)await work.Invoice(row.Id);else await work.Execute(new(row,action,"  QA matrix reason  "));status=action==OrderAction.Invoice?200:204;}catch(AdminApiException error){status=error.Status;code=error.Code;}
                var after=await Snapshot(row.Id);
                if(!yes){Assert(status==409&&code=="INVALID_STATE"&&before==after,"Denied matrix cell changed SQL or wrong error");}
                else if(action==OrderAction.Invoice){Assert(status==200&&before==after,"Invoice changed SQL");}
                else
                {
                    Assert(status==204,"Allowed matrix cell rejected");var read=await work.Read(row.Id);var stock=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).ToListAsync();var target=action switch{OrderAction.Confirm=>"DA_XAC_NHAN",OrderAction.Cancel or OrderAction.CustomerCancel=>"DA_HUY",OrderAction.Dispatch=>"DANG_GIAO",OrderAction.Complete=>"HOAN_TAT",_=>"GIAO_THAT_BAI"};Assert(read.Status==target&&read.Version!=row.Version&&read.ReturnReceived==(action==OrderAction.ReceiveReturn),"Transition state/version/return wrong");
                    for(var i=0;i<h.Count;i++){var q=h[i].VariantId==a?2:3;var dh=action==OrderAction.Dispatch?-q:action==OrderAction.ReceiveReturn?q:0;var dr=action is OrderAction.Dispatch or OrderAction.Cancel or OrderAction.CustomerCancel?-q:0;Assert(stock[i].OnHand==h[i].OnHand+dh&&stock[i].Reserved==h[i].Reserved+dr,"Matrix inventory delta wrong");}
                    var pay=await db.Payments.AsNoTracking().SingleAsync(x=>x.OrderId==row.Id);var order=await db.Orders.AsNoTracking().SingleAsync(x=>x.Id==row.Id);var complete=action==OrderAction.Complete;Assert(pay.Status==(complete?"DA_THU":"CHUA_THU")&&pay.Amount==(complete?read.Total:0)&&OrderPresentation.Collected(read)==pay.Amount&&OrderPresentation.CollectedAt(read)?.UtcDateTime==pay.CollectedAt&&(!complete||pay.CollectedAt==order.CompletedAt&&pay.ActorId==qa.AdminId),"COD invariant/display wrong");
                    Assert(await db.OrderHistory.CountAsync(x=>x.OrderId==row.Id)==history+1&&await db.Movements.CountAsync(x=>x.OrderId==row.Id)==journals+(action is OrderAction.Dispatch or OrderAction.ReceiveReturn or OrderAction.Cancel or OrderAction.CustomerCancel?2:0),"History/journal count wrong");
                }
                matrix.Add(new{state,action=action.ToString(),allowed=yes,status,code,before,after});
            }
        });
        await Test("WIN.A19.02/03/A20.03/05 optional return reason, repeat/stale denial and required cancel/failure reasons atomic",async()=>
        {
            foreach(var action in new[]{OrderAction.Cancel,OrderAction.Fail}){var row=await New(action==OrderAction.Fail?"DANG_GIAO":"CHO_XAC_NHAN");var before=await Snapshot(row.Id);await Reject("INVALID_INPUT",()=>work.Execute(new(row,action," ")));Assert(before==await Snapshot(row.Id),"Missing reason changed SQL");}
            var longReason=await New("GIAO_THAT_BAI");var longBefore=await Snapshot(longReason.Id);await Reject("INVALID_INPUT",()=>work.Execute(new(longReason,OrderAction.ReceiveReturn,new string('x',1001))));Assert(longBefore==await Snapshot(longReason.Id),"Overlong optional return reason changed SQL");
            foreach(var reason in new string?[]{null,"  ","  Hàng thực nhận QA  "})
            {
                var row=await New("GIAO_THAT_BAI");var op=new OrderOperation(row,OrderAction.ReceiveReturn,reason);var read=await work.Execute(op);var before=await Snapshot(row.Id);await Reject("CONCURRENT_UPDATE",()=>api.CommandAsync($"/api/admin/orders/{row.Id}/receive-return",op.Request(),op.Intent));await Reject("INVALID_STATE",()=>work.Execute(new(read,OrderAction.ReceiveReturn)));Assert(before==await Snapshot(row.Id)&&!OrderPresentation.Allowed(read.Status,read.ReturnReceived,OrderAction.ReceiveReturn),"Repeated return changed stock or stayed visible");var last=read.History.Last();Assert(last.Reason==(reason?.Trim()??"")&&last.From==last.To&&last.Actor=="admin-w1-qa","Return reason/history wrong");facts.Add(new{returnReason=reason,returnedOnce=true,before,after=await Snapshot(row.Id)});
            }
        });
        await Test("WIN.A19.04 RACE customer cancel/admin confirm both gated on distinct SQL connections; loser409 then authoritative refresh",async()=>
        {
            var row=await New();var before=await Snapshot(row.Id);var intent=new OrderOperation(row,OrderAction.Confirm);gate.Enabled=true;var admin=work.Execute(intent);var cancel=CustomerPost($"/api/orders/{row.Id}/cancel",new OrderCommand(row.Version,Guid.NewGuid(),"Khách hủy tranh chấp QA"));
            try{await gate.Ready.Task.WaitAsync(TimeSpan.FromSeconds(20));Assert(gate.Arrivals==2&&gate.Connections.Count==2&&!admin.IsCompleted&&!cancel.IsCompleted,"No real contention");gate.ReleaseCustomer.TrySetResult();using var response=await cancel;Assert(response.StatusCode==HttpStatusCode.NoContent,"Customer did not win race");gate.ReleaseAdmin.TrySetResult();await Reject("CONCURRENT_UPDATE",()=>admin);var read=await work.Resolve(intent);Assert(read.Status=="DA_HUY"&&intent.Finished&&!intent.NeedsReadback,"Conflict did not refresh cancelled order");facts.Add(new{raceArrivals=2,distinctConnections=2,winner="customer-cancel204",loser="admin-confirm409 CONCURRENT_UPDATE",before,after=await Snapshot(row.Id)});}finally{gate.ReleaseCustomer.TrySetResult();gate.ReleaseAdmin.TrySetResult();gate.Enabled=false;}
        });
        await Test("WIN.A18.01/02/03/A21.01/02 actual query/status/date boundaries/page and invoice snapshot reads leave SQL unchanged",async()=>
        {
            var date=new DateOnly(2026,6,10);var start=new DateTime(2026,6,9,17,0,0,DateTimeKind.Utc);var ids=new List<long>();foreach(var at in new[]{start.AddTicks(-1),start,start.AddDays(1).AddTicks(-1),start.AddDays(1)}){var row=await New(recipient:"W5-DATE-QA");ids.Add(row.Id);await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.DON_HANG SET CreatedAt={at} WHERE Id={row.Id}");}
            var before=await Snapshot(ids[1]);var dates=await work.List("W5-DATE-QA",null,date,date,size:100);Assert(dates.Items.Select(x=>x.Id).Order().SequenceEqual(new[]{ids[1],ids[2]}.Order()),"UTC+7 inclusive end boundary wrong");var first=await work.List(null,null,null,null);var next=await work.List(null,null,null,null,2);Assert(first.Items.Count==20&&next.Items.Count==20&&first.Items[0].Id!=next.Items[0].Id,"Order page wrapper wrong");foreach(var term in new[]{first.Items[0].Code,"snapshot","Khách QA","0938123591"})Assert((await work.List(term,null,null,null)).Total>0,"Query contract branch failed");Assert((await work.List("no-match-"+Guid.NewGuid(),null,null,null)).Total==0,"Order empty wrong");Assert((await work.List(null,"HOAN_TAT",null,null)).Items.All(x=>x.Status=="HOAN_TAT"),"Order status wrong");Assert(before==await Snapshot(ids[1]),"List changed SQL");
            var invoiceOrder=await New("DA_XAC_NHAN");before=await Snapshot(invoiceOrder.Id);var invoice=await work.Invoice(invoiceOrder.Id);Assert(invoice.Title=="HÓA ĐƠN BÁN HÀNG"&&invoice.Notice.Contains("không phải hóa đơn điện tử thuế")&&invoice.Order.Lines[0].Price==12000000&&OrderPresentation.InvoiceText(invoice).Contains(invoice.Notice)&&before==await Snapshot(invoiceOrder.Id),"Invoice snapshot/notice/read changed SQL");facts.Add(new{date=AdminFormatting.Date(date),includedIds=dates.Items.Select(x=>x.Id),listReadOnly=true,invoiceReadOnly=true});
        });
        await Test("WIN.A18.04 actual customer gets403; client maps403 without private text; lost write reply resolves GET once",async()=>
        {
            using var forbidden=await customer.GetAsync("/api/admin/orders");Assert(forbidden.StatusCode==HttpStatusCode.Forbidden,"Missing privilege not403");probe.ForbidRead=true;var error=await Reject("FORBIDDEN",()=>work.List(null,null,null,null));Assert(error.Status==403&&error.Message.Contains("không có quyền"),"Client forbidden mapping wrong");probe.ForbidRead=false;
            var row=await New();var operation=new OrderOperation(row,OrderAction.Confirm);probe.LosePath=$"/api/admin/orders/{row.Id}/confirm";await Reject("UNCERTAIN",()=>work.Execute(operation));var count=probe.Writes;await Reject("READBACK_REQUIRED",()=>work.Execute(operation));probe.LosePath=null;var resolved=await work.Resolve(operation);Assert(resolved.Status=="DA_XAC_NHAN"&&operation.Finished&&probe.Writes==count,"Unknown order reply repeated write");facts.Add(new{unknownOrderReadbackOnce=true,operationKey=operation.Intent.Key});
            row=await New();operation=new(row,OrderAction.Confirm);var before=await Snapshot(row.Id);probe.LoseBeforePath=$"/api/admin/orders/{row.Id}/confirm";await Reject("UNCERTAIN",()=>work.Execute(operation));Assert(before==await Snapshot(row.Id),"Unsent request changed SQL");probe.LoseBeforePath=null;resolved=await work.Resolve(operation);var key=operation.Intent.Key;Assert(resolved.Version==row.Version&&!operation.Finished,"Unsent request cannot be retried after readback");resolved=await work.Execute(operation);Assert(operation.Intent.Key==key&&resolved.Status=="DA_XAC_NHAN","Retry changed intention key");facts.Add(new{unsentIntentReconciledBeforeRetry=true,operationKey=key,before,after=await Snapshot(row.Id)});
        });
        Assert(probe.Tokens==probe.Writes&&probe.MissingTokens==0,"Write missing fresh CSRF");var output=Path.Combine(QaBackend.Root,".artifacts","admin-w5");Directory.CreateDirectory(output);await File.WriteAllTextAsync(Path.Combine(output,"sql-states.json"),JsonSerializer.Serialize(states));await File.WriteAllTextAsync(Path.Combine(output,"sql-results.json"),JsonSerializer.Serialize(new{qaOnly=true,checks=passed,matrixCells=matrix.Count,matrix,facts,http=probe.Records,snapshotFile="sql-states.json",sqlSnapshotCount=states.Count},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"{passed} admin W5 client checks passed (48 matrix cells).");
    }
    private sealed record Created(long Id);
    private sealed record Error(string Code);
    private sealed record HttpProof(string Method,string Path,int Status,string Body);
    private sealed class OrderProbe:DelegatingHandler
    {
        public int Writes,Tokens,MissingTokens;public string? LosePath,LoseBeforePath;public bool ForbidRead;public List<HttpProof> Records { get; }=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.PathAndQuery;if(path=="/api/security/token")Tokens++;if(request.Method!=HttpMethod.Get){Writes++;if(!request.Headers.Contains("X-CSRF-TOKEN"))MissingTokens++;}
            if(path==LoseBeforePath&&request.Method!=HttpMethod.Get)throw new HttpRequestException("QA request lost before server");
            if(ForbidRead&&path.StartsWith("/api/admin/orders"))return new(HttpStatusCode.Forbidden){Content=JsonContent.Create(new{code="FORBIDDEN"})};
            var body=path.StartsWith("/api/admin/orders")&&request.Content is not null?await request.Content.ReadAsStringAsync(ct):"";var response=await base.SendAsync(request,ct);if(path.StartsWith("/api/admin/orders"))Records.Add(new(request.Method.Method,path,(int)response.StatusCode,body));if(path==LosePath&&request.Method!=HttpMethod.Get&&response.IsSuccessStatusCode){response.Dispose();throw new HttpRequestException("QA order reply lost after commit");}return response;
        }
    }
}
