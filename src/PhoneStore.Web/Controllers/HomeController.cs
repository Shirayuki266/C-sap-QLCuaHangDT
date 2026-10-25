using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core.Contracts;
namespace PhoneStore.Web.Controllers;
public sealed class HomeController(IStoreService store,IWebHostEnvironment environment):Controller
{
    public async Task<IActionResult> Index(){var products=await store.BestSellers();ViewData["HasSales"]=products.Count>0;if(products.Count==0)products=(await store.Catalog(new(PageSize:8))).Items;return View(products);}
    public IActionResult ImageCredits(){var file=Path.Combine(environment.WebRootPath,"data/image-credits.json");var entries=System.Text.Json.JsonSerializer.Deserialize<List<Credit>>(System.IO.File.ReadAllText(file),new System.Text.Json.JsonSerializerOptions{PropertyNameCaseInsensitive=true})??[];return View(entries);}
    public sealed record Credit(string Model,List<CreditImage> Images,string FallbackReason);
    public sealed record CreditImage(string Source,string Author,string License,string LicenseUrl);
}
