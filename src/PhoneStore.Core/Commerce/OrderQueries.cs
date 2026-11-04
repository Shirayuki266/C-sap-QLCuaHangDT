namespace PhoneStore.Core.Commerce;
public sealed record OrderFilter(string? Query=null,string? Status=null,string? From=null,string? To=null,long? CustomerId=null,int Page=1,int PageSize=20);
