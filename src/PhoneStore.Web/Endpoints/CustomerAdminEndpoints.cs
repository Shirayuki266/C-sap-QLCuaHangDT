using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class CustomerAdminEndpoints
{
    public static void MapCustomerAdmin(this WebApplication app)
    {
        var g=app.MapGroup("/api/admin/customers").RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
        g.MapGet("/",async(string? query,bool? active,int? page,int? pageSize,CustomerAdminService s,HttpContext c)=>Results.Ok(await s.List(CommerceEndpoints.Actor(c),query,active,page??1,pageSize??20)));
        g.MapGet("/{id:long}",async(long id,CustomerAdminService s,HttpContext c)=>Results.Ok(await s.Read(CommerceEndpoints.Actor(c),id)));
        g.MapPut("/{id:long}",async(long id,ProfileChangeRequest input,CustomerAdminService s,HttpContext c)=>{await s.Update(CommerceEndpoints.Actor(c),id,input);return Results.NoContent();});
        g.MapPost("/{id:long}/state",async(long id,CustomerStateInput input,CustomerAdminService s,HttpContext c)=>{await s.SetActive(CommerceEndpoints.Actor(c),id,input);return Results.NoContent();});
    }
}
