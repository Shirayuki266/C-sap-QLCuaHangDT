using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
namespace PhoneStore.Infrastructure.Commerce;
public sealed class ReportService(BusinessKernel kernel)
{
    public Task<StoreReport> Read(Actor actor,string kind,string? from=null,string? to=null) => kernel.Read<StoreReport>(async () =>
    {
        await kernel.Require(actor,"ADMIN");
        if(kind=="stock")
        {
            var stock=await(from i in kernel.Db.Inventory.AsNoTracking() join v in kernel.Db.Variants on i.VariantId equals v.Id join p in kernel.Db.Products on v.ProductId equals p.Id orderby p.Name,v.Id select new StockReportLine(v.Id,p.Name,v.Sku,i.OnHand,i.Reserved,i.OnHand-i.Reserved)).ToListAsync();
            return new(kind,"UTC+07:00",null,null,0,stock.Sum(x=>(long)x.OnHand),0,[],[],stock);
        }
        if(kind is not ("revenue" or "bestsellers"))throw Rules.Error("INVALID_INPUT","Loại báo cáo không hợp lệ.");
        var dates=DateWindow.Parse(from,to);
        var orders=from o in kernel.Db.Orders.AsNoTracking() join p in kernel.Db.Payments.AsNoTracking() on o.Id equals p.OrderId
                   where o.Status=="HOAN_TAT"&&p.Method=="COD"&&p.Status=="DA_THU"&&p.Amount==o.Total&&o.CompletedAt!=null&&(!dates.Start.HasValue||o.CompletedAt>=dates.Start)&&(!dates.End.HasValue||o.CompletedAt<dates.End) select o;
        // Read the header once for revenue; joining lines here would multiply order totals.
        var headers=await orders.Select(x=>new{x.Id,x.Total,x.CompletedAt}).ToListAsync();
        var daily=headers.GroupBy(x=>DateOnly.FromDateTime(x.CompletedAt!.Value.AddHours(7))).OrderBy(x=>x.Key).Select(g=>new DailyRevenue(g.Key,g.Count(),g.Sum(x=>x.Total))).ToList();
        var best=await(from l in kernel.Db.OrderLines.AsNoTracking() join o in orders on l.OrderId equals o.Id group l by l.VariantId into g select new{VariantId=g.Key,Quantity=g.Sum(x=>(long)x.Quantity),Revenue=g.Sum(x=>x.Quantity*x.Price)}).ToListAsync();
        var ids=best.Select(x=>x.VariantId).ToArray();var names=await(from v in kernel.Db.Variants.AsNoTracking() join p in kernel.Db.Products on v.ProductId equals p.Id where ids.Contains(v.Id) select new{v.Id,p.Name,v.Sku}).ToDictionaryAsync(x=>x.Id);
        var rows=best.OrderByDescending(x=>x.Quantity).ThenBy(x=>x.VariantId).Select(x=>new Bestseller(x.VariantId,names[x.VariantId].Name,names[x.VariantId].Sku,x.Quantity,x.Revenue)).ToList();
        return new(kind,"UTC+07:00",from,to,headers.Sum(x=>x.Total),rows.Sum(x=>x.Quantity),headers.Count,daily,rows,[]);
    });
}
