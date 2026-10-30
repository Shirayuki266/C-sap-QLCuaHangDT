using System.Text.Json.Serialization;
namespace PhoneStore.Core.Commerce;
public sealed record SupplierInput(string? Code, string? Name, string? Phone, string? Address, [property: JsonRequired] bool Active = true);
public sealed record SupplierView(long Id, string Code, string Name, string Phone, string Address, bool Active);
