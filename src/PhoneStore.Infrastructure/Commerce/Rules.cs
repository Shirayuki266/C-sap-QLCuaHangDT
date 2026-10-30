using System.Text.RegularExpressions;
using PhoneStore.Core;

namespace PhoneStore.Infrastructure.Commerce;

public static class Rules
{
    public const decimal MaxMoney = 9999999999999999.99m;
    public static BusinessException Error(string code, string text) => new(code, text);
    public static string Text(string? input, int max, string label, bool optional = false)
    {
        var value = input?.Trim() ?? "";
        if ((!optional && value.Length == 0) || value.Length > max) throw Error("INVALID_INPUT", $"{label} không hợp lệ (tối đa {max} ký tự).");
        return value;
    }
    public static string Phone(string? input)
    {
        var value = Text(input, 20, "Số điện thoại");
        if (!Regex.IsMatch(value, @"\A\+?[0-9]{9,15}\z", RegexOptions.NonBacktracking)) throw Error("INVALID_INPUT", "Số điện thoại cần 9–15 chữ số.");
        return value;
    }
    public static decimal Money(decimal value, bool zero = false)
    {
        if (value < 0 || (!zero && value == 0) || value > MaxMoney || decimal.Round(value, 2) != value) throw Error("INVALID_INPUT", "Số tiền phải hợp lệ, tối đa 2 chữ số thập phân và nằm trong giới hạn lưu trữ.");
        return value;
    }
    public static decimal Price(decimal cost, decimal percent)
    {
        Money(cost);
        if (percent < 0 || percent > 10000 || decimal.Round(percent, 2) != percent) throw Error("INVALID_INPUT", "Tỷ lệ cần nằm trong 0–10.000%, tối đa 2 chữ số thập phân.");
        return Money(decimal.Round(cost * (1 + percent / 100m), 2, MidpointRounding.AwayFromZero));
    }
    public static void Quantity(int value) { if (value <= 0) throw Error("INVALID_INPUT", "Số lượng phải là số nguyên dương."); }
    public static void Key(Guid value) { if (value == Guid.Empty) throw Error("INVALID_INPUT", "Thiếu khóa yêu cầu."); }
    public static void Version(byte[] actual, string? expected)
    {
        byte[] decoded;
        try { decoded = Convert.FromBase64String(expected ?? ""); }
        catch (FormatException) { throw Error("INVALID_INPUT", "Phiên bản dữ liệu không hợp lệ."); }
        if (decoded.Length != 8) throw Error("INVALID_INPUT", "Cần phiên bản dữ liệu hợp lệ.");
        if (!actual.SequenceEqual(decoded)) throw Error("CONCURRENT_UPDATE", "Dữ liệu đã thay đổi. Vui lòng tải lại.");
    }
    public static int Offset(int page, int size)
    {
        var offset = ((long)page - 1) * size;
        if (page < 1 || size < 1 || size > 100 || offset > int.MaxValue) throw Error("INVALID_INPUT", "Phân trang không hợp lệ (1–100 dòng mỗi trang).");
        return (int)offset;
    }
    public static string Image(string? value, bool optional = true)
    {
        var url = Text(value, 1000, "Đường dẫn ảnh", optional);
        if (url.Length == 0) return url;
        if (Uri.TryCreate(url, UriKind.Absolute, out var parsed) && parsed.Scheme == "https" && parsed.UserInfo.Length == 0) return parsed.AbsoluteUri;
        if (url.StartsWith("/images/", StringComparison.Ordinal) && !url.Contains("..", StringComparison.Ordinal) && !url.Contains('\\') && !url.Any(char.IsControl)) return url;
        throw Error("INVALID_INPUT", "Ảnh phải là URL HTTPS hoặc tài nguyên /images/ hợp lệ.");
    }
}
