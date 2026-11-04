using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class OrderFulfillmentEndpoints
{
    public static void MapOrderFulfillment(this WebApplication app)
    {
        var g=app.MapGroup("/api/admin/orders").RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
        g.MapPost("/{id:long}/dispatch",async(long id,VersionedCommand input,OrderWorkflow s,HttpContext c)=>{await s.Dispatch(CommerceEndpoints.Actor(c),id,input);return Results.NoContent();});
        g.MapPost("/{id:long}/complete",async(long id,VersionedCommand input,OrderWorkflow s,HttpContext c)=>{await s.Complete(CommerceEndpoints.Actor(c),id,input);return Results.NoContent();});
        g.MapPost("/{id:long}/fail",async(long id,VersionedCommand input,OrderWorkflow s,HttpContext c)=>{await s.Fail(CommerceEndpoints.Actor(c),id,input);return Results.NoContent();});
        g.MapPost("/{id:long}/receive-return",async(long id,VersionedCommand input,OrderWorkflow s,HttpContext c)=>{await s.ReceiveReturn(CommerceEndpoints.Actor(c),id,input);return Results.NoContent();});
    }
}
