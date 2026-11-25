using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core;
using PhoneStore.Web.Models;

namespace PhoneStore.Web.Controllers;

// [UC02.01] Controller trang chủ Website: hiển thị tối đa 8 điện thoại bán chạy trong tháng hiện tại.
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HomeController(IStoreService store) : Controller
{
    [HttpGet("/")]
    [HttpGet("/Home/Index")]
    public async Task<IActionResult> Index() => View(new HomeViewModel(await store.ReadBestsellersAsync()));
}
