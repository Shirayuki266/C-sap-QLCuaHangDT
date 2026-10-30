using System.Net;
using System.Net.Http.Json;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Checks;
internal static class DemoHttpChecks
{
    public static async Task Run(Uri address)
    {
        await Check.Run("Seeded database supports real ADMIN and CUSTOMER HTTP queries with reconciled reports",async()=>
        {
            using var imageClient=new HttpClient{BaseAddress=address};using var image=await imageClient.GetAsync("/images/demo-phone.svg");Check.True(image.IsSuccessStatusCode&&image.Content.Headers.ContentType?.MediaType=="image/svg+xml"&&(await image.Content.ReadAsStringAsync()).Contains("DEMO"),"Seeded demo image resource is unavailable.");
            using var admin=new ApiTestClient(address);using var customer=new ApiTestClient(address);
            using var a=await admin.Send("/api/auth/login",new LoginRequest(DemoSeeder.AdminUsername,DemoSeedChecks.AdminPassword,"admin"));using var c=await customer.Send("/api/auth/login",new LoginRequest("demo.customer.01",DemoSeedChecks.CustomerPassword));Check.True(a.IsSuccessStatusCode&&c.IsSuccessStatusCode,"Seeded credential login failed.");
            using var products=await admin.Http.GetAsync("/api/admin/products?pageSize=100");var productPage=(await products.Content.ReadFromJsonAsync<Page<AdminProduct>>())!;Check.True(products.IsSuccessStatusCode&&productPage.Total==20,"Seeded products unavailable.");
            using var orders=await admin.Http.GetAsync("/api/admin/orders?pageSize=100");var orderPage=(await orders.Content.ReadFromJsonAsync<Page<OrderView>>())!;Check.True(orders.IsSuccessStatusCode&&orderPage.Total==20,"Seeded orders unavailable.");
            using var own=await customer.Http.GetAsync("/api/orders");Check.True((await own.Content.ReadFromJsonAsync<Page<OrderView>>())!.Total==1,"Seeded customer order ownership failed.");
            using var revenue=await admin.Http.GetAsync("/api/admin/reports?kind=revenue");var report=(await revenue.Content.ReadFromJsonAsync<StoreReport>())!;Check.True(revenue.IsSuccessStatusCode&&report.Orders==3&&report.Revenue==orderPage.Items.Where(x=>x.Status=="HOAN_TAT").Sum(x=>x.Total),"Seeded report did not reconcile.");
            using var denied=await customer.Http.GetAsync("/api/admin/reports?kind=stock");Check.True(denied.StatusCode==HttpStatusCode.Forbidden,"Seeded customer accessed ADMIN report.");
            using var cartResponse=await customer.Http.GetAsync("/api/cart");var cart=(await cartResponse.Content.ReadFromJsonAsync<CartView>())!;
            var input=new CheckoutInput(Guid.NewGuid(),"Khách kiểm tra API","0901000001","Địa chỉ kiểm tra API",cart.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price!.Value)).ToList());
            using var placed=await customer.Send("/api/orders",input);Check.True(placed.StatusCode==HttpStatusCode.Created,"Seeded HTTP checkout failed.");var order=(await placed.Content.ReadFromJsonAsync<OrderView>())!;
            using var retry=await customer.Send("/api/orders",input);Check.True(retry.StatusCode==HttpStatusCode.Created&&(await retry.Content.ReadFromJsonAsync<OrderView>())!.Id==order.Id,"HTTP idempotent checkout failed.");
            foreach(var action in new[]{"confirm","dispatch","complete"})
            {
                using var result=await admin.Send($"/api/admin/orders/{order.Id}/{action}",new VersionedCommand(order.Version,Guid.NewGuid()));Check.True(result.StatusCode==HttpStatusCode.NoContent,"HTTP fulfillment failed: "+action);
                using var read=await admin.Http.GetAsync($"/api/admin/orders/{order.Id}");order=(await read.Content.ReadFromJsonAsync<OrderView>())!;
            }
            using var invoice=await admin.Http.GetAsync($"/api/admin/orders/{order.Id}/invoice");using var after=await admin.Http.GetAsync("/api/admin/reports?kind=revenue");var updated=(await after.Content.ReadFromJsonAsync<StoreReport>())!;
            Check.True(invoice.IsSuccessStatusCode&&order.Payment=="DA_THU"&&updated.Orders==4&&updated.Revenue==report.Revenue+order.Total,"HTTP order/invoice/report end-to-end totals differ.");
        });
    }
}
