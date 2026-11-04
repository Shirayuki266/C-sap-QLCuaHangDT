using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Checks;
internal static class PublicBestsellerChecks
{
    private sealed class Clock(DateTime at):TimeProvider
    {
        public DateTime At{get;set;}=at;
        public override DateTimeOffset GetUtcNow()=>new(DateTime.SpecifyKind(At,DateTimeKind.Utc));
    }
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"public-bestsellers");await c.Initialize();var clock=new Clock(new(2022,2,15,0,0,0));
        var fixtures=new List<CommerceData>();
        for(var i=0;i<12;i++)fixtures.Add(await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),200));
        var start=new DateTime(2022,1,31,17,0,0,DateTimeKind.Utc);var end=new DateTime(2022,2,28,17,0,0,DateTimeKind.Utc);
        async Task<long> Order(CommerceData data,int quantity,DateTime at,int variant=0,bool unpaid=false,bool pending=false)
        {
            clock.At=at;await using var db=c.Open();var k=new BusinessKernel(db,clock);var cart=new CartService(k);foreach(var line in (await cart.Read(c.Customer)).Lines)await cart.Set(c.Customer,new(line.VariantId,0));
            await cart.Set(c.Customer,new(data.Product.Variants[variant].Id,quantity));var current=await cart.Read(c.Customer);
            var order=await new CheckoutService(k).Place(c.Customer,new(Guid.NewGuid(),"Bestseller fixture","0909999999","Fixture address",current.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price!.Value)).ToList()));
            if(!pending)
            {
                var flow=new OrderWorkflow(k);var queries=new OrderQueryService(k);await flow.Confirm(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));order=await queries.Read(c.Admin,order.Id,true);
                await flow.Dispatch(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));order=await queries.Read(c.Admin,order.Id,true);await flow.Complete(c.Admin,order.Id,new(order.Version,Guid.NewGuid()));
                if(unpaid)await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.THANH_TOAN SET Status='CHUA_THU', Amount=0, CollectedAt=NULL, ActorId=NULL WHERE OrderId={order.Id}");
            }
            return order.Id;
        }
        await using(var db=c.Open())
        {
            var k=c.Kernel(db);var purchase=new PurchaseService(k);var d=fixtures[2];var id=await purchase.Save(c.Admin,0,new(d.SupplierId,[new(d.Product.Variants[0].Id,1,1500m)],RequestKey:Guid.NewGuid()));var draft=await purchase.Read(c.Admin,id);await purchase.Confirm(c.Admin,id,new(draft.Version,Guid.NewGuid()));
            fixtures[2]=d with{Product=await new ProductService(k).Read(c.Admin,d.Product.Id)};
        }
        for(var i=0;i<12;i++)
        {
            if(i==1){await Order(fixtures[i],3,start,0);await Order(fixtures[i],2,start.AddDays(1),1);}
            else await Order(fixtures[i],i<3?5:i<7?13-i:1,start.AddDays(2));
        }
        await Order(fixtures[0],99,start.AddTicks(-1));await Order(fixtures[11],50,end);await Order(fixtures[10],50,start.AddDays(3),unpaid:true);await Order(fixtures[9],30,start.AddDays(4),pending:true);
        await using(var db=c.Open())
        {
            var k=c.Kernel(db);var products=new ProductService(k);var p=await products.Read(c.Admin,fixtures[1].Product.Id);var input=ProductChecks.Input(p);
            await products.Save(c.Admin,p.Id,input with{Name="Current public bestseller label",ImageUrl="/images/demo-phone.svg",Variants=input.Variants!.Select((x,i)=>i==0?x with{ProfitPercent=100m}:x).ToList()});
            p=await products.Read(c.Admin,fixtures[3].Product.Id);await products.Save(c.Admin,p.Id,ProductChecks.Input(p) with{Active=false});
            var brand=await db.Brands.AsNoTracking().SingleAsync(x=>x.Id==fixtures[4].Product.BrandId);await new LookupService(k).Save(c.Admin,"brands",brand.Id,new(brand.Name,false));
            var category=await db.Categories.AsNoTracking().SingleAsync(x=>x.Id==fixtures[5].Product.CategoryId);await new LookupService(k).Save(c.Admin,"categories",category.Id,new(category.Name,false));
        }
        clock.At=new(2022,2,15,0,0,0);
        await Check.Run("DEC.07 FE.01 public monthly ranking aggregates products and snapshot quantity/revenue/ID ties",async()=>
        {
            await using var db=c.Open();var result=await new PublicBestsellerService(new(db,clock)).Read();
            var expected=new[]{6,2,1,0,7,8,9,10}.Select(i=>fixtures[i].Product.Id).ToArray();
            Check.True(result.From==new DateOnly(2022,2,1)&&result.To==new DateOnly(2022,2,28)&&result.TimeZone=="UTC+07:00"&&result.Items.Select(x=>x.ProductId).SequenceEqual(expected),"Public top8 grouping/ranking/eligibility differs.");
            var current=result.Items.Single(x=>x.ProductId==fixtures[1].Product.Id);Check.True(current.QuantitySold==5&&current.Name=="Current public bestseller label"&&current.ImageUrl=="/images/demo-phone.svg"&&current.MinPrice==2000m,"Public labels/min price are not current or historical ranking was repriced.");
            Check.True(result.Items.All(x=>x.QuantitySold<50),"Outside-period, pending or unpaid sale was counted.");
        });
        await Check.Run("DEC.07 UTC+7 month rollover includes exact start and excludes next-month boundary without fallback",async()=>
        {
            await using var db=c.Open();var service=new PublicBestsellerService(new(db,clock));clock.At=start;
            Check.True((await service.Read()).From==new DateOnly(2022,2,1),"UTC+7 month start drifted.");clock.At=start.AddTicks(-1);var january=await service.Read();
            Check.True(january.Items.Single().ProductId==fixtures[0].Product.Id&&january.Items.Single().QuantitySold==99,"Pre-boundary sale was assigned to February.");
            clock.At=end;var march=await service.Read();Check.True(march.From==new DateOnly(2022,3,1)&&march.Items.Single().ProductId==fixtures[11].Product.Id&&march.Items.Single().QuantitySold==50,"Exclusive next-month boundary failed.");
            clock.At=new(2022,12,15);var empty=await service.Read();Check.True(empty.Items.Count==0&&empty.From==new DateOnly(2022,12,1),"Empty period silently fell back to another month.");clock.At=new(2022,2,15);
        });
        await Check.Run("DEC.07 public bestseller reads leave inventory payment and history unchanged; anonymous HTTP is bounded and redacted",async()=>
        {
            await using var db=c.Open();var stocks=await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).Select(x=>new{x.VariantId,x.OnHand,x.Reserved}).ToListAsync();var history=await db.OrderHistory.CountAsync();var movements=await db.Movements.CountAsync();var paid=await db.Payments.SumAsync(x=>x.Amount);
            var service=new PublicBestsellerService(new(db,clock));await service.Read();await service.Read();
            using var guest=new HttpClient(new HttpClientHandler{UseProxy=false}){BaseAddress=address};using var response=await guest.GetAsync("/api/catalog/bestsellers");var raw=await response.Content.ReadAsStringAsync();using var json=JsonDocument.Parse(raw);
            Check.True(response.StatusCode==HttpStatusCode.OK&&json.RootElement.GetProperty("items").GetArrayLength()<=8&&!raw.Contains("\"cost\"",StringComparison.OrdinalIgnoreCase)&&!raw.Contains("\"revenue\"",StringComparison.OrdinalIgnoreCase)&&!raw.Contains("\"reserved\"",StringComparison.OrdinalIgnoreCase),"Public endpoint auth/bound/redaction failed.");
            Check.True((await db.Inventory.AsNoTracking().OrderBy(x=>x.VariantId).Select(x=>new{x.VariantId,x.OnHand,x.Reserved}).ToListAsync()).SequenceEqual(stocks)&&await db.OrderHistory.CountAsync()==history&&await db.Movements.CountAsync()==movements&&await db.Payments.SumAsync(x=>x.Amount)==paid,"Public query mutated business data.");
        });
    }
}
