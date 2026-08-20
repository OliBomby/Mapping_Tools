using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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
            Rectangle bounds = new(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int rowLength = bitmap.Width * 4;
                byte[] row = new byte[rowLength];
                for (int y = 0; y < bitmap.Height; y++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    IntPtr rowAddress = data.Stride >= 0
                        ? IntPtr.Add(data.Scan0, y * data.Stride)
                        : IntPtr.Add(data.Scan0, (bitmap.Height - 1 - y) * -data.Stride);
                    Marshal.Copy(rowAddress, row, 0, rowLength);
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int sourceOffset = x * 4;
                        int destinationOffset = (y * bitmap.Width + x) * 4;
                        pixels[destinationOffset] = row[sourceOffset + 2];
                        pixels[destinationOffset + 1] = row[sourceOffset + 1];
                        pixels[destinationOffset + 2] = row[sourceOffset];
                        pixels[destinationOffset + 3] = row[sourceOffset + 3];
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
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
