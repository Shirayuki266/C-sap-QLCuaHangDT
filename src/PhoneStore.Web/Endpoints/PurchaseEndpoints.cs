using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class PurchaseEndpoints
{
    public static void MapPurchases(this WebApplication app)
    {
        var routes = app.MapGroup("/api/admin/purchases").RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
        routes.MapGet("/", (string? query, string? status, int? page, int? pageSize, PurchaseService service, HttpContext c) => service.List(CommerceEndpoints.Actor(c), query, status, page ?? 1, pageSize ?? 20));
        routes.MapGet("/{id:long}", (long id, PurchaseService service, HttpContext c) => service.Read(CommerceEndpoints.Actor(c), id));
        routes.MapPost("/", async (PurchaseInput input, PurchaseService service, HttpContext c) => Results.Json(new { id = await service.Save(CommerceEndpoints.Actor(c), 0, input) }, statusCode:201));
        routes.MapPut("/{id:long}", async (long id, PurchaseInput input, PurchaseService service, HttpContext c) =>
        {
            if (id <= 0) throw Rules.Error("INVALID_INPUT", "Mã phiếu nhập không hợp lệ.");
            await service.Save(CommerceEndpoints.Actor(c), id, input); return Results.NoContent();
        });
        routes.MapPost("/{id:long}/cancel", async (long id, VersionedCommand input, PurchaseService service, HttpContext c) =>
        { await service.Cancel(CommerceEndpoints.Actor(c), id, input); return Results.NoContent(); });
        routes.MapPost("/{id:long}/confirm", async (long id, VersionedCommand input, PurchaseService service, HttpContext c) =>
        { await service.Confirm(CommerceEndpoints.Actor(c), id, input); return Results.NoContent(); });
    }
}
