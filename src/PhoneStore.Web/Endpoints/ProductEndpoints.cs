using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
// [UC06.01 - UC06.03] Định tuyến Admin API đọc chi tiết, thêm mới và cập nhật sản phẩm/biến thể (/api/admin/products).
public static class ProductEndpoints
{
    public static void MapProducts(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin/products").RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
        admin.MapGet("/{id:long}", (long id, ProductService service, HttpContext context) => service.Read(CommerceEndpoints.Actor(context), id));
        admin.MapPost("/", async (ProductInput input, ProductService service, HttpContext context)
            => Results.Json(new { id = await service.Save(CommerceEndpoints.Actor(context), 0, input) }, statusCode: 201));
        admin.MapPut("/{id:long}", async (long id, ProductInput input, ProductService service, HttpContext context) =>
        {
            if (id <= 0) throw Rules.Error("INVALID_INPUT", "Mã sản phẩm không hợp lệ.");
            await service.Save(CommerceEndpoints.Actor(context), id, input); return Results.NoContent();
        });
    }
}
