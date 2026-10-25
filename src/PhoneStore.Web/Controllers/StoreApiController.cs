using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhoneStore.Core.Contracts;
using PhoneStore.Infrastructure.Services;
using PhoneStore.Web.Security;
namespace PhoneStore.Web.Controllers;
[ApiController,Route("api"),AutoValidateAntiforgeryToken]
public sealed class StoreApiController(IStoreService store):ControllerBase
{
    private long Actor=>long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("security/token"),AllowAnonymous] public object Token([FromServices]IAntiforgery csrf)=>new{token=csrf.GetAndStoreTokens(HttpContext).RequestToken};
    [HttpPost("auth/register"),AllowAnonymous,EnableRateLimiting("auth")] public async Task<object> Register(RegisterRequest i)=>new{id=await store.Register(i)};
    [HttpPost("auth/login"),AllowAnonymous,EnableRateLimiting("auth")] public async Task<UserDto> Login(LoginRequest i,[FromQuery]bool admin=false) {var result=await store.Login(i,admin);await SessionEvents.SignIn(HttpContext,result.User,result.Session);return result.User;}
    [HttpPost("auth/logout"),Authorize] public async Task<object> Logout() {await store.Logout(Actor,Guid.Parse(User.FindFirstValue("session")!));await HttpContext.SignOutAsync();return new{success=true};}
    [HttpGet("profile"),Authorize] public Task<UserDto> Profile()=>store.GetUser(Actor);
    [HttpPut("profile"),Authorize(Roles="CUSTOMER")] public Task<bool> Profile(ProfileRequest i)=>store.UpdateProfile(Actor,i);
    [HttpPost("auth/password"),Authorize] public async Task<bool> Password(PasswordRequest i) {await store.ChangePassword(Actor,i);await HttpContext.SignOutAsync();return true;}
    [HttpGet("catalog"),AllowAnonymous] public async Task<Page<PublicProductDto>> Catalog([FromQuery]CatalogFilter f){var page=await store.Catalog(f);return new(page.Items.Select(PublicProductDto.From).ToList(),page.Total,page.PageNumber,page.PageSize);}
    [HttpGet("catalog/{id:long}"),AllowAnonymous] public async Task<PublicProductDto> Product(long id)=>PublicProductDto.From(await store.ProductDetail(id));
    [HttpGet("suggestions"),AllowAnonymous]public Task<List<SearchSuggestion>> Suggestions([FromQuery]string? query)=>store.Suggestions(query);
    [HttpGet("bestsellers"),AllowAnonymous]public async Task<List<PublicProductDto>> BestSellers()=>(await store.BestSellers()).Select(PublicProductDto.From).ToList();
    [HttpGet("lookups/{kind}"),AllowAnonymous] public Task<List<LookupDto>> Lookups(string kind)=>store.Lookups(kind);
    [HttpGet("cart"),Authorize(Roles="CUSTOMER")] public Task<CartDto> Cart()=>store.GetCart(Actor);
    [HttpPost("cart"),Authorize(Roles="CUSTOMER")] public Task<bool> AddCart(CartInput i)=>store.SetCart(Actor,i,true);
    [HttpPut("cart"),Authorize(Roles="CUSTOMER")] public Task<bool> SetCart(CartInput i)=>store.SetCart(Actor,i);
    [HttpPost("cart/merge"),Authorize(Roles="CUSTOMER")] public Task<bool> Merge(List<CartInput> i)=>store.MergeCart(Actor,i);
    [HttpPost("orders"),Authorize(Roles="CUSTOMER")] public async Task<OrderDto> Place(CheckoutRequest i)=>await store.GetOrder(Actor,await store.PlaceOrder(Actor,i));
    [HttpGet("orders"),Authorize(Roles="CUSTOMER")] public Task<Page<OrderDto>> Orders(string? query,string? status,DateOnly? from,DateOnly? to,int page=1)=>store.GetOrders(Actor,false,query,status,from,to,null,page);
    [HttpGet("orders/{id:long}"),Authorize(Roles="CUSTOMER")] public Task<OrderDto> Order(long id)=>store.GetOrder(Actor,id);
    [HttpPost("orders/{id:long}/cancel"),Authorize(Roles="CUSTOMER")] public Task<bool> Cancel(long id,TransitionRequest i)=>store.Transition(Actor,id,"cancel",i,false);
}
[ApiController,Route("api/admin"),Authorize(Roles="ADMIN"),AutoValidateAntiforgeryToken]
public sealed class AdminApiController(IStoreService store):ControllerBase
{
    private long Actor=>long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("products")] public Task<Page<ProductDto>> Products([FromQuery]CatalogFilter f)=>store.Catalog(f,true,Actor);
    [HttpGet("products/{id:long}")] public Task<ProductDto> Product(long id)=>store.ProductDetail(id,true,Actor);
    [HttpPost("products")] public async Task<object> Create(ProductInput i)=>new{id=await store.SaveProduct(Actor,0,i)};
    [HttpPut("products/{id:long}")] public async Task<object> Update(long id,ProductInput i)=>new{id=await store.SaveProduct(Actor,id,i)};
    [HttpGet("lookups/{kind}")] public Task<List<LookupDto>> Lookups(string kind)=>store.Lookups(kind,true,Actor);
    [HttpPost("lookups/{kind}")] public async Task<object> Lookup(string kind,LookupRequest i)=>new{id=await store.SaveLookup(Actor,kind,0,i)};
    [HttpPut("lookups/{kind}/{id:int}")] public async Task<object> Lookup(string kind,int id,LookupRequest i)=>new{id=await store.SaveLookup(Actor,kind,id,i)};
    [HttpGet("customers")] public Task<Page<CustomerDto>> Customers(string? query,int page=1)=>store.Customers(Actor,query,page);
    [HttpPut("customers/{id:long}")] public Task<bool> Customer(long id,CustomerInput i)=>store.SaveCustomer(Actor,id,i);
    [HttpGet("suppliers")] public Task<List<SupplierDto>> Suppliers(string? query)=>store.Suppliers(Actor,query);
    [HttpPost("suppliers")] public async Task<object> Supplier(SupplierInput i)=>new{id=await store.SaveSupplier(Actor,0,i)};
    [HttpPut("suppliers/{id:long}")] public async Task<object> Supplier(long id,SupplierInput i)=>new{id=await store.SaveSupplier(Actor,id,i)};
    [HttpGet("stock")] public Task<List<StockDto>> Stock(string? query,int? brandId,bool low=false)=>store.Stock(Actor,query,brandId,low);
    [HttpPost("stock/{id:long}/adjust")] public Task<bool> Adjust(long id,AdjustmentInput i)=>store.AdjustStock(Actor,id,i);
    [HttpGet("movements")] public Task<Page<MovementDto>> Movements(long? variantId,int page=1)=>store.Movements(Actor,variantId,page);
    [HttpGet("purchases")] public Task<Page<PurchaseDto>> Purchases(string? query,string? status,int page=1)=>store.Purchases(Actor,query,status,page);
    [HttpGet("purchases/{id:long}")] public Task<PurchaseDto> Purchase(long id)=>store.GetPurchase(Actor,id);
    [HttpPost("purchases")] public async Task<object> Purchase(PurchaseInput i)=>new{id=await store.SavePurchase(Actor,0,i)};
    [HttpPut("purchases/{id:long}")] public async Task<object> Purchase(long id,PurchaseInput i)=>new{id=await store.SavePurchase(Actor,id,i)};
    [HttpPost("purchases/{id:long}/confirm")] public Task<bool> Confirm(long id,TransitionRequest i)=>store.ConfirmPurchase(Actor,id,i);
    [HttpPost("purchases/{id:long}/cancel")] public Task<bool> Cancel(long id,TransitionRequest i)=>store.ConfirmPurchase(Actor,id,i,true);
    [HttpGet("orders")] public Task<Page<OrderDto>> Orders(string? query,string? status,DateOnly? from,DateOnly? to,long? customerId,int page=1)=>store.GetOrders(Actor,true,query,status,from,to,customerId,page);
    [HttpGet("orders/{id:long}")] public Task<OrderDto> Order(long id)=>store.GetOrder(Actor,id,true);
    [HttpPost("orders/{id:long}/{actionName}")] public Task<bool> Transition(long id,string actionName,TransitionRequest i)=>store.Transition(Actor,id,actionName,i,true);
    [HttpGet("reports")] public Task<ReportResult> Reports(string kind="revenue",DateOnly? from=null,DateOnly? to=null)=>store.Report(Actor,kind,from,to);
}

