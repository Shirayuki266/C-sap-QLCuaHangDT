using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using PhoneStore.Web.Models;
using PhoneStore.Web.Security;
using PhoneStore.Web.Storefront;

namespace PhoneStore.Web.Controllers;

[Route("Cart"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CartController(IStoreService store, CheckoutConfirmation confirmations) : Controller
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

    [Authorize(Policy = "Customer"), HttpGet("Checkout")]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        var actor = Current(); var cart = await store.ReadCartAsync(actor);
        if (cart.Lines.Count == 0) { TempData["Notice"] = "Giỏ hàng đang rỗng. Vui lòng chọn sản phẩm trước khi đặt hàng."; return RedirectToAction(nameof(Index)); }
        var profile = await store.ReadProfileAsync(actor, cancellationToken);
        return await CheckoutPage(new() { RequestKey = Guid.NewGuid(), Recipient = profile.FullName, Phone = profile.Phone, Address = profile.Address, PaymentMethod = "COD" }, cart);
    }

    private async Task<IActionResult> CheckoutPage(CheckoutViewModel model, CartView? cart = null)
    {
        model.Cart = cart ?? await store.ReadCartAsync(Current());
        model.Confirmation = confirmations.Protect(Current().AccountId, model.RequestKey, model.Cart);
        ModelState.Remove(nameof(model.Confirmation));
        return View("Checkout", model);
    }

    [Authorize(Policy = "Customer"), HttpPost("Checkout")]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var actor = Current();
        var lines = confirmations.Read(model.Confirmation, actor.AccountId, model.RequestKey);
        if (lines is null) ModelState.AddModelError(string.Empty, "Phiên xác nhận không hợp lệ. Vui lòng tải lại trang đặt hàng.");
        if (ModelState.IsValid)
        {
            try
            {
                var id = await store.PlaceOrderAsync(actor, new(model.RequestKey, model.Recipient, model.Phone, model.Address, lines, model.PaymentMethod!));
                TempData["Notice"] = "Đặt hàng COD thành công. Bạn có thể xem lại thông tin đơn bên dưới.";
                return RedirectToAction("Details", "Orders", new { id });
            }
            catch (PriceChangedException changed)
            {
                model.PriceChanged = true;
                ModelState.AddModelError(string.Empty, "Giá hoặc giỏ hàng đã thay đổi. Chưa tạo đơn và chưa giữ hàng. Vui lòng kiểm tra giá mới rồi xác nhận lại.");
                return await CheckoutPage(model, changed.Cart);
            }
            catch (BusinessException error) when (error.Code == "INVALID_STATE")
            {
                try
                {
                    var id = await store.ResolveOrderAsync(actor, model.RequestKey);
                    TempData["Notice"] = "Yêu cầu này đã có kết quả. Vui lòng kiểm tra đơn trước khi đặt thêm.";
                    return RedirectToAction("Details", "Orders", new { id });
                }
                catch (BusinessException missing) when (missing.Code == "NOT_FOUND") { FormErrors.AddSummary(ModelState, error); }
            }
            catch (BusinessException error) when (error.Code is "INVALID_INPUT" or "OUT_OF_STOCK" or "CONCURRENT_UPDATE") { FormErrors.AddSummary(ModelState, error); }
        }
        return await CheckoutPage(model);
    }

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
