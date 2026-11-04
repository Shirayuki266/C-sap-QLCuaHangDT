using System.Text.Json.Serialization;
namespace PhoneStore.Core.Commerce;
public sealed record AdminCustomer(long Id,string Username,string FullName,string Phone,string? Email,string Address,bool Active,DateTimeOffset CreatedAt);
public sealed record CustomerStateInput([property:JsonRequired] bool Active,[property:JsonRequired] bool ExpectedActive);
