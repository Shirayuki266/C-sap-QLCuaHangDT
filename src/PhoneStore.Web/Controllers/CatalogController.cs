using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core;
using PhoneStore.Web.Models;
using PhoneStore.Web.Storefront;

namespace PhoneStore.Web.Controllers;

[Route("Catalog"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CatalogController(IStoreService store) : Controller
{
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] CatalogQuery query)
    {
        ViewData["CatalogQuery"] = query.Q;
        var brands = await store.ReadBrandsAsync();
        if (ModelState.IsValid)
        {
            try { return View(new CatalogIndexViewModel(query, brands, await store.ReadCatalogAsync(query.ToFilter()))); }
            catch (BusinessException error) when (error.Code is "INVALID_INPUT" or "VALIDATION_FAILED") { FormErrors.AddSummary(ModelState, error); }
        }
        // Pagination policy is400; filter syntax/range errors are a soft200 with the entered form retained.
        if (new[] { nameof(query.Page), nameof(query.PageSize) }.Any(key => ModelState.TryGetValue(key, out var entry) && entry.Errors.Count > 0))
            Response.StatusCode = StatusCodes.Status400BadRequest;
        return View(new CatalogIndexViewModel(query, brands, null));
    }

    [HttpGet("Details/{id:long}")]
    public async Task<IActionResult> Details(long id, long? variantId)
    {
        if (!ModelState.IsValid) return BadRequest();
        var product = await store.ReadProductAsync(id, variantId);
        var selected = variantId.HasValue ? product.Variants.Single(variant => variant.Id == variantId) :
            product.Variants.FirstOrDefault(variant => variant.Available > 0) ?? product.Variants.First();
        return View(new ProductDetailsViewModel(product, selected));
    }
}
