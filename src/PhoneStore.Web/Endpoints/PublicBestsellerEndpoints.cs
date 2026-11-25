using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Web.Endpoints;
// [UC02.01] Định tuyến API công khai trả danh sách sản phẩm bán chạy trong tháng (/api/catalog/bestsellers).
public static class PublicBestsellerEndpoints
{
    public static void MapPublicBestsellers(this WebApplication app)=>app.MapGet("/api/catalog/bestsellers",(PublicBestsellerService service)=>service.Read());
}
