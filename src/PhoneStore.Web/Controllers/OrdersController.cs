using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Web.Security;

namespace PhoneStore.Web.Controllers;

[Authorize(Policy = "Customer"), Route("Orders"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(IStoreService store) : Controller
{
    [HttpGet("Details/{id:long}")]
    public async Task<IActionResult> Details(long id)
    {
        var session = SessionAuthentication.Current(HttpContext);
        return View(await store.ReadCustomerOrderAsync(new Actor(session.Account.Id, session.SessionId), id));
    }
}
