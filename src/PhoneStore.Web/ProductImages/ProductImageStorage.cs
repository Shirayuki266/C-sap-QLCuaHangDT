using System.Security.Cryptography;
using SkiaSharp;

namespace PhoneStore.Web.ProductImages;

public sealed record ProductImageUpload(byte[] Content);
public sealed record UploadedProductImage(string Url, int Width, int Height, byte[] Preview);

public sealed class ProductImageStorage(string directory)
{
    public const int MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxRequestBytes = 7 * 1024 * 1024;
    private readonly SemaphoreSlim decoding = new(2, 2);
    public string DirectoryPath { get; } = Path.GetFullPath(directory);

    public async Task<UploadedProductImage> SaveAsync(byte[] content, CancellationToken ct)
    {
        if (content.Length is 0 or > MaxFileBytes) throw new ProductImageException("IMAGE_TOO_LARGE");
        await decoding.WaitAsync(ct);
        try
        {
            using var data = SKData.CreateCopy(content);
            using var codec = SKCodec.Create(data);
            if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Png or SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Webp))
                throw new ProductImageException("IMAGE_INVALID");
            var info = codec.Info;
            if (info.Width > 6000 || info.Height > 6000 || (long)info.Width * info.Height > 16_000_000)
                throw new ProductImageException("IMAGE_DIMENSIONS");
            using var decoded = new SKBitmap(new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
            if (codec.GetPixels(decoded.Info, decoded.GetPixels()) != SKCodecResult.Success) throw new ProductImageException("IMAGE_INVALID");
            using var image = Normalize(decoded, codec.EncodedOrigin);
            using var normalized = image.Encode(SKEncodedImageFormat.Png, 100);
            var bytes = normalized.ToArray();
            if (bytes.Length > MaxFileBytes) throw new ProductImageException("IMAGE_TOO_LARGE");
            var name = Convert.ToHexStringLower(SHA256.HashData(bytes)) + ".png";
            Directory.CreateDirectory(DirectoryPath);
            var destination = Path.Combine(DirectoryPath, name);
            if (!File.Exists(destination))
            {
                var temporary = Path.Combine(DirectoryPath, Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    await File.WriteAllBytesAsync(temporary, bytes, ct);
                    try { File.Move(temporary, destination); }
                    catch (IOException) when (File.Exists(destination)) { /* Same content uploaded concurrently. */ }
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
            var ratio = Math.Min(1, 320d / Math.Max(image.Width, image.Height));
            using var thumbnail = image.Resize(new SKImageInfo(Math.Max(1, (int)(image.Width * ratio)), Math.Max(1, (int)(image.Height * ratio))), new SKSamplingOptions(SKFilterMode.Linear));
            using var preview = thumbnail.Encode(SKEncodedImageFormat.Png, 100);
            return new("/images/uploads/products/" + name, image.Width, image.Height, preview.ToArray());
        }
        finally { decoding.Release(); }
    }

    private static SKBitmap Normalize(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapped = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = swapped ? source.Height : source.Width; var height = swapped ? source.Width : source.Height;
        var scale = Math.Min(1, 1600d / Math.Max(width, height));
        var target = new SKBitmap(new SKImageInfo(Math.Max(1, (int)(width * scale)), Math.Max(1, (int)(height * scale)), SKColorType.Rgba8888, SKAlphaType.Premul));
        try
        {
            using var canvas = new SKCanvas(target); canvas.Clear(SKColors.Transparent); canvas.Scale((float)scale);
            switch (origin)
            {
                case SKEncodedOrigin.TopRight: canvas.Translate(source.Width, 0); canvas.Scale(-1, 1); break;
                case SKEncodedOrigin.BottomRight: canvas.Translate(source.Width, source.Height); canvas.RotateDegrees(180); break;
                case SKEncodedOrigin.BottomLeft: canvas.Translate(0, source.Height); canvas.Scale(1, -1); break;
                case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
                case SKEncodedOrigin.RightTop: canvas.Translate(source.Height, 0); canvas.RotateDegrees(90); break;
                case SKEncodedOrigin.RightBottom: canvas.Translate(source.Height, source.Width); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
                case SKEncodedOrigin.LeftBottom: canvas.Translate(0, source.Width); canvas.RotateDegrees(270); break;
            }
            using var image = SKImage.FromBitmap(source);
            canvas.DrawImage(image, new SKRect(0, 0, source.Width, source.Height), new SKSamplingOptions(SKFilterMode.Linear));
            canvas.Flush(); return target;
        }
        catch { target.Dispose(); throw; }
    }
}

public sealed class ProductImageException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
