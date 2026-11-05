namespace PhoneStore.Web.Storefront;

/// <summary>Presentation assets for the fixed demo catalog; persisted product data stays authoritative.</summary>
public static class StorefrontProductImages
{
    private static readonly IReadOnlyDictionary<string, string> Models = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Samsung Galaxy A05"] = "lc001", ["Samsung Galaxy A05s"] = "lc002",
        ["Samsung Galaxy A06"] = "lc003", ["Samsung Galaxy A14"] = "lc004",
        ["Samsung Galaxy A15"] = "lc005", ["Redmi 12"] = "lc006",
        ["Redmi 13"] = "lc007", ["Redmi 10C"] = "lc008", ["Redmi 14C"] = "lc009",
        ["realme C11 (2021)"] = "lc010", ["realme C21Y"] = "lc011", ["realme C35"] = "lc012",
        ["OPPO A18"] = "lc013", ["OPPO A38"] = "lc014", ["Samsung Galaxy A24"] = "lc015",
        ["Samsung Galaxy A25 5G"] = "lc016", ["Samsung Galaxy A34 5G"] = "lc017",
        ["Samsung Galaxy A54 5G"] = "lc018", ["Redmi Note 12"] = "lc019",
        ["Redmi Note 12 Pro (4G)"] = "lc020", ["Redmi Note 13 5G"] = "lc021",
        ["Redmi Note 13 Pro+ 5G"] = "lc022", ["Xiaomi 13 Lite"] = "lc023",
        ["Redmi Note 10 Pro"] = "lc024", ["Xiaomi Mi 11 Lite (4G)"] = "lc025",
        ["OPPO A78"] = "lc026", ["OPPO Reno9 Pro+ 5G"] = "lc027", ["vivo S18"] = "lc028",
        ["vivo X100"] = "lc029", ["HONOR X9b"] = "lc030", ["Apple iPhone 13"] = "lc031",
        ["Apple iPhone 14"] = "lc032", ["Apple iPhone 15"] = "lc033",
        ["Apple iPhone 15 Plus"] = "lc034", ["Apple iPhone 15 Pro"] = "lc035",
        ["Apple iPhone 15 Pro Max"] = "lc036", ["Samsung Galaxy S23"] = "lc037",
        ["Samsung Galaxy S23 Ultra"] = "lc038", ["Samsung Galaxy S24"] = "lc039",
        ["Samsung Galaxy S24 Ultra"] = "lc040", ["Xiaomi 13T Pro"] = "lc041",
        ["OnePlus 9 Pro"] = "lc042", ["ASUS ROG Phone 5 (Tencent edition)"] = "lc043",
        ["ASUS ROG Phone II"] = "lc044", ["ASUS ROG Phone 8"] = "lc045",
        ["nubia Z80 Ultra"] = "lc046", ["Samsung Galaxy Z Flip5"] = "lc047",
        ["Samsung Galaxy Z Fold5"] = "lc048", ["OPPO Find N2"] = "lc049",
        ["OPPO Find N3 Flip"] = "lc050"
    };

    public static string Resolve(string url, string productName)
    {
        if (!Models.TryGetValue(productName, out var code)) return url;
        // Match both the current model name and the original catalog path. Custom uploads,
        // missing images and similarly named products must retain their own image/fallback.
        for (var view = 1; view <= 3; view++)
            if (url == $"/images/demo/{code}-{view}.svg")
                return $"/images/storefront/{code}.svg";
        return url;
    }
}
