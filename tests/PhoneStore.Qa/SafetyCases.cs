using Microsoft.Data.SqlClient;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Core.Contracts;
using Microsoft.EntityFrameworkCore;
internal sealed partial class QaSuite
{
    async Task SafetyAndRiskCases(){
        await Check("QA-TARGET-GUARD","Đích sai/mất cấu hình bị từ chối trước SQL","Chỉ server và DB QA chính xác được chấp nhận",()=>{
            foreach(var values in new (string? connection,string? server,string? db)[]{(null,null,null),("Server=localhost;Database=PhoneStore","localhost","PhoneStore"),("Server=other;Database=PhoneStore_QA_Test","localhost","PhoneStore_QA_Test"),("Server=localhost;Database=PhoneStore","localhost","PhoneStore_QA_Test")}){
                bool rejected=false;try{QaTarget.Validate(values.connection,values.server,values.db);}catch(InvalidOperationException){rejected=true;}Assert(rejected);
            }QaTarget.Validate(new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog="PhoneStore_QA_Test"}.ConnectionString,"localhost","PhoneStore_QA_Test");return Task.CompletedTask;
        },kind:"Safety regression; no SQL writes");
        await Check("QA-IDENTITY","Backend QA chứng minh đích trước bộ thử cũ","Identity đúng môi trường/server/DB và guard HTTP đạt",async()=>{await QaTarget.VerifyHttp(url);});
        await Check("QA-CART-NULL-BOUND","Giỏ null/overflow trực tiếp không đổi dữ liệu SQL","400, số lượng trước/sau không đổi, không giữ kho",async()=>{
            var v=(await Fixture(5))[0];await Clear(a);await Status(await a.Send("/api/cart",new CartInput(v.Id,2)),200);
            foreach(var quantity in new[]{"null","2147483648","1.25","-1"})await Status(await a.Raw("/api/cart",$"{{\"variantId\":{v.Id},\"quantity\":{quantity}}}"),400);
            await using var db=Open();Assert((await db.Cart.SingleAsync(x=>x.CustomerId==aId&&x.VariantId==v.Id)).Quantity==2);var stock=await db.Stock.SingleAsync(x=>x.VariantId==v.Id);Assert(stock.Reserved==0&&stock.OnHand==5);
        });
        await Check("QA-FILTER-CROSS-FLOW","Giá đúng biến thể lọc từ danh sách tới checkout","Giá API/detail/cart/order/snapshot SQL giống nhau",async()=>{
            var variants=await Fixture(5,2,123456.75m);var product=await ProductFor(variants[0].Id);var selected=variants[1];
            var list=await anon.Read<Page<ProductDto>>($"/api/catalog?Query={product.Code}&StorageGb={selected.StorageGb}&MinPrice={selected.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)}&MaxPrice={selected.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            Assert(list.Items.Count==1&&list.Items[0].Variants.Count==1&&list.Items[0].Variants[0].Id==selected.Id);
            var detail=await anon.Read<ProductDto>($"/api/catalog/{product.Id}");Assert(detail.Variants.Single(x=>x.Color==selected.Color&&x.StorageGb==selected.StorageGb).Price==selected.Price);
            var request=await Cart(a,[selected]);var order=await Place(a,request);Assert(order.Total==selected.Price&&order.Lines.Single().Price==selected.Price);
            await using var db=Open();Assert((await db.Orders.Include(x=>x.Lines).SingleAsync(x=>x.Id==order.Id)).Lines.Single().Price==selected.Price);
        });
    }
}
