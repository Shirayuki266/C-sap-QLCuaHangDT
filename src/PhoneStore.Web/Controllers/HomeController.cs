using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core;
using PhoneStore.Web.Models;

namespace PhoneStore.Web.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HomeController(IStoreService store) : Controller
{
    [HttpGet("/")]
    [HttpGet("/Home/Index")]
    public async Task<IActionResult> Index() => View(new HomeViewModel(await store.ReadBestsellersAsync()));
}
