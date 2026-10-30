using System.Text.Json.Serialization;
namespace PhoneStore.Core.Commerce;

public sealed record ImageInput(string? Url, int SortOrder = 0);
public sealed record VariantInput(long Id, string? Sku, string? Color, int StorageGb, decimal Cost,
    [property: JsonRequired] decimal ProfitPercent, bool Active = true, string? Version = null);
public sealed record ProductInput(string? Code, string? Name, int BrandId, int CategoryId, string? Specifications,
    string? Description, string? ImageUrl, bool Active, List<VariantInput>? Variants, List<ImageInput>? Images = null);
public sealed record AdminVariant(long Id, string Sku, string Color, int StorageGb, decimal Cost, decimal ProfitPercent,
    decimal Price, bool Active, int OnHand, int Reserved, string Version);
public sealed record AdminProduct(long Id, string Code, string Name, int BrandId, int CategoryId, string Specifications,
    string Description, string ImageUrl, bool Active, List<AdminVariant> Variants, List<ImageInput> Images);
