using System.Net.Http.Json;
using PhoneStore.Core.Contracts;
internal sealed partial class QaSuite
{
    async Task BoundaryMatrix(List<Endpoint> endpoints){
        var product=await admin.Read<ProductDto>("/api/admin/products/1");var order=await admin.Read<OrderDto>("/api/admin/orders/1");var stock=await Stock(1);var draftId=await Create("/api/admin/purchases",new PurchaseInput(supplier,[]));var draft=await admin.Read<PurchaseDto>($"/api/admin/purchases/{draftId}");var i=0;
        foreach(var e in endpoints){i++;if(e.Method=="GET"||e.Route=="/api/auth/logout")continue;object? invalid=e.Route switch {
            "/api/admin/products" or "/api/admin/products/{id:long}"=>Input(product) with{Code=new string('X',31)},
            "/api/admin/customers/{id:long}"=>new CustomerInput(new string('X',121),"0911111111",null,null,true),
            "/api/admin/suppliers" or "/api/admin/suppliers/{id:long}"=>new SupplierInput(new string('X',31),"QA",null,null,true),
            "/api/admin/lookups/{kind}" or "/api/admin/lookups/{kind}/{id:int}"=>new LookupRequest(new string('X',101),true),
            "/api/admin/purchases" or "/api/admin/purchases/{id:long}"=>new PurchaseInput(supplier,[new(1,-1,100m)],draft.Version),
            "/api/admin/purchases/{id:long}/confirm" or "/api/admin/purchases/{id:long}/cancel"=>new TransitionRequest("invalid",Guid.Empty),
            "/api/admin/orders/{id:long}/{actionName}" or "/api/orders/{id:long}/cancel"=>new TransitionRequest(order.Version,Guid.NewGuid(),new string('X',501)),
            "/api/admin/stock/{id:long}/adjust"=>new AdjustmentInput(-1,"QA",stock.Version,Guid.NewGuid()),
            "/api/auth/login"=>new LoginRequest(new string('X',101),customerPassword),
            "/api/auth/register"=>new RegisterRequest(new string('X',101),customerPassword,"QA","0966666666",null,null),
            "/api/auth/password"=>new PasswordRequest(new string('X',129),customerPassword),
            "/api/profile"=>new ProfileRequest(new string('X',121),"0911111111",null,null),
            "/api/cart"=>new CartInput(1,-1),
            "/api/cart/merge"=>new[]{new CartInput(1,-1)},
            "/api/guest-cart/preview"=>new[]{new CartInput(1,-1)},
            "/api/orders"=>new CheckoutRequest(Guid.NewGuid(),new string('X',121),"0911111111","123 Nguyễn Trãi",[]),
            _=>null};
            if(invalid==null)continue;var path=e.Route.Replace("{id:long}","1").Replace("{id:int}","1").Replace("{kind}","brands").Replace("{actionName}","confirm");if(e.Route=="/api/admin/customers/{id:long}")path="/api/admin/customers/"+aId;if(e.Route=="/api/admin/purchases/{id:long}")path="/api/admin/purchases/"+draftId;var client=e.Role=="ADMIN"?admin:e.Role=="PUBLIC"?anon:a;
            await Check("EP"+i.ToString("000")+"-BOUND",e.Method+" "+e.Route+" giá trị biên sai","HTTP4xx và không500",async()=>await Status(await client.Send(path,invalid,new HttpMethod(e.Method)),400,409),kind:"HTTP validation");
        }
    }
}

