using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Web.Security;
using PhoneStore.Web.Models;
using PhoneStore.Web.Storefront;

namespace PhoneStore.Web.Controllers;

[Authorize(Policy = "Customer"), Route("Orders"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(IStoreService store) : Controller
{
    private Actor Current()
    {
        var session = SessionAuthentication.Current(HttpContext);
        return new(session.Account.Id, session.SessionId);
    }
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] OrderQuery filter)
    {
        if (!ModelState.IsValid) return View(new OrderListViewModel(filter, null));
        try
        {
            var results = await store.ListCustomerOrdersAsync(Current(), new(Query: filter.Code, Status: string.IsNullOrWhiteSpace(filter.Status) ? null : filter.Status,
                From: filter.From, To: filter.To, Page: filter.Page, PageSize: filter.PageSize));
            return View(new OrderListViewModel(filter, results));
        }
        catch (BusinessException error) when (error.Code == "INVALID_INPUT")
        { FormErrors.AddSummary(ModelState, error); return View(new OrderListViewModel(filter, null)); }
    }
    [HttpGet("Details/{id:long}")]
    public async Task<IActionResult> Details(long id)
    {
        var order = await store.ReadCustomerOrderAsync(Current(), id);
        return View(new OrderDetailsViewModel { Order = order, Id = order.Id, Version = order.Version });
    }
    [HttpPost("Cancel")]
    public async Task<IActionResult> Cancel(CancelOrderForm form)
    {
        // Read through the same ownership boundary even when field validation fails; both missing/foreign IDs remain404.
        var order = await store.ReadCustomerOrderAsync(Current(), form.Id);
        if (ModelState.IsValid)
        {
            try
            {
                await store.CancelCustomerOrderAsync(Current(), form.Id, form.Reason, form.Version);
                TempData["Notice"] = "Đã hủy đơn hàng. Lượng hàng giữ cho đơn đã được trả lại.";
                return RedirectToAction(nameof(Details), new { id = form.Id });
            }
            catch (BusinessException error) when (error.Code is "CONCURRENT_UPDATE" or "STALE_VERSION" or "INVALID_STATE")
            {
                TempData["Notice"] = "Đơn đã thay đổi, vui lòng tải lại. Yêu cầu hủy chưa được thực hiện.";
                return RedirectToAction(nameof(Details), new { id = form.Id });
            }
            catch (BusinessException error) when (error.Code == "INVALID_INPUT") { FormErrors.AddSummary(ModelState, error); }
        }
        return View("Details", new OrderDetailsViewModel { Order = order, Id = form.Id, Reason = form.Reason, Version = form.Version });
    }
}
