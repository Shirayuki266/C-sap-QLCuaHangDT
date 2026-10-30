using Microsoft.AspNetCore.Antiforgery;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;
using System.Text.Json.Serialization;

namespace PhoneStore.Web;

public sealed record ApiError(string Code, string Message, string TraceId, [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] object? Details=null)
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

    public static ApiError From(Exception exception, string traceId) => exception switch
    {
        PriceChangedException changed => new("PRICE_CHANGED", changed.Message, traceId, changed.Cart),
        BusinessException business => new(business.Code, business.Message, traceId),
        AntiforgeryValidationException => new("INVALID_INPUT", "Phiên xác thực biểu mẫu không hợp lệ. Vui lòng tải lại và thử lại.", traceId),
        BadHttpRequestException { StatusCode: 400 } => new("INVALID_INPUT", "Dữ liệu yêu cầu không hợp lệ. Vui lòng kiểm tra và thử lại.", traceId),
        _ => new("SERVER_ERROR", "Không thể xử lý yêu cầu. Vui lòng thử lại sau.", traceId)
    };
}
