using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Images;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SliderPicturator;
using Mapping_Tools.Core.Tools.SliderPicturator.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.SliderPicturator;

[TestClass]
public sealed class SliderPicturatorEngineTests
{
    [TestMethod]
    public void Recolor_SmallRgbaImage_PreservesDimensionsAndCountsSegments()
    {
        // Arrange
        RgbaImage image = new(2, 2, [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 0, 0, 0, 0]);

        // Act
        (var recoloured, long segments) = SliderPicturatorEngine.Recolor(
            image,
            new SliderPicturatorEngineOptions
            {
                CurrentTrackColor = RgbaColour.FromRgb(0, 128, 255),
                BorderColor = RgbaColour.White,
                BackgroundColor = RgbaColour.FromArgb(0, 0, 0, 0),
                Quality = 1,
            });

        // Assert
        recoloured.Width.Should().Be(2);
        recoloured.Height.Should().Be(2);
        segments.Should().BeGreaterThan(0);
        recoloured.Pixels.Should().NotEqual(image.Pixels);
    }

    [TestMethod]
    public void Picturate_SmallImageWithoutSelectedSlider_ReturnsLinearAnchorsAndNoFrameDistance()
    {
        // Arrange
        RgbaImage image = new(2, 1, [255, 255, 255, 255, 0, 0, 0, 255]);

        // Act
        (var path, double frameDistance) = SliderPicturatorEngine.Picturate(
            image,
            4,
            new SliderPicturatorEngineOptions
            {
                CurrentTrackColor = RgbaColour.FromRgb(0, 128, 255),
                BorderColor = RgbaColour.White,
                BackgroundColor = RgbaColour.FromArgb(0, 0, 0, 0),
                Quality = 1,
            });

        // Assert
        path.Should().HaveCountGreaterThan(2);
        frameDistance.Should().Be(0);
        path[0].Should().Be(new Vector2(256, 192));
    }

    [TestMethod]
    public void RgbaImage_SetPixelAfterGetPixel_PreservesArgbChannels()
    {
        // Arrange
        RgbaImage image = new(1, 1, [10, 20, 30, 40]);
        var replacement = RgbaColour.FromArgb(200, 100, 110, 120);

        // Act
        image.SetPixel(0, 0, replacement);

        // Assert
        image.GetPixel(0, 0).Should().Be(replacement);
    }

    [TestMethod]
    public void Recolor_WithTransparentBorderInput_EmitsOpaqueSliderBorder()
    {
        // Arrange
        RgbaImage image = new(1, 1, [0, 0, 0, 255]);
        var border = RgbaColour.FromArgb(32, 12, 34, 56);

        // Act
        var (recoloured, _) = SliderPicturatorEngine.Recolor(
            image,
            new SliderPicturatorEngineOptions
            {
                CurrentTrackColor = RgbaColour.FromRgb(255, 255, 255),
                BorderColor = border,
                BackgroundColor = RgbaColour.FromArgb(0, 0, 0, 0),
                BlackOn = false,
                BorderOn = true,
                Quality = 1,
            });

        // Assert
        recoloured.GetPixel(0, 0).A.Should().Be(255);
    }
}
