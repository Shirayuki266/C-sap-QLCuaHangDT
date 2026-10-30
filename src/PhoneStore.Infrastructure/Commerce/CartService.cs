using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Infrastructure.Commerce;
public sealed class CartService(BusinessKernel kernel)
{
    public Task<CartView> Preview(List<CartInput> inputs) => kernel.Read<CartView>(async () =>
    {
        Validate(inputs);var ids=inputs.Select(x=>x.VariantId).Distinct().ToList();
        var rows=await (from v in kernel.Db.Variants.AsNoTracking() join p in kernel.Db.Products on v.ProductId equals p.Id
                        join b in kernel.Db.Brands on p.BrandId equals b.Id join c in kernel.Db.Categories on p.CategoryId equals c.Id
                        join s in kernel.Db.Inventory on v.Id equals s.VariantId where ids.Contains(v.Id)
                        select new{v,p.Name,Trading=v.Active&&p.Active&&b.Active&&c.Active,Available=s.OnHand-s.Reserved}).ToListAsync();
        var result=new List<CartLineView>();
        foreach(var input in inputs.OrderBy(x=>x.VariantId))
        {
            var row=rows.SingleOrDefault(x=>x.v.Id==input.VariantId);
            if(row is null){result.Add(new(input.VariantId,null,null,null,null,input.Quantity,null,0,false,"NOT_FOUND"));continue;}
            var issue=!row.Trading?"DISCONTINUED":row.Available<input.Quantity?"OUT_OF_STOCK":null;
            result.Add(new(input.VariantId,row.Name,row.v.Sku,row.v.Color,row.v.StorageGb,input.Quantity,row.v.Price,row.Available,row.Trading,issue));
        }
        return new(result,result.Sum(x=>x.Quantity*(x.Price??0)),result.Any(x=>x.Issue is not null));
    });
    public Task<CartView> Read(Actor actor) => kernel.Read<CartView>(async () =>
    {
        await kernel.Require(actor,"CUSTOMER");
        var lines=await kernel.Db.Cart.AsNoTracking().Where(x=>x.CustomerId==actor.AccountId).Select(x=>new CartInput(x.VariantId,x.Quantity)).ToListAsync();
        return await Preview(lines);
    });
    private static void Validate(List<CartInput>? lines)
    {
        if(lines is null||lines.Count>100||lines.Any(x=>x is null||x.VariantId<=0||x.Quantity<=0))throw Rules.Error("INVALID_INPUT","Giỏ cần mã biến thể/số lượng hợp lệ, tối đa 100 dòng.");
    }
    private async Task SetOne(long customer,CartInput input,bool add)
    {
        if(input.VariantId<=0)throw Rules.Error("INVALID_INPUT","Mã biến thể không hợp lệ.");
        var line=await kernel.Db.Cart.SingleOrDefaultAsync(x=>x.CustomerId==customer&&x.VariantId==input.VariantId);
        if(!add&&input.Quantity==0){if(line is not null)kernel.Db.Cart.Remove(line);return;}
        Rules.Quantity(input.Quantity);var quantity=add?checked((line?.Quantity??0)+input.Quantity):input.Quantity;Rules.Quantity(quantity);
        var preview=await Preview([new(input.VariantId,quantity)]);var current=preview.Lines.Single();
        if(!current.Sellable)throw Rules.Error("INVALID_INPUT","Biến thể không tồn tại hoặc đã ngừng kinh doanh.");
        if(current.Available<quantity)throw Rules.Error("OUT_OF_STOCK","Số lượng vượt hàng có thể bán.");
        if(line is null)
        {
            if(await kernel.Db.Cart.CountAsync(x=>x.CustomerId==customer)>=100)throw Rules.Error("INVALID_INPUT","Giỏ có tối đa 100 biến thể.");
            kernel.Db.Cart.Add(new CartLine{CustomerId=customer,VariantId=input.VariantId,Quantity=quantity,UpdatedAt=kernel.Now});
        }
        else {line.Quantity=quantity;line.UpdatedAt=kernel.Now;}
    }
    public Task<bool> Set(Actor actor,CartInput input,bool add=false)=>kernel.Write(async()=>
    {await kernel.Require(actor,"CUSTOMER");await SetOne(actor.AccountId,input,add);return true;});
    public Task<bool> Merge(Actor actor,List<CartInput> inputs)=>kernel.Write(async()=>
    {
        await kernel.Require(actor,"CUSTOMER");Validate(inputs);
        var existing=await kernel.Db.Cart.Where(x=>x.CustomerId==actor.AccountId).Select(x=>x.VariantId).ToListAsync();
        if(existing.Union(inputs.Select(x=>x.VariantId)).Count()>100)throw Rules.Error("INVALID_INPUT","Giỏ hợp nhất vượt 100 biến thể.");
        foreach(var group in inputs.GroupBy(x=>x.VariantId).OrderBy(x=>x.Key))await SetOne(actor.AccountId,new(group.Key,checked((int)group.Sum(x=>(long)x.Quantity))),true);
        return true;
    });
}
