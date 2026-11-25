namespace PhoneStore.Core.Commerce;
// [UC02.01] DTO danh sách điện thoại bán chạy trong tháng hiển thị trên Trang chủ Website.
public sealed record PublicBestseller(long ProductId,string Name,string ImageUrl,decimal MinPrice,long QuantitySold);
public sealed record PublicBestsellers(DateOnly From,DateOnly To,string TimeZone,List<PublicBestseller> Items);
