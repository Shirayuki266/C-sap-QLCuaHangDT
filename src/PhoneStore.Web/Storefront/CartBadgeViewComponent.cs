using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core;
using PhoneStore.Web.Security;

namespace PhoneStore.Web.Storefront;

public sealed class CartBadgeViewComponent(IStoreService store) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!UserClaimsPrincipal.IsInRole("CUSTOMER")) return Content("Giỏ hàng");
        var current = SessionAuthentication.Current(HttpContext);
        var cart = await store.ReadCartAsync(new(current.Account.Id, current.SessionId));
        return Content($"Giỏ hàng ({cart.Lines.Sum(line => line.Quantity)})");
    }
}
