using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;

namespace PhoneStore.Web.Security;

public sealed class SessionAuthentication(IAccountSessions sessions) : CookieAuthenticationEvents
{
    public const string SessionClaim = "phone_store_session";
    private const string ActorKey = "PhoneStore.ValidatedSession";

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        AccountSession? session = null;
        if (long.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), NumberStyles.None, CultureInfo.InvariantCulture, out var accountId) &&
            Guid.TryParse(context.Principal?.FindFirstValue(SessionClaim), out var sessionId))
            session = await sessions.ValidateAsync(accountId, sessionId, context.HttpContext.RequestAborted);
        if (session is null || session.Account.Role != context.Principal?.FindFirstValue(ClaimTypes.Role))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }
        context.HttpContext.Items[ActorKey] = session;
        context.ShouldRenew = false;
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context) => Reject(context.HttpContext, 401, "UNAUTHORIZED", "Vui lòng đăng nhập để tiếp tục.");
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) => Reject(context.HttpContext, 403, "FORBIDDEN", "Bạn không có quyền thực hiện chức năng này.");

    private static Task Reject(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ApiError(code, message, context.TraceIdentifier));
    }

    public static AccountSession Current(HttpContext context) => context.Items[ActorKey] as AccountSession
        ?? throw new BusinessException("UNAUTHORIZED", "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");

    public static ClaimsPrincipal Principal(AccountSession session) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, session.Account.Id.ToString(CultureInfo.InvariantCulture)),
        new Claim(ClaimTypes.Name, session.Account.Username),
        new Claim(ClaimTypes.Role, session.Account.Role),
        new Claim(SessionClaim, session.SessionId.ToString("D"))
    ], CookieAuthenticationDefaults.AuthenticationScheme));
}
