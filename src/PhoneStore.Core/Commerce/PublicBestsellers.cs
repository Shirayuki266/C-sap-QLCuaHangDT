namespace PhoneStore.Core.Commerce;
public sealed record PublicBestseller(long ProductId,string Name,string ImageUrl,decimal MinPrice,long QuantitySold);
public sealed record PublicBestsellers(DateOnly From,DateOnly To,string TimeZone,List<PublicBestseller> Items);
