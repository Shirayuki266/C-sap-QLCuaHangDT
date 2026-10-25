using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core.Contracts;
using PhoneStore.Infrastructure.Services;
namespace PhoneStore.Web.Controllers;
public sealed class CatalogController(IStoreService store):Controller
{
    public async Task<IActionResult> Index([FromQuery]CatalogFilter filter) {
        ViewData["Filter"]=filter;ViewData["Brands"]=await store.Lookups("brands");ViewData["Colors"]=await store.Colors();
        try{return View(await store.Catalog(filter));}catch(PhoneStore.Core.Models.BusinessException e){ModelState.AddModelError("",e.Message);return View(new Page<ProductDto>([],0,1,12));}
    }
    public async Task<IActionResult> Details(long id)=>View(await store.ProductDetail(id));
    [HttpGet("Search")]public async Task<IActionResult> Search([FromQuery]CatalogFilter filter){ViewData["Searching"]=true;var result=await Index(filter);if(result is ViewResult view)view.ViewName="Index";return result;}
}

