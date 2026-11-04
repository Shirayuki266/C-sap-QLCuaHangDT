using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class PurchaseConfirmChecks
{
    public static async Task<CommerceData> Receive(CommerceContext c, CommerceData data, int quantity = 10)
    {
        await using var db = c.Open(); var k = c.Kernel(db); var purchases = new PurchaseService(k);
        var id = await purchases.Save(c.Admin, 0, new(data.SupplierId, data.Product.Variants.Select(v => new PurchaseLineInput(v.Id, quantity, v.Cost)).ToList(), RequestKey:Guid.NewGuid()));
        var row = await purchases.Read(c.Admin,id); await purchases.Confirm(c.Admin,id,new(row.Version,Guid.NewGuid()));
        return data with { Product=await new ProductService(k).Read(c.Admin,data.Product.Id) };
    }
    public static async Task Run(Uri address, string connection)
    {
        using var c = new CommerceContext(connection,address,"confirm"); await c.Initialize(); var data=await CommerceData.Create(c);
        await using var db=c.Open(); var k=c.Kernel(db); var service=new PurchaseService(k);
        async Task<long> Draft(decimal cost) => await service.Save(c.Admin,0,new(data.SupplierId,data.Product.Variants.Select(v=>new PurchaseLineInput(v.Id,2,cost)).ToList(),RequestKey:Guid.NewGuid()));
        await Check.Run("A15 confirmation is once-only and latest confirmation controls cost/price",async()=>
        {
            var older=await Draft(100); var newer=await Draft(200); var row=await service.Read(c.Admin,newer);
            await service.Confirm(c.Admin,newer,new(row.Version,Guid.NewGuid())); row=await service.Read(c.Admin,older);
            await service.Confirm(c.Admin,older,new(row.Version,Guid.NewGuid()));
            var p=await new ProductService(k).Read(c.Admin,data.Product.Id);
            Check.True(p.Variants.All(v=>v.Cost==100m&&v.Price==120m&&v.OnHand==4&&v.Reserved==0),"Receipt price/stock/latest confirmation incorrect.");
            row=await service.Read(c.Admin,older); await CommerceContext.Reject("INVALID_STATE",async()=>await service.Confirm(c.Admin,older,new(row.Version,Guid.NewGuid())));
        });
        await Check.Run("A15 multi-line repricing failure rolls back earlier stock updates and all journals",async()=>
        {
            var p=await new ProductService(k).Read(c.Admin,data.Product.Id);
            await new ProductService(k).Save(c.Admin,p.Id,ProductChecks.Input(p) with {Variants=ProductChecks.Input(p).Variants!.Select((v,i)=>v with {ProfitPercent=i==1?10000:0}).ToList()});
            var id=await service.Save(c.Admin,0,new(data.SupplierId,[new(p.Variants[0].Id,1,1),new(p.Variants[1].Id,1,Rules.MaxMoney-1)],RequestKey:Guid.NewGuid()));
            var row=await service.Read(c.Admin,id); var before=await new ProductService(k).Read(c.Admin,p.Id);
            await CommerceContext.Reject("INVALID_INPUT",async()=>await service.Confirm(c.Admin,id,new(row.Version,Guid.NewGuid())));
            var after=await new ProductService(k).Read(c.Admin,p.Id);
            Check.True(after.Variants.Select(v=>(v.OnHand,v.Cost,v.Price)).SequenceEqual(before.Variants.Select(v=>(v.OnHand,v.Cost,v.Price)))&&
                !await db.Movements.AnyAsync(x=>x.PurchaseId==id)&&(await service.Read(c.Admin,id)).Status=="NHAP","Failed receipt left partial effects.");
        });
        await Check.Run("A15 independent concurrent confirmations produce one inventory effect",async()=>
        {
            var fresh=await CommerceData.Create(c); var id=await service.Save(c.Admin,0,new(fresh.SupplierId,fresh.Product.Variants.Select(v=>new PurchaseLineInput(v.Id,3,v.Cost)).ToList(),RequestKey:Guid.NewGuid())); var row=await service.Read(c.Admin,id);
            async Task<bool> Attempt(){await using var d=c.Open();try{await new PurchaseService(c.Kernel(d)).Confirm(c.Admin,id,new(row.Version,Guid.NewGuid()));return true;}catch(BusinessException e)when(e.Code is "CONCURRENT_UPDATE" or "INVALID_STATE"){return false;}}
            var results=await Task.WhenAll(Attempt(),Attempt());
            Check.True(results.Count(x=>x)==1&&(await new ProductService(k).Read(c.Admin,fresh.Product.Id)).Variants.All(v=>v.OnHand==3),"Receipt race duplicated stock.");
            var empty=await service.Save(c.Admin,0,new(fresh.SupplierId,[],RequestKey:Guid.NewGuid()));var e=await service.Read(c.Admin,empty);
            await CommerceContext.Reject("INVALID_INPUT",async()=>await service.Confirm(c.Admin,empty,new(e.Version,Guid.NewGuid())));
        });
    }
}
