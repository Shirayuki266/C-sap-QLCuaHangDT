using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;

namespace PhoneStore.Web.Storefront;

public sealed record WebsiteError(int Status, string Title, string Message, string? TraceId = null);

public sealed class StorefrontErrors(RequestDelegate next, ILogger<StorefrontErrors> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!StorefrontHosting.IsWebsite(context)) { await next(context); return; }
        if (StorefrontHosting.IsPersonal(context)) context.Response.Headers.CacheControl = "no-store";
        try
        {
            await next(context);
            if (context.Response.StatusCode >= 400 && !context.Response.HasStarted &&
                context.Response.ContentLength is null or 0)
                await Render(context, ForStatus(context.Response.StatusCode));
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            if (exception is BusinessException { Code: "UNAUTHORIZED" or "UNAUTHENTICATED" })
            {
                context.Response.Clear();
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Redirect("/Account/Login?returnUrl=" + Uri.EscapeDataString(context.Request.GetEncodedPathAndQuery()));
                return;
            }
            if (exception is BusinessException business)
            {
                var status = business.Code switch
                {
                    "FORBIDDEN" => 403, "NOT_FOUND" => 404,
                    "CONCURRENT_UPDATE" or "STALE_VERSION" or "INVALID_STATE" or "ALREADY_APPLIED" or
                    "PRICE_CHANGED" or "OUT_OF_STOCK" or "INSUFFICIENT_STOCK" or "VARIANT_INACTIVE" or
                    "CART_EMPTY" or "REQUEST_KEY_CONFLICT" => 409,
                    _ => 400
                };
                await Render(context, ForStatus(status));
                return;
            }
            if (exception is AntiforgeryValidationException or BadHttpRequestException)
            {
                await Render(context, ForStatus(400)); return;
            }
            if (exception is PriceChangedException) { await Render(context, ForStatus(409)); return; }
            logger.LogError(exception, "Website request failed: {TraceId}", context.TraceIdentifier);
            await Render(context, ForStatus(500) with { TraceId = context.TraceIdentifier });
        }
    }

    private static WebsiteError ForStatus(int status) => status switch
    {
        403 => new(403, "Bạn chưa có quyền truy cập", "Vui lòng sử dụng tài khoản khách hàng để tiếp tục."),
        404 => new(404, "Không tìm thấy trang", "Trang bạn tìm có thể đã thay đổi hoặc không còn khả dụng."),
        400 => new(400, "Vui lòng kiểm tra yêu cầu", "Dữ liệu hoặc phiên biểu mẫu chưa hợp lệ. Hãy tải lại trang và thử lại."),
        405 => new(405, "Không thể thực hiện yêu cầu", "Hãy quay lại trang trước và sử dụng thao tác trên biểu mẫu."),
        409 => new(409, "Thông tin đã thay đổi", "Vui lòng tải lại trang để xem thông tin mới nhất rồi thử lại."),
        _ => new(status, "Chưa thể tải trang", "Vui lòng thử lại sau. Nếu sự cố tiếp diễn, hãy liên hệ cửa hàng.")
    };

    private static async Task Render(HttpContext context, WebsiteError error)
    {
        context.Response.Clear();
        context.Response.StatusCode = error.Status;
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        var data = new ViewDataDictionary<WebsiteError>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = error };
        var result = new ViewResult { ViewName = "/Views/Shared/WebsiteError.cshtml", ViewData = data, StatusCode = error.Status };
        var action = new ActionContext(context, context.GetRouteData(), new ActionDescriptor());
        await context.RequestServices.GetRequiredService<IActionResultExecutor<ViewResult>>().ExecuteAsync(action, result);
    }
}
