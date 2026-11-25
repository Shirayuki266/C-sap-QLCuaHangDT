using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Web.ProductImages;
using SkiaSharp;

namespace PhoneStore.Checks;

internal static class ProductImageChecks
{
    public static byte[] Fixture(SKEncodedImageFormat format, int width = 40, int height = 30)
    {
        using var image = new SKBitmap(width, height); image.Erase(SKColors.CornflowerBlue);
        using var bytes = image.Encode(format, 90); return bytes.ToArray();
    }
    public static async Task Run()
    {
        await using var database = await TestDatabase.Create();
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "PhoneStore.slnx"))) root = root.Parent;
        await using var factory = new MvcFoundationFactory(database.ConnectionString, root!.FullName);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync();
            await MvcCatalogFixture.Seed(scope.ServiceProvider);
        }
        var storage = factory.Services.GetRequiredService<ProductImageStorage>();
        using var admin = factory.Browser(); using var customer = factory.Browser(); using var guest = factory.Browser();
        async Task<string> Token(HttpClient client)
        {
            using var response = await client.GetAsync("/api/security/token"); response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return json.RootElement.GetProperty("token").GetString()!;
        }
        async Task<HttpResponseMessage> Post(HttpClient client, string route, object input, bool csrf = true)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(input) };
            if (csrf) request.Headers.Add("X-CSRF-TOKEN", await Token(client)); return await client.SendAsync(request);
        }
        async Task Login(HttpClient client, string user, string surface)
        {
            using var response = await Post(client, "/api/auth/login", new { username = user, password = MvcCatalogFixture.Password, surface }); response.EnsureSuccessStatusCode();
        }
        await Login(admin, "catalog-qa-admin", "admin"); await Login(customer, "catalog-qa-customer", "customer");
        var valid = Fixture(SKEncodedImageFormat.Png);
        int Files() => Directory.GetFiles(storage.DirectoryPath).Length;
        await Check.Run("IMAGE auth/CSRF rejects guest, CUSTOMER and missing token before file writes", async () =>
        {
            using var anonymous = await Post(guest, "/api/admin/product-images", new { content = valid });
            using var forbidden = await Post(customer, "/api/admin/product-images", new { content = valid });
            using var noToken = await Post(admin, "/api/admin/product-images", new { content = valid }, false);
            Check.True(anonymous.StatusCode == HttpStatusCode.Unauthorized && forbidden.StatusCode == HttpStatusCode.Forbidden && noToken.StatusCode == HttpStatusCode.BadRequest && Files() == 0, "Unauthorized upload wrote a file.");
        });
        await Check.Run("IMAGE PNG/JPEG/WebP fully decode to public normalized PNG; identical retries are deduplicated", async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            var productsBefore = await db.Products.CountAsync();
            foreach (var format in new[] { SKEncodedImageFormat.Png, SKEncodedImageFormat.Jpeg, SKEncodedImageFormat.Webp })
            {
                using var response = await Post(admin, "/api/admin/product-images", new { content = Fixture(format) });
                Check.True(response.StatusCode == HttpStatusCode.Created, "Valid image rejected.");
                var result = (await response.Content.ReadFromJsonAsync<UploadedProductImage>())!;
                using var thumbnail = SKBitmap.Decode(result.Preview);
                Check.True(result.Width == 40 && result.Height == 30 && result.Url.StartsWith("/images/uploads/products/") && thumbnail is not null, "Invalid upload result.");
                using var file = await guest.GetAsync(result.Url); Check.True(file.IsSuccessStatusCode && file.Content.Headers.ContentType?.MediaType == "image/png", "Uploaded image not publicly available.");
                using var decoded = SKBitmap.Decode(await file.Content.ReadAsByteArrayAsync()); Check.True(decoded is not null && decoded.Width == 40, "Stored file invalid.");
            }
            var count = Files(); using var repeat = await Post(admin, "/api/admin/product-images", new { content = valid });
            Check.True(repeat.StatusCode == HttpStatusCode.Created && Files() == count, "Retry duplicated assets.");
            Check.True(await db.Products.CountAsync() == productsBefore, "Uploading an image created a product.");
        });
        await Check.Run("IMAGE normalizes large dimensions and JPEG orientation before storing", async () =>
        {
            using var resized = await Post(admin, "/api/admin/product-images", new { content = Fixture(SKEncodedImageFormat.Png, 4000, 3100) });
            resized.EnsureSuccessStatusCode(); var small = (await resized.Content.ReadFromJsonAsync<UploadedProductImage>())!;
            Check.True(small.Width == 1600 && small.Height == 1240, "A normal 12-megapixel image was not bounded proportionally.");
            byte[] orientation = [0xff,0xe1,0,34,69,120,105,102,0,0,73,73,42,0,8,0,0,0,1,0,18,1,3,0,1,0,0,0,6,0,0,0,0,0,0,0];
            var jpeg = Fixture(SKEncodedImageFormat.Jpeg);
            using var rotated = await Post(admin, "/api/admin/product-images", new { content = jpeg[..2].Concat(orientation).Concat(jpeg[2..]).ToArray() });
            rotated.EnsureSuccessStatusCode(); var portrait = (await rotated.Content.ReadFromJsonAsync<UploadedProductImage>())!;
            Check.True(portrait.Width == 30 && portrait.Height == 40, "JPEG EXIF orientation was lost.");
        });
        await Check.Run("IMAGE invalid/SVG/truncated/oversized/dimension-bomb inputs create no files", async () =>
        {
            var before = Files();
            foreach (var content in new[] { Array.Empty<byte>(), "<svg onload='alert(1)'/>"u8.ToArray(), valid[..24], new byte[ProductImageStorage.MaxFileBytes + 1], Fixture(SKEncodedImageFormat.Png, 6001, 1) })
            {
                using var response = await Post(admin, "/api/admin/product-images", new { content });
                Check.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge, "Invalid upload accepted.");
            }
            Check.True(Files() == before && !Directory.GetFiles(storage.DirectoryPath, "*.tmp").Any(), "Rejected upload left files.");
            using var hugeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/admin/product-images") { Content = new StringContent(new string('x', ProductImageStorage.MaxRequestBytes + 1), System.Text.Encoding.UTF8, "application/json") };
            hugeRequest.Headers.Add("X-CSRF-TOKEN", await Token(admin));
            using var rejected = await admin.SendAsync(hugeRequest);
            Check.True(rejected.StatusCode == HttpStatusCode.RequestEntityTooLarge && Files() == before, "Oversized JSON request wrote an asset.");
        });
        await Check.Run("IMAGE explicit product save attaches uploaded image and customer detail uses it", async () =>
        {
            using var uploaded = await Post(admin, "/api/admin/product-images", new { content = valid }); uploaded.EnsureSuccessStatusCode();
            var photo = (await uploaded.Content.ReadFromJsonAsync<UploadedProductImage>())!;
            await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            var template = await db.Products.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
            using var saved = await Post(admin, "/api/admin/products", new {
                code = "QA-IMAGE-UPLOAD", name = "Image upload QA phone", template.BrandId, template.CategoryId,
                specifications = "", description = "", imageUrl = photo.Url, active = true,
                variants = new[] { new { id = 0L, sku = "QA-IMAGE-VARIANT", color = "Blue", storageGb = 128, cost = 100m, profitPercent = 20m, active = true, version = (string?)null } },
                images = new[] { new { url = photo.Url, sortOrder = 0 } }
            });
            Check.True(saved.StatusCode == HttpStatusCode.Created, "Product with uploaded image could not be saved.");
            using var created = JsonDocument.Parse(await saved.Content.ReadAsStringAsync()); var id = created.RootElement.GetProperty("id").GetInt64();
            var product = await db.Products.AsNoTracking().SingleAsync(x => x.Id == id);
            using var details = await guest.GetAsync("/Catalog/Details/" + id);
            Check.True(product.ImageUrl == photo.Url && details.IsSuccessStatusCode && (await details.Content.ReadAsStringAsync()).Contains(photo.Url), "Saved image not shown on customer detail.");
        });
    }
}
