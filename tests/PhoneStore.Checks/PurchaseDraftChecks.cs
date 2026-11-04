using System.Net;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class PurchaseDraftChecks
{
    public static async Task Run(Uri address, string connection)
    {
        using var c = new CommerceContext(connection, address, "drafts"); await c.Initialize(); var data = await CommerceData.Create(c);
        await using var db = c.Open(); var service = new PurchaseService(c.Kernel(db));
        var input = new PurchaseInput(data.SupplierId, data.Product.Variants.Select(v => new PurchaseLineInput(v.Id, 2, 100)).ToList(), RequestKey:Guid.NewGuid());
        await Check.Run("A14 draft persistence, duplicate rejection and creation retry leave stock/prices unchanged", async () =>
        {
            var id = await service.Save(c.Admin, 0, input); Check.True(await service.Save(c.Admin, 0, input) == id, "Draft creation retry duplicated a receipt.");
            var row = await service.Read(c.Admin, id); Check.True(row.Total == 400m && row.Lines.Count == 2, "Draft totals/lines incorrect.");
            await CommerceContext.Reject("INVALID_STATE", async () => await service.Save(c.Admin, 0, input with { Lines=[new(data.Product.Variants[0].Id,3,100)] }));
            await CommerceContext.Reject("INVALID_INPUT", async () => await service.Save(c.Admin, 0, input with { RequestKey=Guid.NewGuid(), Lines=[input.Lines![0],input.Lines[0]] }));
            var variantIds=data.Product.Variants.Select(v=>v.Id).ToList();
            Check.True(await db.Inventory.Where(x=>variantIds.Contains(x.VariantId)).AllAsync(x=>x.OnHand==0&&x.Reserved==0), "Draft changed stock.");
        });
        await Check.Run("A14 equal-total line edits advance rowversion; A15 cancellation is draft-only", async () =>
        {
            var id = await service.Save(c.Admin,0,input with {RequestKey=Guid.NewGuid()}); var original = await service.Read(c.Admin,id);
            await service.Save(c.Admin,id,new(data.SupplierId,[new(data.Product.Variants[0].Id,4,100)],original.Version));
            var updated=await service.Read(c.Admin,id); Check.True(updated.Version!=original.Version&&updated.Total==original.Total, "Equal-total draft edits failed to advance header version.");
            await CommerceContext.Reject("CONCURRENT_UPDATE",async()=>await service.Save(c.Admin,id,new(data.SupplierId,[],original.Version)));
            await service.Cancel(c.Admin,id,new(updated.Version,Guid.NewGuid())); var cancelled=await service.Read(c.Admin,id);
            await CommerceContext.Reject("INVALID_STATE",async()=>await service.Save(c.Admin,id,new(data.SupplierId,[],cancelled.Version)));
            using var denied=await c.CustomerClient.Http.GetAsync($"/api/admin/purchases/{id}");
            Check.True(denied.StatusCode==HttpStatusCode.Forbidden,"Purchase was exposed to CUSTOMER.");
            var empty=await service.Save(c.Admin,0,new(data.SupplierId,[],RequestKey:Guid.NewGuid())); Check.True((await service.Read(c.Admin,empty)).Lines.Count==0,"Empty draft could not persist.");
        });
    }
}
