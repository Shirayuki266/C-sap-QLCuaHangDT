using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;

namespace PhoneStore.Checks;

internal sealed partial class Prompt2AFixture
{
    private sealed record SupplementObservation(string Id,string Status,string Expectation,object Request,object Response,object Before,object After,string Note);
    public async Task Supplement()
    {
        var observations=new List<SupplementObservation>();
        async Task<object> ProductSql(CommerceData d)
        {
            await using var db=C.Open();var p=await db.Products.AsNoTracking().SingleAsync(x=>x.Id==d.Product.Id);
            var ids=d.Product.Variants.Select(x=>x.Id).ToArray();
            var stock=await db.Inventory.AsNoTracking().Where(x=>ids.Contains(x.VariantId)).OrderBy(x=>x.VariantId).Select(x=>new{x.VariantId,x.OnHand,x.Reserved}).ToListAsync();
            return new{p.Id,p.Active,p.ImageUrl,Stock=stock,Gallery=await db.ProductImages.AsNoTracking().Where(x=>x.ProductId==p.Id).OrderBy(x=>x.Id).Select(x=>x.Url).ToListAsync()};
        }
        await Check.Run("A05.05 supplement required gallery URL rejects null empty whitespace and oversized values",async()=>
        {
            var d=await Data();await Save(d,x=>x with{ImageUrl="/images/valid-main.png"});
            foreach(var url in new string?[]{null,""," \t\r\n ","//example.test/a.png","https://example.test/"+new string('x',1001)})
            {
                await using var db=C.Open();var p=await new ProductService(C.Kernel(db)).Read(C.Admin,d.Product.Id);var before=await Snapshot();
                using var r=await C.AdminClient.Send($"/api/admin/products/{p.Id}",ProductChecks.Input(p) with{Images=[new(url)]},HttpMethod.Put);await Error(r,HttpStatusCode.BadRequest,"INVALID_INPUT");await Unchanged(before);
            }
            const string prefix="https://example.test/";var maximum=prefix+new string('a',1000-prefix.Length);
            await Save(d,x=>x with{ImageUrl=maximum,Images=[new("/images/valid-gallery.png")]});
            await using var current=C.Open();var saved=await new ProductService(C.Kernel(current)).Read(C.Admin,d.Product.Id);
            Check.True(saved.ImageUrl==maximum&&saved.Images.Single().Url=="/images/valid-gallery.png","Valid maximum-length primary/gallery URL was not stored.");
        });
        await Check.Run("A05.05 supplement records optional primary image separately from mandatory gallery entries",async()=>
        {
            var d=await Data();await Save(d,x=>x with{ImageUrl="/images/before-empty.png",Images=[new("/images/gallery-stays.png")]});
            await using var db=C.Open();var p=await new ProductService(C.Kernel(db)).Read(C.Admin,d.Product.Id);var before=await ProductSql(d);var stockBefore=(await Core.Snapshot()).Stock;
            using var r=await C.AdminClient.Send($"/api/admin/products/{p.Id}",ProductChecks.Input(p) with{ImageUrl=""},HttpMethod.Put);
            var after=await ProductSql(d);var saved=await new ProductService(C.Kernel(db)).Read(C.Admin,d.Product.Id);
            Check.True(r.StatusCode==HttpStatusCode.NoContent&&saved.ImageUrl==""&&saved.Images.Single().Url=="/images/gallery-stays.png","Primary-image probe failed outside the allowed/rejected outcomes.");
            StateMatrixFixture.Same(stockBefore,(await Core.Snapshot()).Stock,"Optional primary-image edit changed inventory.");
            observations.Add(new("A05.05-primary-empty","PASS","User clarified: only Images[].Url must reject blanks; primary ImageUrl stays optional",
                new{method="PUT",path=$"/api/admin/products/{p.Id}",imageUrl="",otherFields="Valid retained product/variant versions"},new{status=(int)r.StatusCode,body="NoContent"},before,after,
                "Images[].Url blanks reject400; primary ImageUrl blank succeeds204, matching the explicit user clarification. No backend change."));
        });
        await Check.Run("K09.03 supplement positive physical-minus-reserved availability is exact and redacted",async()=>
        {
            var d=await Data(7);var input=await CheckoutChecks.Cart(C,d,2);await using(var db=C.Open())await new CheckoutService(C.Kernel(db)).Place(C.Customer,input);
            var before=await Snapshot();var rows=(await Core.Snapshot()).Stock.Where(x=>d.Product.Variants.Any(v=>v.Id==x.VariantId)).ToList();
            Check.True(rows.All(x=>x.OnHand==7&&x.Reserved==2),"Positive availability fixture did not reserve exact quantities.");
            using var r=await C.CustomerClient.Http.GetAsync($"/api/catalog/{d.Product.Id}");var raw=await r.Content.ReadAsStringAsync();var detail=await r.Content.ReadFromJsonAsync<PublicProduct>();
            Check.True(r.StatusCode==HttpStatusCode.OK&&detail!.Variants.All(x=>x.Available==5)&&!raw.Contains("\"onHand\"",StringComparison.OrdinalIgnoreCase)&&!raw.Contains("\"reserved\"",StringComparison.OrdinalIgnoreCase),"Public positive availability or internal-balance redaction wrong.");await Unchanged(before);
        });
        await Check.Run("K09.02 supplement accepts API isolation and transfers selected-pair validation to WEB.K09.02",async()=>
        {
            var d=await Data();var other=await Data();var before=await Snapshot();var sqlBefore=await ProductSql(d);var foreign=other.Product.Variants[0].Id;
            var path=$"/api/catalog/{d.Product.Id}?variantId={foreign}";using var r=await C.CustomerClient.Http.GetAsync(path);
            if(r.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound){await Unchanged(before);return;}
            var detail=await r.Content.ReadFromJsonAsync<PublicProduct>();
            Check.True(r.StatusCode==HttpStatusCode.OK&&detail!.Id==d.Product.Id&&detail.Variants.All(x=>d.Product.Variants.Any(v=>v.Id==x.Id))&&!detail.Variants.Any(x=>x.Id==foreign),"Foreign selection leaked or injected a variant.");await Unchanged(before);
            observations.Add(new("K09.02-selected-variant","PASS","API owns its returned variants; selected product-variant validation transfers to WEB.K09.02 as an accepted difference",
                new{method="GET",path,requestedProduct=d.Product.Id,requestedVariant=foreign},new{status=(int)r.StatusCode,productId=detail!.Id,variantIds=detail.Variants.Select(x=>x.Id).ToArray(),selectedVariantField=false,foreignVariantExposed=false},sqlBefore,await ProductSql(d),
                "The query is ignored, returning200 with only owned variants and no DB writes. API behavior passes; kiểm tra cặp sản phẩm–biến thể chuyển sang WEB.K09.02, khác biệt được chấp nhận. This does not claim a native API pair-rejection guard. No production edit was made."));
        });
        var output=Environment.GetEnvironmentVariable("PHONESTORE_PROMPT2A_SUPPLEMENT_RESULTS");
        if(!string.IsNullOrWhiteSpace(output))await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{Observations=observations},new JsonSerializerOptions{WriteIndented=true}));
        foreach(var item in observations)Console.WriteLine($"SUPPLEMENT {item.Status} {item.Id}: {item.Note}");
        if(Environment.GetEnvironmentVariable("PHONESTORE_PROMPT2A_SUPPLEMENT_STRICT")=="1")Check.True(observations.All(x=>x.Status=="PASS"),"Strict backend acceptance has unresolved observations; see sanitized observations.");
    }
}
