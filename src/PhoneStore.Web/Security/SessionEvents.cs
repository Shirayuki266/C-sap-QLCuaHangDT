using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Contracts;
using PhoneStore.Infrastructure.Data;
namespace PhoneStore.Web.Security;
public sealed class SessionEvents(StoreDbContext db):CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context) {
        if(!long.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),out var id)||!Guid.TryParse(context.Principal?.FindFirstValue("session"),out var session)
            || !await db.Sessions.AnyAsync(x=>x.Id==session&&x.AccountId==id&&x.ExpiresAt>DateTime.UtcNow&&x.Account.Active)) {
            context.RejectPrincipal(); await context.HttpContext.SignOutAsync();
        }
    }
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> c) { if(c.Request.Path.StartsWithSegments("/api")) {c.Response.StatusCode=401;return c.Response.WriteAsJsonAsync(new{code="UNAUTHORIZED",message="Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại."});}return base.RedirectToLogin(c); }
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> c) { if(c.Request.Path.StartsWithSegments("/api")) {c.Response.StatusCode=403;return c.Response.WriteAsJsonAsync(new{code="FORBIDDEN",message="Bạn không có quyền thực hiện chức năng này."});}c.Response.Redirect("/Account/Denied");return Task.CompletedTask; }
    public static Task SignIn(HttpContext context,UserDto user,Guid session)=>context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Name,user.Username),new Claim(ClaimTypes.Role,user.Role),new Claim("session",session.ToString())},CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=false,ExpiresUtc=DateTimeOffset.UtcNow.AddHours(8)});
}

