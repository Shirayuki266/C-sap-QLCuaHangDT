using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class InventoryEndpoints
{
    public static void MapInventory(this WebApplication app)
    {
        var routes=app.MapGroup("/api/admin/stock").RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
        routes.MapGet("/",(string? query,int? brandId,int? below,int? page,int? pageSize,InventoryService s,HttpContext c)=>s.List(CommerceEndpoints.Actor(c),query,brandId,below,page??1,pageSize??20));
        routes.MapGet("/movements",(long? variantId,Guid? operationKey,int? page,int? pageSize,InventoryService s,HttpContext c)=>s.Movements(CommerceEndpoints.Actor(c),variantId,operationKey,page??1,pageSize??20));
        routes.MapPost("/{id:long}/adjust",async(long id,StockCountInput input,InventoryService s,HttpContext c)=>{await s.Adjust(CommerceEndpoints.Actor(c),id,input);return Results.NoContent();});
    }
}
