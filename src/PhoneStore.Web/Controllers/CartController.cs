using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Contracts;
using PhoneStore.Core.Models;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Infrastructure.Services;
using PhoneStore.Web.Models;
namespace PhoneStore.Web.Controllers;
public sealed class CartController(IStoreService store):Controller
{
    private long Actor=>long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Index()=>View(User.IsInRole("CUSTOMER")?await store.GetCart(Actor):new CartDto([]));
    [Authorize(Roles="CUSTOMER"),HttpPost] public async Task<IActionResult> Add(CartInput input) {
        if(!ModelState.IsValid){TempData["Error"]="Mã biến thể và số lượng phải là số nguyên hợp lệ.";return RedirectToAction(nameof(Index));}
        try {await store.SetCart(Actor,input,true);TempData["Success"]="Đã thêm sản phẩm vào giỏ.";}catch(BusinessException e){TempData["Error"]=e.Message;}return RedirectToAction(nameof(Index));
    }
    [Authorize(Roles="CUSTOMER"),HttpPost] public async Task<IActionResult> Update(CartInput input) {
        if(!ModelState.IsValid){TempData["Error"]="Mã biến thể và số lượng phải là số nguyên hợp lệ. Giỏ hàng chưa được thay đổi.";return RedirectToAction(nameof(Index));}
        try{await store.SetCart(Actor,input);TempData["Success"]=input.Quantity==0?"Đã xóa mặt hàng.":"Đã cập nhật số lượng.";}catch(BusinessException e){TempData["Error"]=e.Message;}return RedirectToAction(nameof(Index));
    }
    [Authorize(Roles="CUSTOMER")] public async Task<IActionResult> Checkout() {
        var user=await store.GetUser(Actor);var cart=await store.GetCart(Actor); if(cart.Lines.Count==0){TempData["Error"]="Giỏ hàng đang trống.";return RedirectToAction(nameof(Index));}
        return View(new CheckoutModel{RequestKey=Guid.NewGuid(),Recipient=user.FullName,Phone=user.Phone,Address=user.Address??"",Cart=cart,Lines=cart.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price)).ToList()});
    }
    [Authorize(Roles="CUSTOMER"),HttpPost] public async Task<IActionResult> Checkout(CheckoutModel input) {
        if(ModelState.IsValid) try {var id=await store.PlaceOrder(Actor,new(input.RequestKey,input.Recipient,input.Phone,input.Address,input.Lines,input.PaymentMethod));TempData["Success"]="Đặt hàng thành công. Đơn hàng đang chờ xác nhận.";return RedirectToAction("Details","Orders",new{id});}catch(BusinessException e){ModelState.AddModelError("",e.Message);}
        input.Cart=await store.GetCart(Actor);
        if(input.Lines.Count!=input.Cart.Lines.Count||input.Lines.Any(x=>!input.Cart.Lines.Any(l=>l.VariantId==x.VariantId&&l.Price==x.Price&&l.Quantity==x.Quantity))) {
            ModelState.AddModelError("","Giá và số lượng bên dưới đã cập nhật; vui lòng kiểm tra trước khi xác nhận lại.");
            ModelState.Remove(nameof(input.Lines));
            foreach(var key in ModelState.Keys.Where(x=>x.StartsWith("Lines[")).ToArray()) ModelState.Remove(key);
            input.Lines=input.Cart.Lines.Select(x=>new PriceConfirmation(x.VariantId,x.Quantity,x.Price)).ToList();
        }
        return View(input);
    }
    [HttpPost("api/guest-cart/preview")] public async Task<CartDto> Preview([FromBody]List<CartInput> inputs) {
        return await store.PreviewCart(inputs);
    }
}
[Authorize(Roles="CUSTOMER")]
public sealed class OrdersController(IStoreService store):Controller
{
    private long Actor=>long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Index(string? status,DateOnly? from,DateOnly? to,int page=1) {
        try{return View(await store.GetOrders(Actor,false,null,status,from,to,null,page));}catch(BusinessException e){ModelState.AddModelError("",e.Message);return View(new Page<OrderDto>([],0,1,20));}
    }
    public async Task<IActionResult> Details(long id)=>View(await store.GetOrder(Actor,id));
    [HttpPost] public async Task<IActionResult> Cancel(long id,TransitionRequest input) {try{await store.Transition(Actor,id,"cancel",input,false);TempData["Success"]="Đã hủy đơn và giải phóng hàng giữ.";}catch(BusinessException e){TempData["Error"]=e.Message;}return RedirectToAction(nameof(Details),new{id});}
}






