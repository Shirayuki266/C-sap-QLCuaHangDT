using System.Globalization;

namespace PhoneStore.Web.Storefront;

public static class WebsiteFormatting
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    public static string Money(decimal value) => value.ToString(value == decimal.Truncate(value) ? "N0" : "N2", Vietnamese) + " đ";
    public static string Time(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc))
        .ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", Vietnamese);
}
