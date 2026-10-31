using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;
using PhoneStore.Web.Security;

namespace PhoneStore.Web.Storefront;

public sealed class StorefrontCookieEvents(SessionAuthentication original) : CookieAuthenticationEvents
{
    public override Task ValidatePrincipal(CookieValidatePrincipalContext context) => original.ValidatePrincipal(context);

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (!StorefrontHosting.IsWebsite(context.HttpContext)) return original.RedirectToLogin(context);
        context.Response.Headers.CacheControl = "no-store";
        var returnUrl = context.Request.GetEncodedPathAndQuery();
        context.Response.Redirect("/Account/Login?returnUrl=" + Uri.EscapeDataString(returnUrl));
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (!StorefrontHosting.IsWebsite(context.HttpContext)) return original.RedirectToAccessDenied(context);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
