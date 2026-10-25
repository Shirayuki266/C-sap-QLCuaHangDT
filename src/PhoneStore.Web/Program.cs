using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Models;
using PhoneStore.Core.Contracts;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Infrastructure.Services;
using PhoneStore.Web.Security;
var commandFlags=new[]{"--migrate","--seed","--schema"};
var builder=WebApplication.CreateBuilder(args.Where(x=>!commandFlags.Contains(x)).ToArray());
builder.Logging.ClearProviders(); if(Environment.GetEnvironmentVariable("PHONESTORE_LAUNCHER_MANAGED")!="1")builder.Logging.AddConsole();
var isQa=builder.Environment.IsEnvironment("QA");
if(isQa) QaTarget.Validate(Environment.GetEnvironmentVariable("ConnectionStrings__Store"),builder.Configuration["QA:Server"],builder.Configuration["QA:Database"]);
builder.Services.AddDbContext<StoreDbContext>(o=>o.UseSqlServer(builder.Configuration.GetConnectionString("Store")??"Server=localhost;Database=PhoneStore;Integrated Security=True;TrustServerCertificate=True;Encrypt=True"));
builder.Services.AddScoped<StoreService>(); builder.Services.AddScoped<IStoreService>(sp=>sp.GetRequiredService<StoreService>()); builder.Services.AddScoped<SessionEvents>();
builder.Services.AddControllersWithViews(o=>{o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());o.Filters.Add(new ApiAntiforgeryResultFilter());});
builder.Services.Configure<ApiBehaviorOptions>(o=>o.InvalidModelStateResponseFactory=_=>new BadRequestObjectResult(new{code="INVALID_INPUT",message="Dữ liệu thiếu hoặc không hợp lệ. Vui lòng kiểm tra các trường bắt buộc và định dạng số/ngày."}));
builder.Services.AddAntiforgery(o=>o.HeaderName="X-CSRF-TOKEN");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{o.Cookie.Name="PhoneStore.Session";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;o.LoginPath="/Account/Login";o.AccessDeniedPath="/Account/Denied";o.EventsType=typeof(SessionEvents);o.ExpireTimeSpan=TimeSpan.FromHours(8);o.SlidingExpiration=false;});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;o.AddPolicy("auth",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=20,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));});
if(builder.Environment.IsDevelopment()||isQa) builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
var app=builder.Build();
using(var scope=app.Services.CreateScope()) {
    var db=scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    if(args.Contains("--schema")) {var directory=Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,"../../.artifacts/schema"));Directory.CreateDirectory(directory);await File.WriteAllTextAsync(Path.Combine(directory,"candidate-schema.sql"),db.Database.GenerateCreateScript());return;}
    if(args.Contains("--migrate")) await DatabaseSetup.Migrate(db);
    if(args.Contains("--seed")) {
        if(!app.Environment.IsDevelopment()&&(string.IsNullOrEmpty(builder.Configuration["Seed:AdminPassword"])||string.IsNullOrEmpty(builder.Configuration["Seed:CustomerPassword"]))) throw new InvalidOperationException("Cần cấu hình Seed passwords ngoài Development.");
        await scope.ServiceProvider.GetRequiredService<StoreService>().Seed(builder.Configuration["Seed:AdminPassword"]??"Admin@12345",builder.Configuration["Seed:CustomerPassword"]??"Khach@12345");
    }
    if(args.Contains("--migrate")||args.Contains("--seed")) return;
}
var culture=CultureInfo.GetCultureInfo("vi-VN"); CultureInfo.DefaultThreadCurrentCulture=culture;CultureInfo.DefaultThreadCurrentUICulture=culture;
app.Use(async(context,next)=>{
    context.Response.Headers["X-Content-Type-Options"]="nosniff";context.Response.Headers["Referrer-Policy"]="same-origin";
    try {await next();}
    catch(Exception ex) {
        if(context.Response.HasStarted) throw;
        var business=ex as BusinessException; var code=business?.Code??"SERVER_ERROR";
        var status=code switch{"UNAUTHORIZED"=>401,"FORBIDDEN"=>403,"NOT_FOUND"=>404,"OUT_OF_STOCK" or "INVALID_STATE" or "CONCURRENT_UPDATE" or "PRICE_CHANGED"=>409,"INVALID_INPUT"=>400,_=>500};
        var message=business?.Message??"Không thể xử lý yêu cầu. Vui lòng thử lại hoặc liên hệ quản trị.";
        if(business is null) app.Logger.LogError(ex,"Request failed {TraceId}",context.TraceIdentifier);
        context.Response.Clear();context.Response.StatusCode=status;
        if(context.Request.Path.StartsWithSegments("/api")) await context.Response.WriteAsJsonAsync(new{code,message,traceId=context.TraceIdentifier});
        else {context.Response.ContentType="text/html; charset=utf-8";await context.Response.WriteAsync($"<!doctype html><html lang='vi'><meta charset='utf-8'><title>Thông báo</title><h1>Không thể xử lý</h1><p>{System.Net.WebUtility.HtmlEncode(message)}</p><a href='/'>Danh mục điện thoại</a></html>");}
    }
});
if(!app.Environment.IsDevelopment()&&!isQa&&!app.Environment.IsEnvironment("Local")) {app.UseHsts();app.UseHttpsRedirection();}
app.UseStatusCodePages(async statusContext=>{
    var context=statusContext.HttpContext;
    if(!context.Request.Path.StartsWithSegments("/api"))return;
    var status=context.Response.StatusCode;
    var code=status switch{404=>"NOT_FOUND",405=>"METHOD_NOT_ALLOWED",429=>"RATE_LIMITED",_=>"REQUEST_FAILED"};
    var message=status switch{404=>"Không tìm thấy tài nguyên được yêu cầu.",405=>"Phương thức yêu cầu không được hỗ trợ.",429=>"Thao tác quá nhanh. Vui lòng chờ một phút và thử lại.",_=>"Không thể xử lý yêu cầu. Vui lòng thử lại."};
    await context.Response.WriteAsJsonAsync(new{code,message});
});
app.UseStaticFiles();app.UseRouting();app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();
app.MapGet("/health/ready",async (StoreDbContext db)=>{
    var connection=db.Database.GetDbConnection();
    var target=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes((connection.DataSource+"|"+connection.Database).ToLowerInvariant())));
    var ready=false;using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(3));
    try{await db.Accounts.Take(1).CountAsync(deadline.Token);ready=true;}catch(Exception){}
    return Results.Json(new{application="PhoneStore",environment=app.Environment.EnvironmentName,target,ready,code=ready?"READY":"DATABASE_UNAVAILABLE"},statusCode:ready?200:503);
});
if(isQa) app.MapGet("/api/qa/identity",(StoreDbContext db)=>Results.Json(new {environment="QA",server=db.Database.GetDbConnection().DataSource,database=db.Database.GetDbConnection().Database}));
app.MapControllers();app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}");
app.Run();






