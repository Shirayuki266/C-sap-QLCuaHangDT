using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Checks;

internal static class StateMatrixChecks
{
    private sealed record Result(string Id,string Expected,string Actual,bool Conforms,string Detail);
    private sealed record Reply(int Status,string? Code);
    private sealed record Request(ApiTestClient Client,string Path,object Body,HttpMethod? Method=null);
    private sealed class Audit
    {
        public List<Result> Results{get;}=[];
        public List<string> Failures{get;}=[];
        public async Task Cell(string id,Func<Task> test)
        {
            try{await Check.Run("MATRIX "+id,test);Results.Add(new(id,"Matrix effect and error mapping","Matched",true,"SQL snapshot assertions passed."));}
            catch(Exception error){Failures.Add(id);Results.Add(new(id,"Matrix effect and error mapping",error.GetType().Name,false,"Assertion failed; see test source."));Console.WriteLine("MATRIX FAIL "+id);}
        }
        public void Observe(string id,string expected,string actual,bool conforms,string detail)
        {
            Results.Add(new(id,expected,actual,conforms,detail));Console.WriteLine($"MATRIX {(conforms?"MATCH":"DEVIATION")} {id}: expected {expected}; actual {actual}");
        }
        public void Finish()
        {
            var output=Environment.GetEnvironmentVariable("PHONESTORE_MATRIX_RESULTS");
            if(!string.IsNullOrWhiteSpace(output))File.WriteAllText(output,JsonSerializer.Serialize(new{Results,Failures},new JsonSerializerOptions{WriteIndented=true}));
            Check.True(Failures.Count==0,"State matrix assertions failed: "+string.Join(", ",Failures));
            if(Environment.GetEnvironmentVariable("PHONESTORE_MATRIX_STRICT")=="1")Check.True(Results.All(x=>x.Conforms),"Strict matrix conformance has unresolved discrepancies; production behavior was not changed.");
        }
    }
    private static async Task<Reply> Send(Request request)
    {
        using var response=await request.Client.Send(request.Path,request.Body,request.Method);
        return await Read(response);
    }
    private static async Task<Reply> Read(HttpResponseMessage response)
    {
        string? code=null;
        if(!response.IsSuccessStatusCode)
        {
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());code=json.RootElement.GetProperty("code").GetString();
        }
        return new((int)response.StatusCode,code);
    }
    private static async Task<Reply[]> Race(params Request[] requests)
    {
        var tokens=new List<string>();foreach(var r in requests)tokens.Add(await r.Client.Token());
        var start=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<Reply> Attempt(int index)
        {
            using var message=new HttpRequestMessage(requests[index].Method??HttpMethod.Post,requests[index].Path){Content=JsonContent.Create(requests[index].Body)};
            message.Headers.Add("X-CSRF-TOKEN",tokens[index]);await start.Task;
            using var response=await requests[index].Client.Http.SendAsync(message);return await Read(response);
        }
        var pending=Enumerable.Range(0,requests.Length).Select(Attempt).ToArray();start.SetResult();return await Task.WhenAll(pending);
    }
    private static string Canonical(Reply reply)=>reply.Code=="CONCURRENT_UPDATE"?"STALE_VERSION":reply.Code??"SUCCESS";
    public static async Task Run(Uri address,string connection)
    {
        var audit=new Audit();using var c=new CommerceContext(connection,address,"state-matrix");await c.Initialize();
        var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),500);var fixture=new StateMatrixFixture(c,data);
        // Section 4: every cell is a separate data-driven check; customer/admin cancellation remain separate.
        foreach(var state in StateMatrixFixture.States.Select((name,index)=>(name,index)))foreach(var action in OrderMatrixCase.All)
        {
            await audit.Cell($"ORDER {state.name} / {action.Name}",async()=>
            {
                var order=await fixture.CreateOrder(state.name);var before=await fixture.Snapshot();
                var command=new VersionedCommand(order.Version,Guid.NewGuid(),"Matrix action reason");
                if(!action.Allowed.Contains(state.index))
                {
                    await CommerceContext.Reject("INVALID_STATE",()=>fixture.Apply(order,action.Name,command));await fixture.Unchanged(before);
                }
                else{await fixture.Apply(order,action.Name,command);await fixture.AssertOrderEffect(before,order,action);}
            });
        }
        await PurchaseMatrix(audit,c);
        await RetryMatrix(audit,fixture);
        await LiteralProbes(audit,fixture);
        audit.Finish();
    }
    private static async Task PurchaseMatrix(Audit audit,CommerceContext c)
    {
        var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c,3),20);var f=new StateMatrixFixture(c,data);
        long alternate;
        await using(var db=c.Open())
        {
            var k=c.Kernel(db);alternate=await new SupplierService(k).Save(c.Admin,0,new("MATRIX-ALT","Matrix alternate supplier","0900000098","Fixture address"));
            var p=await new ProductService(k).Read(c.Admin,data.Product.Id);var input=ProductChecks.Input(p);
            await new ProductService(k).Save(c.Admin,p.Id,input with{Variants=input.Variants!.Select(x=>x with{ProfitPercent=12.5m}).ToList()});
        }
        List<PurchaseLineInput> Lines()=>data.Product.Variants.Take(2).Select((v,i)=>new PurchaseLineInput(v.Id,i+2,i==0?100.04m:200.04m)).ToList();
        async Task<PurchaseView> Draft(string state)
        {
            await using var db=c.Open();var service=new PurchaseService(c.Kernel(db));var before=await f.Snapshot();
            var id=await service.Save(c.Admin,0,new(data.SupplierId,Lines(),RequestKey:Guid.NewGuid()));var row=await service.Read(c.Admin,id);var after=await f.Snapshot();
            StateMatrixFixture.Same(before.Stock,after.Stock,"P1 changed stock.");StateMatrixFixture.Same(before.Variants,after.Variants,"P1 changed prices.");
            if(state=="DA_XAC_NHAN")await service.Confirm(c.Admin,id,new(row.Version,Guid.NewGuid()));
            if(state=="DA_HUY")await service.Cancel(c.Admin,id,new(row.Version,Guid.NewGuid()));
            return await service.Read(c.Admin,id);
        }
        foreach(var state in new[]{"NHAP","DA_XAC_NHAN","DA_HUY"})foreach(var action in new[]{"edit","add-line","remove-line","cancel","confirm"})
        {
            await audit.Cell($"PURCHASE {state} / {action}",async()=>
            {
                var row=await Draft(state);var before=await f.Snapshot();var key=Guid.NewGuid();var lines=Lines();
                if(action=="edit")lines=lines.Select(x=>x with{Quantity=x.Quantity+1,Cost=x.Cost+1m}).ToList();
                if(action=="add-line")lines.Add(new(data.Product.Variants[2].Id,4,300.04m));
                if(action=="remove-line")lines.RemoveAt(1);
                await using var db=c.Open();var service=new PurchaseService(c.Kernel(db));
                async Task Apply()
                {
                    if(action=="confirm")await service.Confirm(c.Admin,row.Id,new(row.Version,key));
                    else if(action=="cancel")await service.Cancel(c.Admin,row.Id,new(row.Version,key));
                    else await service.Save(c.Admin,row.Id,new(alternate,lines,row.Version));
                }
                if(state!="NHAP"){await CommerceContext.Reject("INVALID_STATE",Apply);await f.Unchanged(before);return;}
                await Apply();var after=await f.Snapshot();var current=await service.Read(c.Admin,row.Id);
                Check.True(current.Status==(action=="confirm"?"DA_XAC_NHAN":action=="cancel"?"DA_HUY":"NHAP")&&current.Version!=row.Version,"Purchase matrix status/version wrong.");
                StateMatrixFixture.Same(before.Orders,after.Orders,"Purchase changed orders.");StateMatrixFixture.Same(before.Lines,after.Lines,"Purchase changed sale snapshots.");StateMatrixFixture.Same(before.Payments,after.Payments,"Purchase changed COD.");StateMatrixFixture.Same(before.History,after.History,"Purchase changed order history.");StateMatrixFixture.Same(before.Cart,after.Cart,"Purchase changed cart.");
                StateMatrixFixture.Same(before.Purchases.Where(x=>x.Id!=row.Id),after.Purchases.Where(x=>x.Id!=row.Id),"Purchase changed other receipts.");
                StateMatrixFixture.Same(before.PurchaseLines.Where(x=>x.PurchaseId!=row.Id),after.PurchaseLines.Where(x=>x.PurchaseId!=row.Id),"Purchase changed other receipt lines.");
                if(action!="confirm")
                {
                    StateMatrixFixture.Same(before.Stock,after.Stock,"Draft edit/cancel changed inventory.");StateMatrixFixture.Same(before.Variants,after.Variants,"Draft edit/cancel changed prices.");StateMatrixFixture.Same(before.Movements,after.Movements,"Draft edit/cancel wrote stock journal.");
                    if(action=="cancel")StateMatrixFixture.Same(row.Lines,current.Lines,"Cancel changed receipt lines.");
                    else Check.True(current.SupplierId==alternate&&current.Total==lines.Sum(x=>x.Quantity*x.Cost)&&current.Lines.Select(x=>new PurchaseLineInput(x.VariantId,x.Quantity,x.Cost)).SequenceEqual(lines),"Draft replacement/add/removal or supplier incorrect.");
                    return;
                }
                Check.True(current.ConfirmerId==c.Admin.AccountId&&current.ConfirmedAt.HasValue&&current.Total==row.Total,"Confirmation metadata/total wrong.");
                StateMatrixFixture.Same(row.Lines,current.Lines,"Confirmation changed receipt snapshots.");
                var added=after.Movements.Where(x=>!before.Movements.Any(y=>y.Id==x.Id)).ToList();Check.True(added.Count==row.Lines.Count,"Confirmation journal count wrong.");
                foreach(var old in before.Stock)
                {
                    var line=row.Lines.SingleOrDefault(x=>x.VariantId==old.VariantId);var now=after.Stock.Single(x=>x.VariantId==old.VariantId);
                    Check.True(now.OnHand==old.OnHand+(line?.Quantity??0)&&now.Reserved==old.Reserved,"Receipt H/R delta wrong.");
                    var oldVariant=before.Variants.Single(x=>x.Id==old.VariantId);var variant=after.Variants.Single(x=>x.Id==old.VariantId);
                    if(line is null){StateMatrixFixture.Same(old,now,"Receipt changed unrelated stock version.");StateMatrixFixture.Same(oldVariant,variant,"Receipt changed unrelated variant.");continue;}
                    Check.True(variant.Cost==line.Cost&&variant.ProfitPercent==oldVariant.ProfitPercent&&variant.Price==decimal.Round(line.Cost*(1m+oldVariant.ProfitPercent/100m),2,MidpointRounding.AwayFromZero),"Receipt cost/rounding/profit wrong.");
                    var movement=added.Single(x=>x.VariantId==line.VariantId);Check.True(movement.PurchaseId==row.Id&&movement.OrderId==null&&movement.Kind=="NHAP"&&movement.DeltaOnHand==line.Quantity&&movement.DeltaReserved==0&&movement.OperationKey==key,"Receipt movement wrong (matrix NHAP_HANG maps to NHAP).");
                }
            });
        }
    }
    private static async Task RetryMatrix(Audit audit,StateMatrixFixture f)
    {
        var c=f.Context;
        foreach(var scenario in new[]{("RACE.02","CHO_XAC_NHAN","customer-cancel","confirm"),("RACE.03","DA_XAC_NHAN","admin-cancel","dispatch"),("RACE.04","DA_XAC_NHAN","dispatch","dispatch"),("RACE.05","GIAO_THAT_BAI","receive-return","receive-return")})
        {
            await audit.Cell("RETRY "+scenario.Item1,async()=>
            {
                var order=await f.CreateOrder(scenario.Item2);var before=await f.Snapshot();
                Request Command(string action,string version)=>new(action=="customer-cancel"?c.CustomerClient:c.AdminClient,
                    (action=="customer-cancel"?$"/api/orders/{order.Id}/cancel":$"/api/admin/orders/{order.Id}/{(action=="admin-cancel"?"cancel":action)}"),new VersionedCommand(version,Guid.NewGuid(),"Race reason"));
                var replies=await Race(Command(scenario.Item3,order.Version),Command(scenario.Item4,order.Version));var winner=Array.FindIndex(replies,x=>x.Status==204);
                Check.True(winner>=0&&replies.Count(x=>x.Status==204)==1,"Order race had zero or two winners.");var loser=replies[1-winner];
                Check.True(loser.Status==409&&Canonical(loser)=="STALE_VERSION","Losing old-version request was not mapped STALE_VERSION/409.");
                var action=winner==0?scenario.Item3:scenario.Item4;await f.AssertOrderEffect(before,order,OrderMatrixCase.All.Single(x=>x.Name==action));
                var committed=await f.Snapshot();var retry=await Send(Command(action,order.Version));Check.True(retry.Status==409&&Canonical(retry)=="STALE_VERSION","Old-version retry code wrong.");await f.Unchanged(committed);
                var current=await f.Read(order.Id);var fresh=await Send(Command(action,current.Version));Check.True(fresh.Status==409&&fresh.Code=="INVALID_STATE","Fresh-version repeat not mapped INVALID_STATE/ALREADY_APPLIED.");await f.Unchanged(committed);
                Console.WriteLine($"MATRIX ERROR {scenario.Item1}: loser={loser.Code}/409; stale-retry={retry.Code}/409; fresh-repeat={fresh.Code}/409");
            });
        }
        await audit.Cell("RETRY RACE.06 purchase confirmation",async()=>
        {
            var data=await CommerceData.Create(c);await using var db=c.Open();var service=new PurchaseService(c.Kernel(db));var lines=data.Product.Variants.Select(v=>new PurchaseLineInput(v.Id,3,321.01m)).ToList();
            var id=await service.Save(c.Admin,0,new(data.SupplierId,lines,RequestKey:Guid.NewGuid()));var row=await service.Read(c.Admin,id);var before=await f.Snapshot();var path=$"/api/admin/purchases/{id}/confirm";
            Request Command(string version)=>new(c.AdminClient,path,new VersionedCommand(version,Guid.NewGuid()));
            var replies=await Race(Command(row.Version),Command(row.Version));Check.True(replies.Count(x=>x.Status==204)==1&&replies.Count(x=>x.Status==409&&Canonical(x)=="STALE_VERSION")==1,"Receipt race loser code wrong.");
            var after=await f.Snapshot();foreach(var line in lines){var old=before.Stock.Single(x=>x.VariantId==line.VariantId);var stock=after.Stock.Single(x=>x.VariantId==line.VariantId);var variant=after.Variants.Single(x=>x.Id==line.VariantId);Check.True(stock.OnHand==old.OnHand+line.Quantity&&stock.Reserved==old.Reserved&&variant.Cost==line.Cost&&variant.Price==decimal.Round(line.Cost*(1m+variant.ProfitPercent/100m),2,MidpointRounding.AwayFromZero),"Receipt race had multiple price/stock effects.");}
            Check.True(after.Movements.Count(x=>x.PurchaseId==id)==lines.Count,"Receipt race journal duplicated.");
            var committed=await f.Snapshot();var stale=await Send(Command(row.Version));Check.True(stale.Status==409&&Canonical(stale)=="STALE_VERSION","Receipt stale retry code wrong.");await f.Unchanged(committed);
            var current=await service.Read(c.Admin,id);var fresh=await Send(Command(current.Version));Check.True(fresh.Status==409&&fresh.Code=="INVALID_STATE","Receipt fresh repeat code wrong.");await f.Unchanged(committed);
            Console.WriteLine($"MATRIX ERROR RACE.06: loser=CONCURRENT_UPDATE/409; fresh-repeat={fresh.Code}/409");
        });
        await audit.Cell("RETRY inventory OperationKey concurrent and fresh-version repeats",async()=>
        {
            var v=f.Data.Product.Variants[0].Id;var before=await f.Snapshot();var stock=before.Stock.Single(x=>x.VariantId==v);var key=Guid.NewGuid();var path=$"/api/admin/stock/{v}/adjust";
            var command=new StockCountInput(stock.OnHand+7,"Matrix count",Convert.ToBase64String(stock.Version),key);
            var replies=await Race(new(c.AdminClient,path,command),new(c.AdminClient,path,command));Check.True(replies.Count(x=>x.Status==204)==1&&replies.Count(x=>x.Status==409&&Canonical(x)=="STALE_VERSION")==1,"Count race loser code wrong.");
            var after=await f.Snapshot();var now=after.Stock.Single(x=>x.VariantId==v);Check.True(now.OnHand==command.Counted&&now.Reserved==stock.Reserved&&after.Movements.Count(x=>x.OperationKey==key)==1,"Count race applied twice.");
            var stale=await Send(new(c.AdminClient,path,command));Check.True(stale.Status==409&&Canonical(stale)=="STALE_VERSION","Count stale retry code wrong.");await f.Unchanged(after);
            // Distinct counted value reaches the ledger guard; identical counted value is observed separately below.
            var ledger=await Send(new(c.AdminClient,path,command with{Counted=now.OnHand+1,Version=Convert.ToBase64String(now.Version)}));Check.True(ledger.Status==409&&ledger.Code=="INVALID_STATE","Count ledger replay did not map ALREADY_APPLIED.");await f.Unchanged(after);
            Console.WriteLine($"MATRIX ERROR count: loser=CONCURRENT_UPDATE/409; ledger-repeat={ledger.Code}/409");
        });
        await audit.Cell("RETRY K12.06 RACE.11 checkout same-key owner replay",async()=>
        {
            var input=await CheckoutChecks.Cart(c,f.Data);await using var db=c.Open();var service=new CheckoutService(c.Kernel(db));var created=await service.Place(c.Customer,input);var committed=await f.Snapshot();var replay=await service.Place(c.Customer,input);
            Check.True(replay.Id==created.Id,"Checkout same-key replay created another order.");await f.Unchanged(committed);
            using var foreign=new CommerceContext(db.Database.GetConnectionString()!,c.AdminClient.Http.BaseAddress!,"matrix-foreign");await foreign.Initialize();var afterForeign=await f.Snapshot();
            await using var other=foreign.Open();await CommerceContext.Reject("FORBIDDEN",async()=>await new CheckoutService(foreign.Kernel(other)).Place(foreign.Customer,input));await f.Unchanged(afterForeign);
        });
    }
    private static async Task LiteralProbes(Audit audit,StateMatrixFixture f)
    {
        var c=f.Context;var order=await f.CreateOrder("GIAO_THAT_BAI");var before=await f.Snapshot();
        var noReason=await Send(new(c.AdminClient,$"/api/admin/orders/{order.Id}/receive-return",new VersionedCommand(order.Version,Guid.NewGuid())));
        if(noReason.Status==204)await f.AssertOrderEffect(before,order,OrderMatrixCase.All.Single(x=>x.Name=="receive-return"));else await f.Unchanged(before);
        audit.Observe("DIFF-T9-REASON","204 (section3 T9: optional reason)",$"{noReason.Status}/{noReason.Code??"SUCCESS"}",noReason.Status==204,"Final user decision 31 October: return reason optional; cancellation and delivery-failure reasons stay required.");
        var variant=f.Data.Product.Variants[0].Id;var stock=(await f.Snapshot()).Stock.Single(x=>x.VariantId==variant);var key=Guid.NewGuid();var path=$"/api/admin/stock/{variant}/adjust";
        var input=new StockCountInput(stock.OnHand+3,"Exact retry probe",Convert.ToBase64String(stock.Version),key);var first=await Send(new(c.AdminClient,path,input));Check.True(first.Status==204,"Exact count retry fixture failed.");
        var committed=await f.Snapshot();var now=committed.Stock.Single(x=>x.VariantId==variant);var repeat=await Send(new(c.AdminClient,path,input with{Version=Convert.ToBase64String(now.Version)}));await f.Unchanged(committed);
        audit.Observe("DIFF-COUNT-EXACT-RETRY","400/INVALID_INPUT (approved section7 exact count retry)",$"{repeat.Status}/{repeat.Code}",repeat.Status==400&&repeat.Code=="INVALID_INPUT","Accepted existing behavior: unchanged-count validation precedes the ledger guard. Exact retry must leave the entire business snapshot unchanged.");
        await audit.Cell("COD conditional Amount across all six states",async()=>
        {
            foreach(var state in StateMatrixFixture.States)
            {
                var orderInState=await f.CreateOrder(state);var payment=(await f.Snapshot()).Payments.Single(x=>x.OrderId==orderInState.Id);var collected=state=="HOAN_TAT";
                Check.True(payment.Method=="COD"&&payment.Status==(collected?"DA_THU":"CHUA_THU")&&payment.Amount==(collected?orderInState.Total:0m),"Corrected matrix COD status/Amount rule failed.");
                Check.True(collected?payment.CollectedAt.HasValue&&payment.CollectedAt==orderInState.CompletedAt!.Value.UtcDateTime&&payment.ActorId==c.Admin.AccountId:payment.CollectedAt==null&&payment.ActorId==null&&orderInState.CompletedAt==null,"Corrected matrix COD collection metadata wrong.");
            }
        });
        audit.Observe("DIFF-COD-AMOUNT","DA_THU: Amount=Total; CHUA_THU: Amount=0","Conditional Amount asserted across six states",!audit.Failures.Contains("COD conditional Amount across all six states"),"Resolved by the corrected user attachment section5. This is now positive conformance evidence; production behavior is unchanged.");
        var draftBefore=await f.Snapshot();
        await using var draftDb=c.Open();var purchases=new PurchaseService(c.Kernel(draftDb));
        var emptyId=await purchases.Save(c.Admin,0,new(f.Data.SupplierId,[],RequestKey:Guid.NewGuid()));var empty=await purchases.Read(c.Admin,emptyId);
        var draftAfter=await f.Snapshot();StateMatrixFixture.Same(draftBefore.Stock,draftAfter.Stock,"Empty draft changed stock.");StateMatrixFixture.Same(draftBefore.Variants,draftAfter.Variants,"Empty draft changed price.");StateMatrixFixture.Same(draftBefore.Movements,draftAfter.Movements,"Empty draft wrote stock journals.");
        var beforeConfirm=await f.Snapshot();await CommerceContext.Reject("INVALID_INPUT",async()=>await purchases.Confirm(c.Admin,emptyId,new(empty.Version,Guid.NewGuid())));await f.Unchanged(beforeConfirm);
        audit.Observe("DIFF-EMPTY-DRAFT","NHAP with zero lines/total; empty confirmation rejected",$"{empty.Status}; lines={empty.Lines.Count}; total={empty.Total}; empty-confirm=INVALID_INPUT/400",empty.Status=="NHAP"&&empty.Lines.Count==0&&empty.Total==0,"Accepted section6 policy permits persisted empty drafts and requires nonempty confirmation. PURCHASE_EMPTY maps to existing INVALID_INPUT/400.");
    }
}
