using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Mapping_Tools.Core.Classes.Images;

namespace Mapping_Tools.Desktop.Utilities;

/// <summary>Converts Core pixel buffers into Avalonia preview bitmaps at the UI boundary.</summary>
public static class RgbaImageBitmapFactory
{
    /// <summary>Creates an Avalonia bitmap from RGBA bytes.</summary>
    /// <param name="image">The framework-neutral source image.</param>
    /// <returns>A writable Avalonia bitmap containing a copy of the pixels.</returns>
    public static Bitmap Create(RgbaImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        WriteableBitmap bitmap = new(new PixelSize(image.Width, image.Height), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);
        using (ILockedFramebuffer framebuffer = bitmap.Lock())
        {
            int rowBytes = image.Width * 4;
            for (int y = 0; y < image.Height; y++)
                Marshal.Copy(image.Pixels, y * rowBytes, framebuffer.Address + y * framebuffer.RowBytes, rowBytes);
        }
        return bitmap;
    }
}
