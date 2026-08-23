using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Core.Classes.Images;

/// <summary>
///     Stores a tightly packed row-major image without depending on a UI or bitmap library.
/// </summary>
public sealed class RgbaImage
{
    /// <summary>
    ///     Creates an image from RGBA channel bytes in row-major order.
    /// </summary>
    /// <param name="width">The positive image width in pixels.</param>
    /// <param name="height">The positive image height in pixels.</param>
    /// <param name="pixels">Four bytes per pixel in R, G, B, A order.</param>
    public RgbaImage(int width, int height, ReadOnlySpan<byte> pixels)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (pixels.Length != checked(width * height * 4))
            throw new ArgumentException("The pixel buffer must contain exactly four bytes per pixel.", nameof(pixels));

        Width = width;
        Height = height;
        Pixels = pixels.ToArray();
    }

    /// <summary>Gets the image width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the image height in pixels.</summary>
    public int Height { get; }

    /// <summary>Gets the mutable RGBA bytes in row-major order.</summary>
    public byte[] Pixels { get; }

    /// <summary>Gets one pixel by its zero-based coordinates.</summary>
    public RgbaColour GetPixel(int x, int y)
    {
        int offset = GetOffset(x, y);
        return RgbaColour.FromArgb(Pixels[offset + 3], Pixels[offset], Pixels[offset + 1], Pixels[offset + 2]);
    }

    /// <summary>Replaces one pixel by its zero-based coordinates.</summary>
    /// <param name="x">The horizontal pixel coordinate.</param>
    /// <param name="y">The vertical pixel coordinate.</param>
    /// <param name="colour">The replacement colour.</param>
    public void SetPixel(int x, int y, RgbaColour colour)
    {
        int offset = GetOffset(x, y);
        Pixels[offset] = colour.R;
        Pixels[offset + 1] = colour.G;
        Pixels[offset + 2] = colour.B;
        Pixels[offset + 3] = colour.A;
    }

    /// <summary>Creates an independent copy suitable for background processing.</summary>
    /// <returns>A copy with the same dimensions and pixel values.</returns>
    public RgbaImage Clone()
    {
        return new RgbaImage(Width, Height, Pixels);
    }

    private int GetOffset(int x, int y)
    {
        if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
        return checked((y * Width + x) * 4);
    }
}
