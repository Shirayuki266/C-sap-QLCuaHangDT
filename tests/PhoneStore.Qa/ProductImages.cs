using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;
internal static class ProductImages
{
    sealed record Image(string Url,string Source,string Title,string Author,string License,string LicenseUrl,int VerifiedHttp);
    sealed record Entry(string Model,List<Image> Images,string FallbackReason);
    sealed record Correction(string ProductCode,List<string> OldUrls,string NewCoverUrl,string Reason);
    public static async Task Run(string root){
        var json=new JsonSerializerOptions{PropertyNameCaseInsensitive=true,WriteIndented=true};var state=JsonSerializer.Deserialize<QaEnvironment.State>(await File.ReadAllTextAsync(Path.Combine(root,".artifacts/qa-env/private.json")),json)!;QaTarget.Validate(state.ConnectionString,QaEnvironment.Config(root).Server,state.Database);
        await using var db=new StoreDbContext(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(state.ConnectionString).Options);var before=await db.Products.CountAsync();var ordersBefore=await db.Orders.CountAsync();var purchasesBefore=await db.Purchases.CountAsync();await DatabaseSetup.Migrate(db);
        var entries=JsonSerializer.Deserialize<List<Entry>>(await File.ReadAllTextAsync(Path.Combine(root,"qa/product-images.json")),json)!;var changed=0;
        var correctionsFile=Path.Combine(root,"qa/image-corrections.json");if(File.Exists(correctionsFile)){var corrections=JsonSerializer.Deserialize<List<Correction>>(await File.ReadAllTextAsync(correctionsFile),json)??[];foreach(var correction in corrections){if(!correction.ProductCode.StartsWith("DEMO-")||!correction.NewCoverUrl.StartsWith("https://"))throw new Exception("Invalid curated correction");var product=await db.Products.Include(x=>x.Images).SingleOrDefaultAsync(x=>x.Code==correction.ProductCode);if(product!=null&&correction.OldUrls.Contains(product.ImageUrl??"")){product.ImageUrl=correction.NewCoverUrl;db.RemoveRange(product.Images.Where(x=>correction.OldUrls.Contains(x.Url)));changed++;}}}
        foreach(var entry in entries.Where(x=>x.Images.Count>0)){
            var products=await db.Products.Include(x=>x.Images).Where(p=>(p.Code.StartsWith("DEMO-P")&&p.Name.StartsWith(entry.Model+" ·"))||(p.Code=="IP16"&&entry.Model=="iPhone 16")||(p.Code=="S25"&&entry.Model=="Galaxy S25")||(p.Code=="XM14"&&entry.Model=="Xiaomi 14")).ToListAsync();
            foreach(var product in products){if(!string.IsNullOrWhiteSpace(product.ImageUrl)&&product.ImageUrl!="/images/phone.svg")continue;product.ImageUrl=entry.Images[0].Url;if(product.Images.Count==0)product.Images.AddRange(entry.Images.Skip(1).Select((x,i)=>new ProductImage{Url=x.Url,SortOrder=i}));changed++;}
        }
        await db.SaveChangesAsync();if(before!=await db.Products.CountAsync())throw new Exception("Product count changed unexpectedly");Console.WriteLine($"PASS: migration002; {changed} product covers updated; existing custom images preserved.");
        var rejectedFile=Path.Combine(root,".artifacts/qa-env/rejected-image-urls.json");if(File.Exists(rejectedFile)){var rejected=JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(rejectedFile))??[];await db.Set<ProductImage>().Where(x=>x.Product.Code.StartsWith("DEMO-P")&&rejected.Contains(x.Url)).ExecuteDeleteAsync();}
        if(ordersBefore!=await db.Orders.CountAsync()||purchasesBefore!=await db.Purchases.CountAsync())throw new Exception("Existing transactions changed unexpectedly");var migration=await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM __PhoneStoreMigrations WHERE Id LIKE '%002_ProductImages.sql'").SingleAsync();if(migration!=1)throw new Exception("Migration002 ledger missing");var output=Path.Combine(root,".artifacts/web-discovery");Directory.CreateDirectory(output);await File.WriteAllTextAsync(Path.Combine(output,"image-migration.json"),JsonSerializer.Serialize(new{database=state.Database,status="PASS",migration="002_ProductImages",products=before,orders=ordersBefore,purchases=purchasesBefore,changed,verification="Migration/re-run preserves existing product and transaction counts; custom image covers skipped."},json));
    }
}
