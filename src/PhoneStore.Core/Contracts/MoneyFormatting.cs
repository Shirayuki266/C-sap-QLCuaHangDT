using System.Globalization;
namespace PhoneStore.Core.Contracts;
public static class MoneyFormatting
{
    public static string FormatMoney(this decimal value) => value.ToString(value == decimal.Truncate(value) ? "N0" : "N2", CultureInfo.GetCultureInfo("vi-VN"));
}
