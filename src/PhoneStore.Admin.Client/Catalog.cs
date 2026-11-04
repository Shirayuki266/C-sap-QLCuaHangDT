namespace PhoneStore.Admin.Client;

public sealed record LookupRow(int Id, string Name, bool Active)
{
    public override string ToString() => Name + (Active ? "" : " · Đã ngừng");
}
public sealed record LookupRequest(string Name, bool Active);
public sealed record ProductImage(string Url, int SortOrder = 0);
public sealed record ProductVariant(long Id, string Sku, string Color, int StorageGb, decimal Cost, decimal ProfitPercent,
    decimal Price, bool Active, int OnHand, int Reserved, string Version)
{
    public int Available => OnHand - Reserved; // Authorized ADMIN presentation, never a write input.
}
public sealed record ProductRecord(long Id, string Code, string Name, int BrandId, int CategoryId, string Specifications,
    string Description, string ImageUrl, bool Active, List<ProductVariant> Variants, List<ProductImage> Images);
public sealed record VariantRequest(long Id, string Sku, string Color, int StorageGb, decimal Cost, decimal ProfitPercent, bool Active, string? Version);
public sealed record ProductRequest(string Code, string Name, int BrandId, int CategoryId, string Specifications,
    string Description, string ImageUrl, bool Active, List<VariantRequest> Variants, List<ProductImage> Images);
public sealed record PurchaseSuggestion(long VariantId, string Sku, int Quantity);
public sealed record PurchaseHandoff(long ProductId, IReadOnlyList<PurchaseSuggestion> Lines);

public sealed class VariantDraft
{
    public long Id { get; init; }
    public string? Version { get; init; }
    public decimal OriginalCost { get; init; }
    public string Sku { get; set; } = "";
    public string Color { get; set; } = "";
    public int StorageGb { get; set; } = 128;
    public decimal Cost { get; set; } = 1;
    public decimal ProfitPercent { get; set; } = 20;
    public bool Active { get; set; } = true;
    public decimal? Price { get; init; }
    public int? OnHand { get; init; }
    public int? Reserved { get; init; }
    public int? Available => OnHand - Reserved;
    public int SuggestedQuantity { get; set; } = 1;
    public VariantRequest Request() => new(Id, Sku, Color, StorageGb, Id == 0 ? Cost : OriginalCost, ProfitPercent, Active, Version);
}

public static class CatalogForms
{
    public static bool SavedMatches(ProductRecord saved,ProductRequest draft)
    {
        var original=CatalogWorkspace.Request(saved);
        return draft.Code==saved.Code&&draft.Name==saved.Name&&draft.BrandId==saved.BrandId&&draft.CategoryId==saved.CategoryId&&draft.Specifications==saved.Specifications&&draft.Description==saved.Description&&draft.ImageUrl==saved.ImageUrl&&draft.Active==saved.Active&&
            draft.Variants.OrderBy(v=>v.Id).SequenceEqual(original.Variants.OrderBy(v=>v.Id))&&draft.Images.Select(i=>i.Url).SequenceEqual(saved.Images.OrderBy(i=>i.SortOrder).Select(i=>i.Url));
    }
    public static bool ImageAllowed(string? text, bool optional = true)
    {
        var url = text?.Trim() ?? "";
        if (url.Length == 0) return optional;
        if (url.Length > 1000 || url.Any(char.IsControl)) return false;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.UserInfo.Length == 0) return true;
        return url.StartsWith("/images/", StringComparison.Ordinal) && !url.Contains("..", StringComparison.Ordinal) && !url.Contains('\\');
    }
    public static string? Validate(ProductRequest input, IEnumerable<int> quantities)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || input.Code.Length > 30 || string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 200) return "Nhập mã (tối đa 30 ký tự) và tên điện thoại (tối đa 200 ký tự).";
        if (input.BrandId <= 0 || input.CategoryId <= 0) return "Chọn hãng và danh mục.";
        if (input.Variants.Count is < 1 or > 100) return "Cần từ 1 đến 100 biến thể.";
        if (input.Variants.Any(v => string.IsNullOrWhiteSpace(v.Sku) || v.Sku.Length > 50 || string.IsNullOrWhiteSpace(v.Color) || v.Color.Length > 50 || v.StorageGb <= 0 || v.Cost <= 0)) return "Mỗi biến thể cần SKU, màu, dung lượng và giá vốn dương.";
        if (input.Variants.Any(v => v.ProfitPercent < 0 || v.ProfitPercent > 10000 || decimal.Round(v.ProfitPercent, 2) != v.ProfitPercent)) return "Tỷ lệ lợi nhuận từ 0 đến 10.000%, tối đa 2 chữ số thập phân.";
        if (quantities.Any(q => q <= 0)) return "Lượng nhập gợi ý phải là số nguyên dương; chưa lưu vào kho.";
        if (!ImageAllowed(input.ImageUrl) || input.Images.Count > 12 || input.Images.Any(i => !ImageAllowed(i.Url, false) || i.SortOrder < 0)) return "Ảnh phải là HTTPS hoặc /images/ hợp lệ; tối đa 12 ảnh bổ sung, không để dòng ảnh rỗng.";
        return null;
    }
    public static List<VariantDraft> Drafts(ProductRecord? product) => product?.Variants.Select(v => new VariantDraft
    {
        Id=v.Id,Version=v.Version,OriginalCost=v.Cost,Sku=v.Sku,Color=v.Color,StorageGb=v.StorageGb,Cost=v.Cost,
        ProfitPercent=v.ProfitPercent,Active=v.Active,Price=v.Price,OnHand=v.OnHand,Reserved=v.Reserved
    }).ToList() ?? [new()];
}

public sealed class CatalogWorkspace(IAdminApi api)
{
    public long? PendingProductId { get; private set; }
    private readonly HashSet<string> pendingLookups=[];
    public static string Kind(string kind) => kind is "brands" or "categories" ? kind : throw new AdminApiException("INVALID_INPUT",400);
    public async Task<List<LookupRow>> Lookups(string kind, CancellationToken ct = default)
    {
        var rows = new List<LookupRow>();
        for (var page=1;;page++)
        {
            var batch=await api.GetAsync<PageResult<LookupRow>>($"/api/admin/lookups/{Kind(kind)}?page={page}&pageSize=100",ct);
            rows.AddRange(batch.Items); if(rows.Count>=batch.Total){pendingLookups.Remove(kind);return rows;}
            if(batch.Items.Count==0)throw new AdminApiException("INVALID_RESPONSE");
        }
    }
    public Task<PageResult<ProductRecord>> Products(string? query, int? brand, bool? active, int page=1, int pageSize=20, CancellationToken ct=default)
        => api.GetAsync<PageResult<ProductRecord>>("/api/admin/products"+AdminFormatting.ListQuery(query,page,pageSize)+(brand is >0?$"&brandId={brand}":"")+(active.HasValue?$"&active={active.Value.ToString().ToLowerInvariant()}":""),ct);
    public async Task<ProductRecord> Product(long id, CancellationToken ct=default)
    {var result=await api.GetAsync<ProductRecord>($"/api/admin/products/{id}",ct);if(PendingProductId==id)PendingProductId=null;return result;}
    public async Task<ProductRecord> Save(long id, ProductRequest input, CancellationToken ct=default)
    {
        if(PendingProductId.HasValue)throw new AdminApiException("READBACK_REQUIRED");
        if(id==0)id=await api.CreateAsync("/api/admin/products",input,ct);
        else await api.PutAsync($"/api/admin/products/{id}",input,ct);
        PendingProductId=id;
        try{return await Product(id,ct);}catch(AdminApiException){throw new AdminApiException("READBACK_REQUIRED");}
    }
    public async Task<List<LookupRow>> SaveLookup(string kind, int id, LookupRequest input, CancellationToken ct=default)
    {
        var path=$"/api/admin/lookups/{Kind(kind)}";
        if(pendingLookups.Contains(kind))throw new AdminApiException("READBACK_REQUIRED");
        if(id==0)await api.CreateAsync(path,input,ct); else await api.PutAsync(path+"/"+id,input,ct);
        pendingLookups.Add(kind);
        try{return await Lookups(kind,ct);}catch(AdminApiException){throw new AdminApiException("READBACK_REQUIRED");}
    }
    public static ProductRequest Request(ProductRecord p) => new(p.Code,p.Name,p.BrandId,p.CategoryId,p.Specifications,p.Description,p.ImageUrl,p.Active,
        p.Variants.Select(v=>new VariantRequest(v.Id,v.Sku,v.Color,v.StorageGb,v.Cost,v.ProfitPercent,v.Active,v.Version)).ToList(),p.Images);
    public async Task<ProductRecord?> ReconcileProduct(long id,string code,CancellationToken ct=default)
    {
        ProductRecord? result=null;
        await api.ReconcileAsync(async token=>
        {
            if(id>0){result=await Product(id,token);return;}
            for(var page=1;;page++)
            {
                var batch=await Products(code,null,null,page,100,token);
                result=batch.Items.SingleOrDefault(p=>string.Equals(p.Code,code.Trim(),StringComparison.OrdinalIgnoreCase));
                if(result is not null || page*100>=batch.Total)break;
            }
        },ct);
        return result;
    }
}
