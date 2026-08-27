using System.Runtime.InteropServices;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Core.Images;
using SkiaSharp;

namespace Mapping_Tools.Infrastructure.Tools.SliderPicturator;

/// <summary>Decodes local image files into framework-neutral RGBA pixel buffers through SkiaSharp.</summary>
/// <remarks>
///     The decoder reads the file before handing it to SkiaSharp so cancellation can interrupt file I/O.
///     Decoding requests an unpremultiplied RGBA buffer, making the channel order and alpha semantics
///     independent of the source format and host platform.
/// </remarks>
public sealed class SkiaSharpImageFileService : IImageFileService
{
    /// <inheritdoc />
    public async Task<RgbaImage> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            byte[] encodedImage = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            SKImageInfo bounds = SKBitmap.DecodeBounds(encodedImage);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                throw new InvalidDataException("The image has no pixels.");

            SKImageInfo rgbaInfo = new(
                bounds.Width,
                bounds.Height,
                SKColorType.Rgba8888,
                SKAlphaType.Unpremul);
            using SKBitmap bitmap = SKBitmap.Decode(encodedImage, rgbaInfo)
                                      ?? throw new InvalidDataException("SkiaSharp could not decode the image.");

            byte[] pixels = new byte[checked(bitmap.Width * bitmap.Height * 4)];
            nint sourceAddress = bitmap.GetPixels();
            if (sourceAddress == nint.Zero) throw new InvalidDataException("The decoded image has no pixel buffer.");

            int rowLength = checked(bitmap.Width * 4);
            byte[] row = new byte[rowLength];
            for (int y = 0; y < bitmap.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Marshal.Copy(IntPtr.Add(sourceAddress, checked(y * bitmap.RowBytes)), row, 0, rowLength);
                row.AsSpan().CopyTo(pixels.AsSpan(checked(y * rowLength), rowLength));
            }

            return new RgbaImage(bitmap.Width, bitmap.Height, pixels);
        }
        catch (Exception exception) when (exception is ArgumentException or
                                              IOException or
                                              InvalidOperationException or
                                              OutOfMemoryException)
        {
            throw new InvalidDataException("Not a valid image file.", exception);
        }
    }
}
