using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace PhoneStore.Web.Security;
public sealed class ApiAntiforgeryResultFilter : IAlwaysRunResultFilter, IOrderedFilter
{
    // Run before MVC's client-error mapping so CSRF failures keep a stable error code.
    public int Order => -3000;
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if(!context.HttpContext.Request.Path.StartsWithSegments("/api"))return;
        if(context.Result is AntiforgeryValidationFailedResult)
            context.Result = Error(400,"INVALID_CSRF","Phiên biểu mẫu đã hết hạn hoặc thiếu mã xác thực. Vui lòng tải lại trang và thử lại.");
        else if(context.Result is ObjectResult {Value:ProblemDetails problem})
            context.Result = ForStatus(problem.Status??500);
        else if(context.Result is StatusCodeResult {StatusCode:>=400} result)
            context.Result = ForStatus(result.StatusCode);
    }
    private static ObjectResult Error(int status,string code,string message)=>new(new{code,message}){StatusCode=status};
    private static ObjectResult ForStatus(int status)=>status switch {
        400=>Error(status,"INVALID_INPUT","Dữ liệu không hợp lệ. Vui lòng kiểm tra biểu mẫu."),
        404=>Error(status,"NOT_FOUND","Không tìm thấy tài nguyên được yêu cầu."),
        405=>Error(status,"METHOD_NOT_ALLOWED","Phương thức yêu cầu không được hỗ trợ."),
        415=>Error(status,"UNSUPPORTED_MEDIA_TYPE","Định dạng dữ liệu không được hỗ trợ. Vui lòng gửi JSON hợp lệ."),
        _=>Error(status,"REQUEST_FAILED","Không thể xử lý yêu cầu. Vui lòng thử lại.")};
    public void OnResultExecuted(ResultExecutedContext context) { }
}
