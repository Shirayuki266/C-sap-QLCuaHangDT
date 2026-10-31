using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Core.Commerce;
using PhoneStore.Web.Models;
using PhoneStore.Web.Security;
using PhoneStore.Web.Storefront;

namespace PhoneStore.Web.Controllers;

[Route("Account"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountController(IStoreService store) : Controller
{
    private const string LoginError = "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản không được phép đăng nhập.";
    private Actor Current()
    {
        var session = SessionAuthentication.Current(HttpContext);
        return new(session.Account.Id, session.SessionId);
    }
    private string LocalDestination(string? destination) => Url.IsLocalUrl(destination) ? destination! : "/Home/Index";
    private static bool IsFormError(BusinessException error) => error.Code is
        "INVALID_INPUT" or "VALIDATION_FAILED" or "DUPLICATE_VALUE" or "CONCURRENT_UPDATE";
    private void ForgetPasswords(params string[] fields)
    {
        foreach (var field in fields)
            if (ModelState.TryGetValue(field, out var entry)) { entry.RawValue = null; entry.AttemptedValue = null; }
    }

    [HttpGet("Register")]
    public IActionResult Register() => User.Identity?.IsAuthenticated == true ? LocalRedirect("/Home/Index") : View(new RegisterViewModel());

    [HttpPost("Register")]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true) return LocalRedirect("/Home/Index");
        if (ModelState.IsValid)
        {
            try
            {
                await store.RegisterAsync(new(model.Username, model.Password, model.FullName, model.Phone, model.Email, model.Address), cancellationToken);
                TempData["Notice"] = "Đăng ký thành công. Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction(nameof(Login));
            }
            catch (BusinessException error) when (IsFormError(error)) { FormErrors.AddSummary(ModelState, error); }
        }
        model.Password = null; ForgetPasswords(nameof(model.Password));
        return View(model);
    }

    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl) => User.Identity?.IsAuthenticated == true ?
        LocalRedirect("/Home/Index") : View(new LoginViewModel { ReturnUrl = LocalDestination(returnUrl) });

    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true) return LocalRedirect("/Home/Index");
        model.ReturnUrl = LocalDestination(model.ReturnUrl);
        ModelState.Remove(nameof(model.ReturnUrl));
        if (ModelState.IsValid)
        {
            try
            {
                var session = await store.LoginAsync(model.Username, model.Password, cancellationToken);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, SessionAuthentication.Principal(session), new AuthenticationProperties
                {
                    IssuedUtc = session.ExpiresAt - SessionPolicy.Lifetime, ExpiresUtc = session.ExpiresAt,
                    IsPersistent = false, AllowRefresh = false
                });
                TempData["Notice"] = "Đăng nhập thành công.";
                return LocalRedirect(model.ReturnUrl);
            }
            catch (BusinessException error) when (error.Code is "UNAUTHORIZED" or "UNAUTHENTICATED" or "INVALID_CREDENTIALS" or "ACCOUNT_LOCKED")
            { ModelState.AddModelError(string.Empty, LoginError); }
            catch (BusinessException error) when (IsFormError(error)) { FormErrors.AddSummary(ModelState, error); }
        }
        model.Password = null; ForgetPasswords(nameof(model.Password));
        return View(model);
    }

    [Authorize(Policy = "Customer"), HttpPost("Logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await store.LogoutAsync(Current(), cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Notice"] = "Bạn đã đăng xuất.";
        return LocalRedirect("/Home/Index");
    }

    [Authorize(Policy = "Customer"), HttpGet("Profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var profile = await store.ReadProfileAsync(Current(), cancellationToken);
        return View(new ProfileViewModel { Username = profile.Username, FullName = profile.FullName,
            Phone = profile.Phone, Email = profile.Email, Address = profile.Address });
    }

    [Authorize(Policy = "Customer"), HttpPost("Profile")]
    public async Task<IActionResult> Profile(ProfileViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await store.UpdateProfileAsync(Current(), new(model.FullName, model.Phone, model.Email, model.Address), cancellationToken);
                TempData["Notice"] = "Đã lưu thông tin tài khoản.";
                return RedirectToAction(nameof(Profile));
            }
            catch (BusinessException error) when (IsFormError(error)) { FormErrors.AddSummary(ModelState, error); }
        }
        // Read display-only username from the validated session, never from the form.
        model.Username = SessionAuthentication.Current(HttpContext).Account.Username;
        return View(model);
    }

    [Authorize(Policy = "Customer"), HttpGet("Password")]
    public IActionResult Password() => View(new ChangePasswordViewModel());

    [Authorize(Policy = "Customer"), HttpPost("Password")]
    public async Task<IActionResult> Password(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await store.ChangePasswordAsync(Current(), new(model.CurrentPassword, model.NewPassword), cancellationToken);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["Notice"] = "Đã đổi mật khẩu và đăng xuất mọi thiết bị. Vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(Login));
            }
            // New-password syntax is validated above using the exact DEC.01 policy.
            // The existing service's remaining INVALID_INPUT case is the current password; never parse its message.
            catch (BusinessException error) when (error.Code == "INVALID_INPUT") { ModelState.AddModelError(nameof(model.CurrentPassword), error.Message); }
            catch (BusinessException error) when (IsFormError(error)) { FormErrors.AddSummary(ModelState, error); }
        }
        model.CurrentPassword = null; model.NewPassword = null;
        ForgetPasswords(nameof(model.CurrentPassword), nameof(model.NewPassword));
        return View(model);
    }
}
