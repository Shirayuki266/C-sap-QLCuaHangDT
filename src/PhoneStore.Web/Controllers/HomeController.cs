using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.Web.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet("/")]
    [HttpGet("/Home/Index")]
    public IActionResult Index() => View();
}
