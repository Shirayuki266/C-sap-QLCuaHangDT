using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class CartEndpoints
{
    public static void MapCarts(this WebApplication app)
    {
        app.MapPost("/api/cart/preview",(List<CartInput> inputs,CartService s)=>s.Preview(inputs)).AddEndpointFilter<CommerceCsrfFilter>();
        var routes=app.MapGroup("/api/cart").RequireAuthorization("Customer").AddEndpointFilter<CommerceCsrfFilter>();
        routes.MapGet("/",(CartService s,HttpContext c)=>s.Read(CommerceEndpoints.Actor(c)));
        routes.MapPost("/",async(CartInput input,CartService s,HttpContext c)=>{await s.Set(CommerceEndpoints.Actor(c),input,true);return Results.NoContent();});
        routes.MapPut("/",async(CartInput input,CartService s,HttpContext c)=>{await s.Set(CommerceEndpoints.Actor(c),input);return Results.NoContent();});
        routes.MapPost("/merge",async(List<CartInput> inputs,CartService s,HttpContext c)=>{await s.Merge(CommerceEndpoints.Actor(c),inputs);return Results.NoContent();});
    }
}
