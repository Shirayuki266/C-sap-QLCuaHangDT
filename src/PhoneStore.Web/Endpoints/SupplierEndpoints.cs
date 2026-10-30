using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
public static class SupplierEndpoints
{
    public static void MapSuppliers(this WebApplication app)
    {
        var routes = app.MapGroup("/api/admin/suppliers").RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
        routes.MapGet("/", (string? query, bool? active, int? page, int? pageSize, SupplierService service, HttpContext context)
            => service.List(CommerceEndpoints.Actor(context), query, active, page ?? 1, pageSize ?? 20));
        routes.MapPost("/", async (SupplierInput input, SupplierService service, HttpContext context)
            => Results.Json(new { id = await service.Save(CommerceEndpoints.Actor(context), 0, input) }, statusCode:201));
        routes.MapPut("/{id:long}", async (long id, SupplierInput input, SupplierService service, HttpContext context) =>
        {
            if (id <= 0) throw Rules.Error("INVALID_INPUT", "Mã nhà cung cấp không hợp lệ.");
            await service.Save(CommerceEndpoints.Actor(context), id, input); return Results.NoContent();
        });
    }
}
