using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class InvoiceEndpoints
{
    public static void MapInvoices(this WebApplication app)=>app.MapGet("/api/admin/orders/{id:long}/invoice",async(long id,InvoiceService s,HttpContext c)=>Results.Ok(await s.Read(CommerceEndpoints.Actor(c),id))).RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
}
