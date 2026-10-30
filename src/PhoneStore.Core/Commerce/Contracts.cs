using System.Text.Json.Serialization;
namespace PhoneStore.Core.Commerce;

public sealed record Actor(long AccountId, Guid SessionId);
public sealed record Page<T>(List<T> Items, int Total, [property: JsonPropertyName("page")] int PageNumber, int PageSize);
public sealed record VersionedCommand(string? Version, Guid OperationKey, string? Reason = null);
