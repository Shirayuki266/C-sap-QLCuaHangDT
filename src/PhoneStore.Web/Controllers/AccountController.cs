using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhoneStore.Core.Contracts;
using PhoneStore.Core.Models;
using PhoneStore.Infrastructure.Services;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Controllers;
public sealed class AccountController(IStoreService store):Controller
{
    private long Actor=>long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public IActionResult Login()=>View(new LoginRequest("",""));
    [HttpPost,EnableRateLimiting("auth")] public async Task<IActionResult> Login(LoginRequest input,string? returnUrl) {
        if(!ModelState.IsValid) return View(input);
        try {var result=await store.Login(input); if(result.User.Role!="CUSTOMER") {await store.Logout(result.User.Id,result.Session);throw new BusinessException("FORBIDDEN","Vui lòng dùng WinApp để đăng nhập quản trị.");} await SessionEvents.SignIn(HttpContext,result.User,result.Session);return LocalRedirect(Url.IsLocalUrl(returnUrl)?returnUrl!:"/Cart");}
        catch(BusinessException e) {ModelState.AddModelError("",e.Message);return View(input with{Password=""});}
    }
    public IActionResult Register()=>View(new RegisterRequest("","","","",null,null));
    [HttpPost,EnableRateLimiting("auth")] public async Task<IActionResult> Register(RegisterRequest input) {
        if(!ModelState.IsValid) return View(input);
        try {await store.Register(input);TempData["Success"]="Đăng ký thành công. Vui lòng đăng nhập.";return RedirectToAction(nameof(Login));}
        catch(BusinessException e) {ModelState.AddModelError("",e.Message);return View(input with{Password=""});}
    }
    [Authorize(Roles="CUSTOMER")] public async Task<IActionResult> Profile() {var u=await store.GetUser(Actor);return View(new ProfileRequest(u.FullName,u.Phone,u.Email,u.Address));}
    [HttpPost,Authorize(Roles="CUSTOMER")] public async Task<IActionResult> Profile(ProfileRequest input) {
        if(!ModelState.IsValid) return View(input); try {await store.UpdateProfile(Actor,input);TempData["Success"]="Đã cập nhật hồ sơ.";return RedirectToAction(nameof(Profile));} catch(BusinessException e){ModelState.AddModelError("",e.Message);return View(input);}
    }
    [Authorize] public IActionResult Password()=>View(new PasswordRequest("",""));
    [Authorize,HttpPost] public async Task<IActionResult> Password(PasswordRequest input) {
        if(!ModelState.IsValid) return View(input);try{await store.ChangePassword(Actor,input);await HttpContext.SignOutAsync();TempData["Success"]="Đã đổi mật khẩu và kết thúc các phiên cũ. Vui lòng đăng nhập lại.";return RedirectToAction(nameof(Login));}catch(BusinessException e){ModelState.AddModelError("",e.Message);return View(new PasswordRequest("",""));}
    }
    [Authorize,HttpPost] public async Task<IActionResult> Logout() {await store.Logout(Actor,Guid.Parse(User.FindFirstValue("session")!));await HttpContext.SignOutAsync();return RedirectToAction("Index","Catalog");}
    public IActionResult Denied()=>StatusCode(403,"Bạn không có quyền truy cập chức năng này.");
}

