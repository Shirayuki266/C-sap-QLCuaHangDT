using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PhoneStore.Core;
using PhoneStore.Core.Commerce;

namespace PhoneStore.Web.Models;

public sealed record CatalogQuery : IValidatableObject
{
    [FromQuery(Name = "q"), StringLength(100, ErrorMessage = "Từ khóa tối đa 100 ký tự.")]
    public string? Q { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hãng hợp lệ.")]
    public int? BrandId { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "Giá từ phải là số tiền không âm hợp lệ.")]
    public decimal? MinPrice { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "Giá đến phải là số tiền không âm hợp lệ.")]
    public decimal? MaxPrice { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Dung lượng cần là số nguyên dương.")]
    public int? StorageGb { get; set; }
    [StringLength(50, ErrorMessage = "Màu sắc tối đa 50 ký tự.")]
    public string? Color { get; set; }
    public string Sort { get; set; } = "name";
    [Range(1, int.MaxValue, ErrorMessage = "Trang cần là số nguyên dương.")]
    public int Page { get; set; } = 1;
    [Range(1, 100, ErrorMessage = "Mỗi trang cần từ 1 đến 100 sản phẩm.")]
    public int PageSize { get; set; } = 12;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Sort is not ("name" or "newest" or "price-asc" or "price-desc"))
            yield return new("Vui lòng chọn cách sắp xếp trong danh sách.", [nameof(Sort)]);
        if (MinPrice > MaxPrice) yield return new("Giá đến cần lớn hơn hoặc bằng giá từ.", [nameof(MaxPrice)]);
        if (MinPrice.HasValue && decimal.Round(MinPrice.Value, 2) != MinPrice) yield return new("Giá từ tối đa 2 chữ số thập phân.", [nameof(MinPrice)]);
        if (MaxPrice.HasValue && decimal.Round(MaxPrice.Value, 2) != MaxPrice) yield return new("Giá đến tối đa 2 chữ số thập phân.", [nameof(MaxPrice)]);
        if (Page > 0 && PageSize > 0 && ((long)Page - 1) * PageSize > int.MaxValue)
            yield return new("Số trang vượt giới hạn tra cứu. Vui lòng chọn trang nhỏ hơn.", [nameof(Page)]);
    }
    public CatalogFilter ToFilter() => new(Q, BrandId, MinPrice, MaxPrice, StorageGb, Color, Sort, Page, PageSize);
    public string PageUrl(int page = 1, string? remove = null)
    {
        var copy = remove switch
        {
            "brand" => this with { BrandId = null }, "price" => this with { MinPrice = null, MaxPrice = null },
            "storage" => this with { StorageGb = null }, "color" => this with { Color = null }, "query" => this with { Q = null },
            "filters" => this with { BrandId = null, MinPrice = null, MaxPrice = null, StorageGb = null, Color = null }, _ => this
        };
        var values = new Dictionary<string, string?>
        {
            ["q"] = copy.Q, ["brandId"] = copy.BrandId?.ToString(CultureInfo.InvariantCulture),
            ["minPrice"] = copy.MinPrice?.ToString(CultureInfo.InvariantCulture), ["maxPrice"] = copy.MaxPrice?.ToString(CultureInfo.InvariantCulture),
            ["storageGb"] = copy.StorageGb?.ToString(CultureInfo.InvariantCulture), ["color"] = copy.Color,
            ["sort"] = copy.Sort, ["page"] = page.ToString(CultureInfo.InvariantCulture), ["pageSize"] = copy.PageSize.ToString(CultureInfo.InvariantCulture)
        };
        return QueryHelpers.AddQueryString("/Catalog/Index", values.Where(pair => !string.IsNullOrEmpty(pair.Value)));
    }
}

public sealed record CatalogIndexViewModel(CatalogQuery Filter, List<CatalogBrand> Brands, Page<PublicProduct>? Results)
{
    public int TotalPages => Results is null ? 0 : (int)(((long)Results.Total + Results.PageSize - 1) / Results.PageSize);
}
public sealed record ProductCardViewModel(long Id, string Name, string? Brand, string ImageUrl, decimal Price, string? VariantLabel, bool? InStock)
{
    // Choose a display row from the service's already-filtered public variants; never calculate a selling price.
    public static ProductCardViewModel From(PublicProduct product)
    {
        var display = product.Variants.OrderBy(variant => variant.Price).ThenBy(variant => variant.Id).First();
        var image = string.IsNullOrWhiteSpace(product.ImageUrl) ? product.Images.FirstOrDefault()?.Url ?? "" : product.ImageUrl;
        return new(product.Id, product.Name, product.Brand, image, display.Price,
            $"{display.StorageGb} GB · {display.Color}", product.Variants.Any(variant => variant.Available > 0));
    }
}
public sealed record ProductDetailsViewModel(PublicProduct Product, PublicVariant Selected);
public sealed record HomeViewModel(PublicBestsellers Bestsellers);
