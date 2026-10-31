using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core;
using PhoneStore.Core.Accounts;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Web;
using PhoneStore.Web.Security;
using PhoneStore.Web.Endpoints;
using PhoneStore.Web.Storefront;

var commandModes = new[]{"--migrate","--provision-admin","--seed-demo","--audit-db"};
var modes = args.Where(commandModes.Contains).ToArray();
if(modes.Length>1)throw new ArgumentException("Chỉ chọn một lệnh quản trị mỗi lần.");
var migrate = modes.Contains("--migrate");
var builder = WebApplication.CreateBuilder(args.Where(argument => !commandModes.Contains(argument)).ToArray());
var connection = SqlTarget.Validate(builder.Configuration.GetConnectionString("Store"));
var backendFingerprint = BackendIdentity.Fingerprint(connection,builder.Environment.EnvironmentName);
builder.Services.AddDbContext<StoreDbContext>(options => options.UseSqlServer(connection));
builder.Services.AddScoped<DatabaseMigrator>();
builder.Services.AddScoped<BusinessKernel>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<CustomerAdminService>();

builder.Services.AddScoped<OrderWorkflow>();
builder.Services.AddScoped<OrderQueryService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<PublicBestsellerService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<PurchaseService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddScoped<IAccountRegistration, AccountRegistration>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<IAccountSessions, AccountSessions>();
builder.Services.AddScoped<IAccountPasswords, AccountPasswords>();
builder.Services.AddScoped<IAccountProfiles, AccountProfiles>();
builder.Services.AddScoped<SessionAuthentication>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "PhoneStore.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = SessionPolicy.Lifetime;
    options.SlidingExpiration = false;
    options.EventsType = typeof(SessionAuthentication);
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Customer", policy => policy.RequireRole("CUSTOMER"));
    options.AddPolicy("Admin", policy => policy.RequireRole("ADMIN"));
});
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "PhoneStore.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
if (builder.Environment.IsEnvironment("Testing")) builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddStorefrontMvc(builder.Configuration);
var app = builder.Build();

if (modes.Length==1 && !migrate)
{
    await BootstrapCommand.Run(modes[0],app.Services);
    return;
}

if (migrate)
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync();
    Console.WriteLine("Đã áp dụng và kiểm tra migration của bản xây lại.");
    return;
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    try { await next(context); }
    catch (Exception exception) when (!context.Response.HasStarted)
    {
        var error = ApiError.From(exception, context.TraceIdentifier);
        if (error.Code == "SERVER_ERROR")
            app.Logger.LogError(exception, "Request failed: {TraceId}", context.TraceIdentifier);
        context.Response.Clear();
        context.Response.StatusCode = ApiError.Status(error.Code);
        await context.Response.WriteAsJsonAsync(error);
    }
});
app.UseStatusCodePages(async status =>
{
    var context = status.HttpContext;
    var (code, message) = context.Response.StatusCode switch
    {
        400 => ("INVALID_INPUT", "Dữ liệu yêu cầu không hợp lệ. Vui lòng kiểm tra và thử lại."),
        404 => ("NOT_FOUND", "Không tìm thấy tài nguyên được yêu cầu."),
        405 => ("METHOD_NOT_ALLOWED", "Phương thức yêu cầu không được hỗ trợ."),
        _ => ("REQUEST_FAILED", "Không thể xử lý yêu cầu.")
    };
    await context.Response.WriteAsJsonAsync(new ApiError(code, message, context.TraceIdentifier));
});

app.UseStaticFiles();
app.UseStorefrontMvc();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { application = "PhoneStore", status = "live" }));
app.MapPost("/api/auth/login", async (LoginRequest request, IAccountSessions sessions, IAntiforgery antiforgery, HttpContext context, CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);
    if (context.User.Identity?.IsAuthenticated == true)
        throw new BusinessException("INVALID_STATE", "Vui lòng đăng xuất trước khi đăng nhập tài khoản khác.");
    var session = await sessions.LoginAsync(request, cancellationToken);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, SessionAuthentication.Principal(session), new AuthenticationProperties
    {
        IssuedUtc = session.ExpiresAt - SessionPolicy.Lifetime,
        ExpiresUtc = session.ExpiresAt,
        IsPersistent = false,
        AllowRefresh = false
    });
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { session.Account.Id, session.Account.Username, session.Account.Role, session.ExpiresAt });
});
app.MapGet("/api/auth/session", (HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    var session = SessionAuthentication.Current(context);
    return Results.Ok(new { session.Account.Id, session.Account.Username, session.Account.Role, session.ExpiresAt });
}).RequireAuthorization();
app.MapPost("/api/auth/logout", async (IAccountSessions sessions, IAntiforgery antiforgery, HttpContext context, CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var current = SessionAuthentication.Current(context);
    await sessions.RevokeAsync(current.Account.Id, current.SessionId, cancellationToken);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Headers.CacheControl = "no-store";
    return Results.NoContent();
}).RequireAuthorization();
app.MapPost("/api/account/password", async (PasswordChangeRequest request, IAccountPasswords passwords,
    IAntiforgery antiforgery, HttpContext context, CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var current = SessionAuthentication.Current(context);
    await passwords.ChangeAsync(current.Account.Id, current.SessionId, request, cancellationToken);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Headers.CacheControl = "no-store";
    return Results.NoContent();
}).RequireAuthorization();
app.MapGet("/api/admin/session", (HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(SessionAuthentication.Current(context).Account);
}).RequireAuthorization("Admin");
app.MapGet("/api/account/profile", async (IAccountProfiles profiles, HttpContext context, CancellationToken cancellationToken) =>
{
    var current = SessionAuthentication.Current(context);
    var profile = await profiles.ReadAsync(current.Account.Id, current.SessionId, cancellationToken);
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(profile);
}).RequireAuthorization("Customer");
app.MapPut("/api/account/profile", async (ProfileChangeRequest request, IAccountProfiles profiles,
    IAntiforgery antiforgery, HttpContext context, CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var current = SessionAuthentication.Current(context);
    await profiles.UpdateAsync(current.Account.Id, current.SessionId, request, cancellationToken);
    context.Response.Headers.CacheControl = "no-store";
    return Results.NoContent();
}).RequireAuthorization("Customer");
app.MapGet("/api/security/token", (IAntiforgery antiforgery, HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken });
});
app.MapPost("/api/auth/register", async (RegistrationRequest request, IAccountRegistration registrations,
    IAntiforgery antiforgery, HttpContext context, CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);
    if (context.User.Identity?.IsAuthenticated == true)
        throw new BusinessException("INVALID_STATE", "Vui lòng đăng xuất trước khi đăng ký tài khoản mới.");
    var result = await registrations.RegisterAsync(request, cancellationToken);
    context.Response.Headers.CacheControl = "no-store";
    return Results.Json(result, statusCode: 201);
});
app.MapGet("/health/ready", async (DatabaseMigrator migrations, HttpContext context) =>
{
    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
    deadline.CancelAfter(TimeSpan.FromSeconds(3));
    try
    {
        if (await migrations.IsCurrentAsync(deadline.Token)) return Results.Ok(new { status = "ready", environment = app.Environment.EnvironmentName, fingerprint = backendFingerprint });
    }
    catch (Exception exception) when (exception is not OutOfMemoryException)
    {
        app.Logger.LogWarning("Database readiness check failed: {TraceId}", context.TraceIdentifier);
    }
    return Results.Json(new ApiError("DATABASE_UNAVAILABLE", "CSDL chưa sẵn sàng hoặc chưa được tạo cấu trúc.", context.TraceIdentifier), statusCode: 503);
});

app.MapLookups();
app.MapProducts();
app.MapCatalog();
app.MapSuppliers();
app.MapPurchases();
app.MapInventory();
app.MapCarts();
app.MapPublicBestsellers();
app.MapCheckout();
app.MapOrderQueries();
app.MapOrderDecisions();
app.MapOrderFulfillment();
app.MapCustomerAdmin();
app.MapInvoices();
app.MapReports();
app.MapControllers();
app.Run();
// Endpoint modules call the same SQL-backed services as future MVC/WinForms clients.
