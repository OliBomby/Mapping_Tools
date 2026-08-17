using System.Drawing;
using Mapping_Tools.Application.SliderPicturator;
using Mapping_Tools.Core.Classes.Images;

namespace Mapping_Tools.Infrastructure.Images;

/// <summary>Decodes local images through the Windows bitmap codecs at the infrastructure boundary.</summary>
/// <remarks>
/// This is the explicitly Windows-specific adapter for the portable
/// <see cref="IImageFileService"/> contract. Core and Application consume only
/// <see cref="RgbaImage"/>; a non-Windows decoder can replace this registration
/// without changing the feature layers.
/// </remarks>
public sealed class SystemDrawingImageFileService : IImageFileService
{
    /// <inheritdoc/>
    public Task<RgbaImage> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using Bitmap bitmap = new(path);
            byte[] pixels = new byte[checked(bitmap.Width * bitmap.Height * 4)];
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Color colour = bitmap.GetPixel(x, y);
                    int offset = (y * bitmap.Width + x) * 4;
                    pixels[offset] = colour.R;
                    pixels[offset + 1] = colour.G;
                    pixels[offset + 2] = colour.B;
                    pixels[offset + 3] = colour.A;
                }
            }
            return Task.FromResult(new RgbaImage(bitmap.Width, bitmap.Height, pixels));
        }
        catch (Exception exception) when (exception is ArgumentException or
            System.Runtime.InteropServices.ExternalException or IOException or OutOfMemoryException)
        {
            throw new InvalidDataException("Not a valid image file.", exception);
        }
    }
}
