using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class ReportEndpoints
{
    public static void MapReports(this WebApplication app)=>app.MapGet("/api/admin/reports",async(string kind,string? from,string? to,ReportService s,HttpContext c)=>Results.Ok(await s.Read(CommerceEndpoints.Actor(c),kind,from,to))).RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
}
