namespace PhoneStore.Core.Commerce;
public sealed record DailyRevenue(DateOnly Day,int Orders,decimal Revenue);
public sealed record Bestseller(long VariantId,string Name,string Sku,long Quantity,decimal Revenue);
public sealed record StockReportLine(long VariantId,string Name,string Sku,int OnHand,int Reserved,int Available);
public sealed record StoreReport(string Kind,string TimeZone,string? From,string? To,decimal Revenue,long Quantity,int Orders,
    List<DailyRevenue> Daily,List<Bestseller> Bestsellers,List<StockReportLine> Stock);
