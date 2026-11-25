namespace PhoneStore.Core.Commerce;
// [UC04.01, UC09.01] Bộ lọc tra cứu đơn hàng theo từ khóa, trạng thái, khoảng ngày UTC+7 và khách hàng.
public sealed record OrderFilter(string? Query=null,string? Status=null,string? From=null,string? To=null,long? CustomerId=null,int Page=1,int PageSize=20);
