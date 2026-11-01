namespace PhoneStore.Admin.Client;

public sealed class AdminApiException(string code, int status = 0, string? traceId = null) : Exception(MessageFor(code, status, traceId))
{
    public string Code { get; } = code;
    public int Status { get; } = status;
    public string? TraceId { get; } = SafeTrace(traceId);
    private static string? SafeTrace(string? value) => value is { Length: > 0 and <= 120 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or ':' or '.') ? value : null;
    private static string MessageFor(string code, int status, string? trace) => status >= 500 && status != 503
        ? "Máy chủ chưa thể xử lý yêu cầu. Mã tra cứu: " + (SafeTrace(trace) ?? "không có")
        : code switch
        {
            "UNAUTHORIZED" => "Phiên đăng nhập không còn hiệu lực. Vui lòng đăng nhập lại.",
            "LOGIN_FAILED" => "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản chưa được phép đăng nhập.",
            "INVALID_INPUT" => "Dữ liệu chưa hợp lệ. Vui lòng kiểm tra các ô đã nhập.",
            "FORBIDDEN" => "Bạn không có quyền thực hiện thao tác này.",
            "NOT_FOUND" => "Không tìm thấy dữ liệu. Vui lòng tải lại danh sách.",
            "CONCURRENT_UPDATE" => "Dữ liệu đã thay đổi, vui lòng tải lại.",
            "INVALID_STATE" => "Trạng thái đã thay đổi. Vui lòng tải lại và kiểm tra kết quả trước khi thao tác tiếp.",
            "OUT_OF_STOCK" => "Không đủ hàng khả dụng. Vui lòng tải lại dữ liệu.",
            "DATABASE_UNAVAILABLE" => "Cơ sở dữ liệu chưa sẵn sàng. Vui lòng thử lại sau.",
            "INVALID_SERVER" => "Địa chỉ máy chủ chưa hợp lệ. HTTP chỉ được dùng trên máy cục bộ; máy khác phải dùng HTTPS.",
            "SERVER_MISMATCH" => "Môi trường hoặc dấu nhận dạng máy chủ không khớp cấu hình. Đăng nhập đã bị chặn.",
            "INVALID_RESPONSE" => "Phản hồi máy chủ chưa hợp lệ. Vui lòng tải lại hoặc liên hệ quản trị.",
            "UNCERTAIN" => "Chưa xác định yêu cầu đã được xử lý hay chưa. Hãy đối soát dữ liệu trước khi gửi lệnh tiếp theo.",
            "NETWORK" => "Không kết nối được máy chủ. Kiểm tra địa chỉ và kết nối rồi thử lại.",
            "TIMEOUT" => "Máy chủ phản hồi quá lâu. Vui lòng thử tải lại.",
            _ => "Không thể thực hiện yêu cầu. Vui lòng kiểm tra và thử lại."
        };
}

public static class ServerAddress
{
    public static Uri Validate(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/" ||
            (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            throw new AdminApiException("INVALID_SERVER");
        return uri;
    }
}
