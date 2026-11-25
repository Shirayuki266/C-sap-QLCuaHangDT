using PhoneStore.Core.Commerce;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Endpoints;
// [UC02.01, UC02.02, UC06.01] Định tuyến API danh mục công khai (/api/catalog) và danh sách sản phẩm nội bộ Admin (/api/admin/products).
public static class CatalogEndpoints
{
    public static void MapCatalog(this WebApplication app)
    {
        app.MapGet("/api/catalog", (string? query, int? brandId, decimal? minPrice, decimal? maxPrice, int? storageGb, string? color, string? sort, int? page, int? pageSize, CatalogService service)
            => service.List(new(query, brandId, minPrice, maxPrice, storageGb, color, sort ?? "name", page ?? 1, pageSize ?? 20)));
        app.MapGet("/api/catalog/{id:long}", (long id, CatalogService service) => service.Read(id));
        app.MapGet("/api/admin/products", (string? query, int? brandId, decimal? minPrice, decimal? maxPrice, int? storageGb, string? color, string? sort, int? page, int? pageSize, bool? active, CatalogService service, HttpContext context)
            => service.Internal(CommerceEndpoints.Actor(context), new(query, brandId, minPrice, maxPrice, storageGb, color, sort ?? "name", page ?? 1, pageSize ?? 20, active)))
            .RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
    }
}
