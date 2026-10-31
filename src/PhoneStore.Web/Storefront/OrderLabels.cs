namespace PhoneStore.Web.Storefront;

public static class OrderLabels
{
    public static string Status(string value) => value switch
    {
        "CHO_XAC_NHAN" => "Chờ xác nhận", "DA_XAC_NHAN" => "Đã xác nhận", "DANG_GIAO" => "Đang giao",
        "HOAN_TAT" => "Hoàn tất", "DA_HUY" => "Đã hủy", "GIAO_THAT_BAI" => "Giao thất bại", _ => value
    };
    public static string Payment(string value) => value switch { "CHUA_THU" => "Chưa thu tiền", "DA_THU" => "Đã thu tiền", _ => value };
}
