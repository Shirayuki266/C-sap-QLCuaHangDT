using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class OrderQueryEndpoints
{
    public static void MapOrderQueries(this WebApplication app)
    {
        var own=app.MapGroup("/api/orders").RequireAuthorization("Customer").AddEndpointFilter<CommerceCsrfFilter>();
        own.MapGet("/",(string? query,string? status,string? from,string? to,int? page,int? pageSize,OrderQueryService s,HttpContext c)=>s.List(CommerceEndpoints.Actor(c),new(query,status,from,to,Page:page??1,PageSize:pageSize??20)));
        own.MapGet("/{id:long}",(long id,OrderQueryService s,HttpContext c)=>s.Read(CommerceEndpoints.Actor(c),id));
        var admin=app.MapGroup("/api/admin").RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
        admin.MapGet("/orders",(string? query,string? status,string? from,string? to,long? customerId,int? page,int? pageSize,OrderQueryService s,HttpContext c)=>s.List(CommerceEndpoints.Actor(c),new(query,status,from,to,customerId,page??1,pageSize??20),true));
        admin.MapGet("/orders/{id:long}",(long id,OrderQueryService s,HttpContext c)=>s.Read(CommerceEndpoints.Actor(c),id,true));
        admin.MapGet("/customers/{id:long}/orders",(long id,string? status,string? from,string? to,int? page,int? pageSize,OrderQueryService s,HttpContext c)=>s.List(CommerceEndpoints.Actor(c),new(Status:status,From:from,To:to,CustomerId:id,Page:page??1,PageSize:pageSize??20),true));
    }
}
