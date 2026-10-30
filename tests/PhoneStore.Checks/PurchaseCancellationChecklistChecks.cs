using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Checks;

internal static class PurchaseCancellationChecklistChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"purchase-cancel-checklist");await c.Initialize();
        var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),10);var f=new StateMatrixFixture(c,data);
        List<PurchaseLineInput> Lines(CommerceData d)=>d.Product.Variants.Select((v,i)=>new PurchaseLineInput(v.Id,i+2,1500m+i*1000m)).ToList();
        async Task<PurchaseView> Read(long id)
        {
            await using var db=c.Open();return await new PurchaseService(c.Kernel(db)).Read(c.Admin,id);
        }
        async Task<PurchaseView> Draft(CommerceData d,bool empty=false)
        {
            await using var db=c.Open();var service=new PurchaseService(c.Kernel(db));
            var id=await service.Save(c.Admin,0,new(d.SupplierId,empty?[]:Lines(d),RequestKey:Guid.NewGuid()));return await service.Read(c.Admin,id);
        }
        async Task Rejected(PurchaseView row,string action,string code,HttpStatusCode status)
        {
            var before=await f.Snapshot();object body=action=="edit"?new PurchaseInput(row.SupplierId,row.Lines.Select(x=>new PurchaseLineInput(x.VariantId,x.Quantity,x.Cost)).ToList(),row.Version):new VersionedCommand(row.Version,Guid.NewGuid());
            var path=action=="edit"?$"/api/admin/purchases/{row.Id}":$"/api/admin/purchases/{row.Id}/{action}";
            using var response=await c.AdminClient.Send(path,body,action=="edit"?HttpMethod.Put:HttpMethod.Post);
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Check.True(response.StatusCode==status&&json.RootElement.GetProperty("code").GetString()==code,"Purchase rejection returned the wrong mapped error/status.");
            await f.Unchanged(before);
        }
        async Task Cancel(PurchaseView row)
        {
            var before=await f.Snapshot();using var response=await c.AdminClient.Send($"/api/admin/purchases/{row.Id}/cancel",new VersionedCommand(row.Version,Guid.NewGuid()));
            Check.True(response.StatusCode==HttpStatusCode.NoContent,"Valid draft cancellation failed.");var after=await f.Snapshot();
            var old=before.Purchases.Single(x=>x.Id==row.Id);var current=after.Purchases.Single(x=>x.Id==row.Id);
            Check.True(old.Status=="NHAP"&&current.Status=="DA_HUY"&&!old.Version.SequenceEqual(current.Version),"Cancellation did not advance draft state/version.");
            // Only these two header fields are allowed to change; every other business row/field must be identical.
            old.Status=current.Status;old.Version=current.Version;StateMatrixFixture.Same(before,after,"Draft cancellation changed stock, prices, receipt lines, COD, order history or unrelated data.");
        }
        await Check.Run("A15.04 cancelling populated and empty drafts changes only receipt status/version",async()=>
        {
            foreach(var empty in new[]{false,true})
            {
                var row=await Draft(data,empty);Check.True(row.Lines.Count==(empty?0:2)&&(!empty||row.Total==0),"Draft fixture missing its intended lines/total.");await Cancel(row);
            }
        });
        await Check.Run("A15.04 empty or inactive-supplier confirmation rejects atomically while draft cancellation remains allowed",async()=>
        {
            var empty=await Draft(data,true);await Rejected(empty,"confirm","INVALID_INPUT",HttpStatusCode.BadRequest);await Cancel(await Read(empty.Id));
            var separate=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),5);var inactive=await Draft(separate);
            await using(var db=c.Open())
            {
                var supplier=await db.Suppliers.AsNoTracking().SingleAsync(x=>x.Id==separate.SupplierId);
                await new SupplierService(c.Kernel(db)).Save(c.Admin,supplier.Id,new(supplier.Code,supplier.Name,supplier.Phone,supplier.Address,false));
            }
            await Rejected(inactive,"confirm","INVALID_INPUT",HttpStatusCode.BadRequest);await Cancel(await Read(inactive.Id));
        });
        await Check.Run("A15.04 stale edit cancellation and confirmation return CONCURRENT_UPDATE without writes",async()=>
        {
            var original=await Draft(data);
            await using(var db=c.Open())await new PurchaseService(c.Kernel(db)).Save(c.Admin,original.Id,new(original.SupplierId,Lines(data).Select(x=>x with{Quantity=x.Quantity+1}).ToList(),original.Version));
            Check.True((await Read(original.Id)).Version!=original.Version,"Stale receipt fixture failed to advance version.");
            foreach(var action in new[]{"edit","cancel","confirm"})await Rejected(original,action,"CONCURRENT_UPDATE",HttpStatusCode.Conflict);
        });
        await Check.Run("A15.04 confirmed and cancelled receipts reject edit cancel and confirm without second effects",async()=>
        {
            foreach(var state in new[]{"DA_XAC_NHAN","DA_HUY"})
            {
                var draft=await Draft(data);
                if(state=="DA_HUY")await Cancel(draft);
                else
                {
                    using var confirmed=await c.AdminClient.Send($"/api/admin/purchases/{draft.Id}/confirm",new VersionedCommand(draft.Version,Guid.NewGuid()));
                    Check.True(confirmed.StatusCode==HttpStatusCode.NoContent,"Confirmed receipt fixture failed.");
                }
                var row=await Read(draft.Id);Check.True(row.Status==state,"Final receipt fixture has wrong state.");
                foreach(var action in new[]{"edit","cancel","confirm"})await Rejected(row,action,"INVALID_STATE",HttpStatusCode.Conflict);
            }
        });
    }
}
