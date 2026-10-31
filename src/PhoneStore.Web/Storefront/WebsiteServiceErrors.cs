using PhoneStore.Core;

namespace PhoneStore.Web.Storefront;

// MVC transport aliases only. Business decisions remain in the existing services.
public static class WebsiteServiceErrors
{
    public static bool CartIssue(string code) => code is "OUT_OF_STOCK" or "INSUFFICIENT_STOCK" or "VARIANT_INACTIVE" or "CART_EMPTY";
    public static int Status(string code) => code switch
    {
        "UNAUTHORIZED" or "UNAUTHENTICATED" => 401,
        "FORBIDDEN" => 403, "NOT_FOUND" => 404,
        "CONCURRENT_UPDATE" or "STALE_VERSION" or "INVALID_STATE" or "ALREADY_APPLIED" or "PRICE_CHANGED" or
        "OUT_OF_STOCK" or "INSUFFICIENT_STOCK" or "VARIANT_INACTIVE" or "CART_EMPTY" or "REQUEST_KEY_CONFLICT" => 409,
        "DATABASE_UNAVAILABLE" => 503,
        "INTERNAL_ERROR" or "SERVER_ERROR" => 500,
        _ => 400
    };
    public static object Json(BusinessException error, string trace) => Status(error.Code) >= 500
        ? new { code = error.Code, message = "Chưa thể xử lý yêu cầu. Vui lòng thử lại sau.", traceId = trace }
        : new { code = error.Code, message = error.Message, traceId = trace };
}
