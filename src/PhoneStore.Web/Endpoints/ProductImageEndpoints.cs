using System.Text.Json;
using PhoneStore.Core;
using PhoneStore.Web.ProductImages;
using PhoneStore.Web.Security;

namespace PhoneStore.Web.Endpoints;

public static class ProductImageEndpoints
{
    public static void MapProductImages(this WebApplication app)
    {
        app.MapPost("/api/admin/product-images", async (HttpContext context, ProductImageStorage storage) =>
        {
            IResult Invalid(string code, int status = 400) => Results.Json(new ApiError(code,
                code == "IMAGE_TOO_LARGE" ? "Chọn ảnh có dữ liệu, tối đa 5 MB." : code == "IMAGE_DIMENSIONS" ? "Ảnh tối đa 6000 pixel mỗi chiều và 16 triệu pixel." : "Chọn ảnh PNG, JPG hoặc WebP hợp lệ.", context.TraceIdentifier), statusCode: status);
            if (!context.Request.HasJsonContentType()) return Invalid("IMAGE_INVALID");
            if (context.Request.ContentLength > ProductImageStorage.MaxRequestBytes) return Invalid("IMAGE_TOO_LARGE", 413);
            using var body = new MemoryStream(); var buffer = new byte[81920]; int read;
            while ((read = await context.Request.Body.ReadAsync(buffer, context.RequestAborted)) > 0)
            {
                if (body.Length + read > ProductImageStorage.MaxRequestBytes) return Invalid("IMAGE_TOO_LARGE", 413);
                await body.WriteAsync(buffer.AsMemory(0, read), context.RequestAborted);
            }
            try
            {
                var input = JsonSerializer.Deserialize<ProductImageUpload>(body.GetBuffer().AsSpan(0, checked((int)body.Length)), new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (input?.Content is null) return Invalid("IMAGE_INVALID");
                var uploaded = await storage.SaveAsync(input.Content, context.RequestAborted);
                return Results.Created(uploaded.Url, uploaded);
            }
            catch (JsonException) { return Invalid("IMAGE_INVALID"); }
            catch (ProductImageException error) { return Invalid(error.Code); }
        }).RequireAuthorization("Admin").AddEndpointFilter<CommerceCsrfFilter>();
    }
}
