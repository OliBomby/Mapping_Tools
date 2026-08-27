using Mapping_Tools.Core.Images;
using Mapping_Tools.Infrastructure.Tools.SliderPicturator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Mapping_Tools.Infrastructure.Tests.Tools.SliderPicturator;

[TestClass]
public sealed class SkiaSharpImageFileServiceTests
{
    [TestMethod]
    public async Task LoadAsync_WithPngSource_PreservesRgbaPixels()
    {
        // Arrange
        string path = CreateTemporaryPath(".png");
        await File.WriteAllBytesAsync(path, CreatePng());
        SkiaSharpImageFileService sut = new();

        try
        {
            // Act
            RgbaImage result = await sut.LoadAsync(path);

            // Assert
            result.Width.Should().Be(2);
            result.Height.Should().Be(1);
            result.Pixels.Should().Equal(
                10, 20, 30, 40,
                200, 210, 220, 230);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task LoadAsync_WithInvalidImage_ThrowsInvalidDataException()
    {
        // Arrange
        string path = CreateTemporaryPath(".img");
        await File.WriteAllBytesAsync(path, [1, 2, 3, 4]);
        SkiaSharpImageFileService sut = new();

        try
        {
            // Act
            Func<Task> act = () => sut.LoadAsync(path);

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task LoadAsync_WithCanceledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SkiaSharpImageFileService sut = new();

        // Act
        Func<Task> act = () => sut.LoadAsync("unused.png", cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static string CreateTemporaryPath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), $"mapping-tools-image-{Guid.NewGuid():N}{extension}");
    }

    private static byte[] CreatePng()
    {
        using SKBitmap bitmap = new(new SKImageInfo(
            2,
            1,
            SKColorType.Rgba8888,
            SKAlphaType.Unpremul));
        bitmap.SetPixel(0, 0, new SKColor(10, 20, 30, 40));
        bitmap.SetPixel(1, 0, new SKColor(200, 210, 220, 230));
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
