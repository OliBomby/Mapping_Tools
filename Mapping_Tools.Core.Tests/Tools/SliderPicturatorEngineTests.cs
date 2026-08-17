using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.Images;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Tools.SliderPicturator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class SliderPicturatorEngineTests
{
    [TestMethod]
    public void Recolor_SmallRgbaImage_PreservesDimensionsAndCountsSegments()
    {
        // Arrange
        RgbaImage image = new(2, 2, [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 0, 0, 0, 0]);

        // Act
        (RgbaImage recoloured, long segments) = SliderPicturatorEngine.Recolor(
            image,
            RgbaColour.FromRgb(0, 128, 255),
            RgbaColour.White,
            RgbaColour.FromArgb(0, 0, 0, 0),
            quality: 1);

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
        (List<Vector2> path, double frameDistance) = SliderPicturatorEngine.Picturate(
            image,
            RgbaColour.FromRgb(0, 128, 255),
            RgbaColour.White,
            RgbaColour.FromArgb(0, 0, 0, 0),
            4,
            new Vector2(256, 192),
            Vector2.Zero,
            quality: 1);

        // Assert
        path.Should().HaveCountGreaterThan(2);
        frameDistance.Should().Be(0);
        path[0].Should().Be(new Vector2(256, 192));
    }

    [TestMethod]
    public void SliderPicturatorOptions_QualityOutsideLegacySliderRange_ThrowsBeforeRun()
    {
        // Arrange
        SliderPicturatorOptions options = new() { PictureFile = "image.png", Quality = 102 };

        // Act
        Action act = options.Validate;

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void RgbaImage_SetPixelAfterGetPixel_PreservesArgbChannels()
    {
        // Arrange
        RgbaImage image = new(1, 1, [10, 20, 30, 40]);
        RgbaColour replacement = RgbaColour.FromArgb(200, 100, 110, 120);

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
        RgbaColour border = RgbaColour.FromArgb(32, 12, 34, 56);

        // Act
        (RgbaImage recoloured, _) = SliderPicturatorEngine.Recolor(
            image,
            RgbaColour.FromRgb(255, 255, 255),
            border,
            RgbaColour.FromArgb(0, 0, 0, 0),
            blackOff: true,
            borderOff: false,
            quality: 1);

        // Assert
        recoloured.GetPixel(0, 0).A.Should().Be(255);
    }
}
