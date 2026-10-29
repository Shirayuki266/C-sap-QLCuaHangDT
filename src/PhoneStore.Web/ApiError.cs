using PhoneStore.Core;

namespace PhoneStore.Web;

public sealed record ApiError(string Code, string Message, string TraceId)
{
    public static int Status(string code) => code switch
    {
        "INVALID_INPUT" => 400,
        "UNAUTHORIZED" => 401,
        "FORBIDDEN" => 403,
        "NOT_FOUND" => 404,
        "METHOD_NOT_ALLOWED" => 405,
        "CONCURRENT_UPDATE" or "INVALID_STATE" or "OUT_OF_STOCK" or "PRICE_CHANGED" => 409,
        "DATABASE_UNAVAILABLE" => 503,
        _ => 500
    };

    public static ApiError From(Exception exception, string traceId) => exception is BusinessException business
        ? new(business.Code, business.Message, traceId)
        : new("SERVER_ERROR", "Không thể xử lý yêu cầu. Vui lòng thử lại sau.", traceId);
}
