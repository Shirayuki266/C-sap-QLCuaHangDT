namespace PhoneStore.Core;

// Ngoại lệ nghiệp vụ chuẩn mang mã lỗi (Code) để tầng Web/Admin ánh xạ sang HTTP Status và thông báo tiếng Việt.
public sealed class BusinessException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
