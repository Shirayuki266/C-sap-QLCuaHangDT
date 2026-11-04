using System.Globalization;
namespace PhoneStore.Infrastructure.Commerce;
public sealed record DateWindow(DateTime? Start,DateTime? End)
{
    public static DateWindow Parse(string? from,string? to)
    {
        DateOnly? a=Date(from),b=Date(to);
        if(a>b)throw Rules.Error("INVALID_INPUT","Ngày bắt đầu không được sau ngày kết thúc.");
        try{return new(a.HasValue?DateTime.SpecifyKind(a.Value.ToDateTime(TimeOnly.MinValue).AddHours(-7),DateTimeKind.Utc):null,
            b.HasValue?DateTime.SpecifyKind(b.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7),DateTimeKind.Utc):null);}
        catch(ArgumentOutOfRangeException){throw Rules.Error("INVALID_INPUT","Khoảng ngày vượt giới hạn hỗ trợ.");}
    }
    private static DateOnly? Date(string? value)
    {
        if(string.IsNullOrEmpty(value))return null;
        if(!DateOnly.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var result))throw Rules.Error("INVALID_INPUT","Ngày cần đúng định dạng yyyy-MM-dd.");
        return result;
    }
}
