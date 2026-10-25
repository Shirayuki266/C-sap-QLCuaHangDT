using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Contracts;
namespace PhoneStore.Infrastructure.Services;
public sealed partial class StoreService
{
    const string SearchCollation="Latin1_General_100_CI_AI";
    public Task<List<string>> Colors()=>db.Variants.Where(x=>x.Active&&x.Product.Active&&x.Product.Brand.Active&&x.Product.Category.Active).Select(x=>x.Color).Distinct().OrderBy(x=>x).ToListAsync();
    public async Task<List<SearchSuggestion>> Suggestions(string? query){
        var term=(query??"").Trim().Replace('đ','d').Replace('Đ','D');if(term.Length==0)return [];if(term.Length>100)throw Error("INVALID_INPUT","Từ khóa tối đa 100 ký tự.");
        var products=await db.Products.AsNoTracking().Where(p=>p.Active&&p.Brand.Active&&p.Category.Active&&p.Variants.Any(v=>v.Active)&&(EF.Functions.Collate(p.Name,SearchCollation).Replace("đ","d").Contains(term)||EF.Functions.Collate(p.Brand.Name,SearchCollation).Replace("đ","d").Contains(term)))
            .OrderBy(p=>EF.Functions.Collate(p.Name,SearchCollation).Replace("đ","d")==term?0:EF.Functions.Collate(p.Name,SearchCollation).Replace("đ","d").StartsWith(term)?1:2).ThenBy(p=>p.Name).ThenBy(p=>p.Id).Select(p=>new{p.Id,p.Name}).Take(6).ToListAsync();
        var brands=await db.Brands.Where(b=>b.Active&&EF.Functions.Collate(b.Name,SearchCollation).Replace("đ","d").Contains(term)&&db.Products.Any(p=>p.Active&&p.BrandId==b.Id&&p.Category.Active&&p.Variants.Any(v=>v.Active))).OrderBy(b=>EF.Functions.Collate(b.Name,SearchCollation).Replace("đ","d")==term?0:EF.Functions.Collate(b.Name,SearchCollation).Replace("đ","d").StartsWith(term)?1:2).ThenBy(b=>b.Name).Select(b=>b.Name).Take(2).ToListAsync();
        string Fold(string text)=>string.Concat(text.Normalize(System.Text.NormalizationForm.FormD).Where(c=>System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)!=System.Globalization.UnicodeCategory.NonSpacingMark)).Replace('đ','d').Replace('Đ','D').ToLowerInvariant();var folded=Fold(term);
        return brands.Select(b=>new SearchSuggestion(b,"Thương hiệu","/Search?Query="+Uri.EscapeDataString(b))).Concat(products.Select(p=>new SearchSuggestion(p.Name,"Sản phẩm","/Catalog/Details/"+p.Id))).DistinctBy(x=>x.Kind+":"+x.Label.ToLowerInvariant()).OrderBy(x=>Fold(x.Label)==folded?0:Fold(x.Label).StartsWith(folded)?1:2).ThenBy(x=>x.Label,StringComparer.OrdinalIgnoreCase).ThenBy(x=>x.Kind).Take(8).ToList();
    }
    public async Task<List<ProductDto>> BestSellers(){
        // Same completed/paid predicate and UTC+7 day bounds as the Admin revenue report.
        var today=DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));var bounds=Bounds(today.AddDays(-29),today);
        var lines=RevenueOrders.Where(o=>o.CompletedAt>=bounds.Start&&o.CompletedAt<bounds.End).SelectMany(o=>o.Lines);
        var ranking=await lines.Where(l=>l.Variant.Product.Active&&l.Variant.Product.Brand.Active&&l.Variant.Product.Category.Active&&l.Variant.Product.Variants.Any(v=>v.Active))
            .GroupBy(l=>l.Variant.ProductId).Select(g=>new{Id=g.Key,Quantity=g.Sum(x=>(long)x.Quantity),Revenue=g.Sum(x=>x.Price*x.Quantity)}).OrderByDescending(x=>x.Quantity).ThenByDescending(x=>x.Revenue).ThenBy(x=>x.Id).Take(8).ToListAsync();
        var ids=ranking.Select(x=>x.Id).ToList();var products=(await ProductQuery.AsNoTracking().Where(p=>ids.Contains(p.Id)).ToListAsync()).Select(ProductMap).Select(p=>p with{Variants=p.Variants.Where(v=>v.Active).ToList()}).ToDictionary(p=>p.Id);
        return ids.Select(id=>products[id]).ToList();
    }
}
