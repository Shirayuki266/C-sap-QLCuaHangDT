using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Web.Models;
using PhoneStore.Web.Security;

namespace PhoneStore.Web.Controllers;

[Route("Cart"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CartController(IStoreService store) : Controller
{
    private Actor Current()
    {
        var session = SessionAuthentication.Current(HttpContext);
        return new(session.Account.Id, session.SessionId);
    }
    private async Task<IActionResult> Page(long? variant = null, string? attempted = null) =>
        View("Index", new CartPageViewModel(User.IsInRole("CUSTOMER") ? await store.ReadCartAsync(Current()) : new([], 0, false), !User.IsInRole("CUSTOMER"), variant, attempted));

    [HttpGet("Index")]
    public Task<IActionResult> Index() => Page();

    [Authorize(Policy = "Customer"), HttpPost("Add")]
    public async Task<IActionResult> Add(CartChangeForm form)
    {
        if (ModelState.IsValid)
        {
            try { await store.AddToCartAsync(Current(), form.ProductId, form.VariantId, form.Quantity); TempData["Notice"] = "Đã thêm sản phẩm vào giỏ hàng."; return RedirectToAction(nameof(Index)); }
            catch (BusinessException error) when (error.Code is "INVALID_INPUT" or "OUT_OF_STOCK") { ModelState.AddModelError(string.Empty, error.Message); }
        }
        return await Page(form.VariantId, Request.Form["quantity"]);
    }

    [Authorize(Policy = "Customer"), HttpPost("Update")]
    public async Task<IActionResult> Update(CartQuantityForm form)
    {
        if (ModelState.IsValid)
        {
            try { await store.UpdateCartAsync(Current(), form.VariantId, form.Quantity); TempData["Notice"] = "Đã cập nhật số lượng theo giá hiện tại."; return RedirectToAction(nameof(Index)); }
            catch (BusinessException error) when (error.Code is "INVALID_INPUT" or "OUT_OF_STOCK" or "NOT_FOUND") { ModelState.AddModelError(string.Empty, error.Message); }
        }
        return await Page(form.VariantId, Request.Form["quantity"]);
    }

    [Authorize(Policy = "Customer"), HttpPost("Remove")]
    public async Task<IActionResult> Remove(long variantId)
    {
        if (!ModelState.IsValid || variantId < 1) { ModelState.AddModelError(string.Empty, "Phiên bản không hợp lệ."); return await Page(variantId); }
        await store.RemoveCartAsync(Current(), variantId);
        TempData["Notice"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Quote")]
    public async Task<IActionResult> Quote([FromBody] CartQuoteForm form)
    {
        if (!ModelState.IsValid || form?.Lines is null) return BadRequest(new { code = "INVALID_INPUT", message = "Giỏ hàng không hợp lệ. Vui lòng kiểm tra số lượng." });
        try { return Json(await store.QuoteCartAsync(form.Lines)); }
        catch (BusinessException error) { return StatusCode(ApiError.Status(error.Code), new { code = error.Code, message = error.Message }); }
    }

    [Authorize(Policy = "Customer"), HttpPost("Merge")]
    public async Task<IActionResult> Merge([FromBody] CartQuoteForm form)
    {
        if (!ModelState.IsValid || form?.Lines is null) return BadRequest(new { code = "INVALID_INPUT", message = "Giỏ hàng không hợp lệ. Vui lòng kiểm tra số lượng." });
        try { return Json(await store.MergeCartAsync(Current(), form.Lines)); }
        catch (BusinessException error) { return StatusCode(ApiError.Status(error.Code), new { code = error.Code, message = error.Message }); }
    }
}
