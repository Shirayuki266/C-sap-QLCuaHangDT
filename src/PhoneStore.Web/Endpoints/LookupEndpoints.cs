using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;

namespace PhoneStore.Web.Endpoints;
public static class LookupEndpoints
{
    public static void MapLookups(this WebApplication app)
    {
        app.MapGet("/api/lookups/{kind}", (string kind, LookupService service) => service.Public(kind));
        var admin = app.MapGroup("/api/admin/lookups").RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
        admin.MapGet("/{kind}", (string kind, string? query, int? page, int? pageSize, LookupService service, HttpContext context)
            => service.List(CommerceEndpoints.Actor(context), kind, query, page ?? 1, pageSize ?? 20));
        admin.MapPost("/{kind}", async (string kind, LookupInput input, LookupService service, HttpContext context)
            => Results.Json(new { id = await service.Save(CommerceEndpoints.Actor(context), kind, 0, input) }, statusCode: 201));
        admin.MapPut("/{kind}/{id:int}", async (string kind, int id, LookupInput input, LookupService service, HttpContext context) =>
        {
            if (id <= 0) throw Rules.Error("INVALID_INPUT", "Mã danh mục không hợp lệ.");
            await service.Save(CommerceEndpoints.Actor(context), kind, id, input); return Results.NoContent();
        });
    }
}
