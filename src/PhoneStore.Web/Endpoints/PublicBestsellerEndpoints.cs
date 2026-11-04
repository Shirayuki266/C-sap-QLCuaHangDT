using PhoneStore.Infrastructure.Commerce;
namespace PhoneStore.Web.Endpoints;
public static class PublicBestsellerEndpoints
{
    public static void MapPublicBestsellers(this WebApplication app)=>app.MapGet("/api/catalog/bestsellers",(PublicBestsellerService service)=>service.Read());
}
