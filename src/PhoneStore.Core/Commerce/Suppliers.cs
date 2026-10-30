namespace PhoneStore.Core.Commerce;
public sealed record SupplierInput(string? Code, string? Name, string? Phone, string? Address, bool Active = true);
public sealed record SupplierView(long Id, string Code, string Name, string Phone, string Address, bool Active);
