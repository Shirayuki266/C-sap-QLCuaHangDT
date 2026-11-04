using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
namespace PhoneStore.Infrastructure.Commerce;
public sealed class PublicBestsellerService(BusinessKernel kernel)
{
    public Task<PublicBestsellers> Read()=>kernel.Read<PublicBestsellers>(async()=>
    {
        var local=DateOnly.FromDateTime(kernel.Now.AddHours(7));var from=new DateOnly(local.Year,local.Month,1);var to=from.AddMonths(1).AddDays(-1);
        var window=DateWindow.Parse(from.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),to.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));
        var db=kernel.Db;
        var sales=from line in db.OrderLines.AsNoTracking()
                  join order in db.Orders on line.OrderId equals order.Id
                  join payment in db.Payments on order.Id equals payment.OrderId
                  join variant in db.Variants on line.VariantId equals variant.Id
                  where order.Status=="HOAN_TAT"&&payment.Method=="COD"&&payment.Status=="DA_THU"&&payment.Amount==order.Total&&order.CompletedAt>=window.Start&&order.CompletedAt<window.End
                  group line by variant.ProductId into product
                  select new{ProductId=product.Key,Quantity=product.Sum(x=>(long)x.Quantity),Revenue=product.Sum(x=>x.Quantity*x.Price)};
        var rows=await(from sale in sales join product in db.Products.AsNoTracking() on sale.ProductId equals product.Id
                       where product.Active&&db.Brands.Any(x=>x.Id==product.BrandId&&x.Active)&&db.Categories.Any(x=>x.Id==product.CategoryId&&x.Active)&&db.Variants.Any(x=>x.ProductId==product.Id&&x.Active)
                       orderby sale.Quantity descending,sale.Revenue descending,product.Id
                       select new PublicBestseller(product.Id,product.Name,product.ImageUrl,db.Variants.Where(v=>v.ProductId==product.Id&&v.Active).Min(v=>v.Price),sale.Quantity)).Take(8).ToListAsync();
        return new(from,to,"UTC+07:00",rows);
    });
}
