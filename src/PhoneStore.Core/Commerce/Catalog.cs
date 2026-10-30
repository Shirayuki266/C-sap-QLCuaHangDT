namespace PhoneStore.Core.Commerce;
public sealed record CatalogFilter(string? Query = null, int? BrandId = null, decimal? MinPrice = null, decimal? MaxPrice = null,
    int? StorageGb = null, string? Color = null, string Sort = "name", int Page = 1, int PageSize = 20);
public sealed record PublicVariant(long Id, string Sku, string Color, int StorageGb, decimal Price, int Available);
public sealed record PublicProduct(long Id, string Code, string Name, string Brand, string Category, string Specifications,
    string Description, string ImageUrl, List<PublicVariant> Variants, List<ImageInput> Images);
