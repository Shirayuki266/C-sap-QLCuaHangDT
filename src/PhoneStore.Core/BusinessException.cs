namespace PhoneStore.Core;

public sealed class BusinessException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
