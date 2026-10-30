using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class OrderDecisionEndpoints
{
    public static void MapOrderDecisions(this WebApplication app)
    {
        app.MapPost("/api/orders/{id:long}/cancel",async(long id,VersionedCommand input,OrderWorkflow s,HttpContext c)=>{await s.Cancel(CommerceEndpoints.Actor(c),id,input);return Results.NoContent();})
            .RequireAuthorization("Customer").AddEndpointFilter<CommerceCsrfFilter>();
        var admin=app.MapGroup("/api/admin/orders").RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
        admin.MapPost("/{id:long}/confirm",async(long id,VersionedCommand input,OrderWorkflow s,HttpContext c)=>{await s.Confirm(CommerceEndpoints.Actor(c),id,input);return Results.NoContent();});
        admin.MapPost("/{id:long}/cancel",async(long id,VersionedCommand input,OrderWorkflow s,HttpContext c)=>{await s.Cancel(CommerceEndpoints.Actor(c),id,input,true);return Results.NoContent();});
    }
}
