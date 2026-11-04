using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class CheckoutEndpoints
{
    public static void MapCheckout(this WebApplication app)
    {
        var routes=app.MapGroup("/api/orders").RequireAuthorization("Customer").AddEndpointFilter<CommerceCsrfFilter>();
        routes.MapPost("/",async(CheckoutInput input,CheckoutService s,HttpContext c)=>Results.Json(await s.Place(CommerceEndpoints.Actor(c),input),statusCode:201));
        routes.MapGet("/by-request/{key:guid}",(Guid key,CheckoutService s,HttpContext c)=>s.Resolve(CommerceEndpoints.Actor(c),key));
    }
}
