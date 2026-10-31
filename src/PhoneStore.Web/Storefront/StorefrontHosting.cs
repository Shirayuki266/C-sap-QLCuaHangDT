using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PhoneStore.Core;
using PhoneStore.Web.Security;

namespace PhoneStore.Web.Storefront;

public static class StorefrontHosting
{
    public static IServiceCollection AddStorefrontMvc(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IStoreService, MvcStoreAdapter>();
        services.AddScoped<StorefrontCookieEvents>();
        services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,
            options => options.EventsType = typeof(StorefrontCookieEvents));
        services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        services.Configure<StorefrontOptions>(configuration.GetSection("Storefront"));
        return services;
    }

    public static IApplicationBuilder UseStorefrontMvc(this IApplicationBuilder app) => app.UseMiddleware<StorefrontErrors>();

    public static bool IsWebsite(HttpContext context) =>
        !context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/health");

    public static bool IsPersonal(HttpContext context) => new[] { "/Account", "/Cart", "/Orders" }
        .Any(prefix => context.Request.Path.StartsWithSegments(prefix));
}

public sealed class StorefrontOptions
{
    public string Name { get; set; } = "Monophone";
    public string Introduction { get; set; } = "Chọn chiếc điện thoại vừa ý. Đồng hành cùng mọi khoảnh khắc.";
    public string ContactNote { get; set; } = "Thông tin cửa hàng đang được cập nhật.";
}

public sealed class StorefrontEntryPoint { }
