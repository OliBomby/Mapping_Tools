using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
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

    [TestMethod]
    public void Recolor_WithAlphaOnAndFullyTransparentWhitePixel_UsesBackgroundColour()
    {
        // Arrange
        RgbaImage image = new(1, 1, [255, 255, 255, 0]);
        SliderPicturatorEngineOptions options = new()
        {
            CurrentTrackColor = RgbaColour.White,
            BorderColor = RgbaColour.White,
            BackgroundColor = RgbaColour.FromRgb(0, 0, 0),
            Quality = 1,
        };

        // Act
        var (recoloured, _) = SliderPicturatorEngine.Recolor(image, options);

        // Assert
        recoloured.GetPixel(0, 0).Should().Be(RgbaColour.FromRgb(0, 0, 0));
    }

    [TestMethod]
    public void Picturate_WithAlphaOnAndFullyTransparentWhitePixel_MatchesBackgroundPixel()
    {
        // Arrange
        RgbaImage transparentWhite = new(1, 1, [255, 255, 255, 0]);
        RgbaImage opaqueBlack = new(1, 1, [0, 0, 0, 255]);
        SliderPicturatorEngineOptions options = new()
        {
            CurrentTrackColor = RgbaColour.White,
            BorderColor = RgbaColour.White,
            BackgroundColor = RgbaColour.FromRgb(0, 0, 0),
            Quality = 1,
        };

        // Act
        var transparentPath = SliderPicturatorEngine.Picturate(transparentWhite, 4, options).Path;
        var opaquePath = SliderPicturatorEngine.Picturate(opaqueBlack, 4, options).Path;

        // Assert
        transparentPath.Should().Equal(opaquePath);
    }

    [TestMethod]
    public void Recolor_WithAlphaDisabledAndDefaultBackground_MatchesOpaqueBlackBackground()
    {
        // Arrange
        RgbaImage image = new(1, 1, [255, 0, 0, 128]);
        SliderPicturatorEngineOptions defaultOptions = new() { AlphaOn = false };
        SliderPicturatorEngineOptions opaqueBlackOptions = new()
        {
            AlphaOn = false,
            BackgroundColor = RgbaColour.FromRgb(0, 0, 0),
        };
        SliderPicturatorEngineOptions transparentOptions = new()
        {
            AlphaOn = false,
            BackgroundColor = RgbaColour.FromArgb(0, 0, 0, 0),
        };

        // Act
        var defaultPixel = SliderPicturatorEngine.Recolor(image, defaultOptions).Image.GetPixel(0, 0);
        var opaqueBlackPixel = SliderPicturatorEngine.Recolor(image, opaqueBlackOptions).Image.GetPixel(0, 0);
        var transparentPixel = SliderPicturatorEngine.Recolor(image, transparentOptions).Image.GetPixel(0, 0);

        // Assert
        defaultPixel.Should().Be(opaqueBlackPixel);
        defaultPixel.Should().NotBe(transparentPixel);
    }

    [TestMethod]
    public void ApplyToBeatmap_WithDurationBasedVelocity_InsertsSliderAndRestoresTiming()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        Beatmap beatmap = new([], [redline], redline);
        List<Vector2> path = [new(256, 192), new(356, 192)];
        SliderPicturatorEngineOptions options = new()
        {
            TimeCode = 1000,
            Duration = 500,
            CurrentTrackColor = RgbaColour.FromRgb(10, 20, 30),
            BorderColor = RgbaColour.FromRgb(40, 50, 60),
        };

        // Act
        SliderPicturatorEngine.ApplyToBeatmap(beatmap, path, 0, options);

        // Assert
        beatmap.HitObjects.Should().ContainSingle();
        beatmap.HitObjects[0].IsSlider.Should().BeTrue();
        beatmap.HitObjects[0].Time.Should().Be(999);
        beatmap.HitObjects[0].PixelLength.Should().BeApproximately(100, 0.000001);
        beatmap.BeatmapTiming.GetMpBAtTime(999).Should().BeApproximately(700, 0.000001);
        beatmap.BeatmapTiming.GetMpBAtTime(1000).Should().BeApproximately(500, 0.000001);
        beatmap.SpecialColours["SliderTrackOverride"].Color.Should().Be(RgbaColour.FromRgb(10, 20, 30));
        beatmap.SpecialColours["SliderBorder"].Color.Should().Be(RgbaColour.FromRgb(40, 50, 60));
    }

    [TestMethod]
    public void ApplyToBeatmap_WithFrameDistance_UsesDistanceBasedVelocityAndSkipsColourChanges()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        Beatmap beatmap = new([], [redline], redline);
        List<Vector2> path = [new(256, 192), new(356, 192)];
        SliderPicturatorEngineOptions options = new()
        {
            TimeCode = 1000,
            Duration = 500,
            SetBeatmapColors = false,
        };

        // Act
        SliderPicturatorEngine.ApplyToBeatmap(beatmap, path, 10, options);

        // Assert
        beatmap.HitObjects.Should().ContainSingle();
        beatmap.BeatmapTiming.GetMpBAtTime(999).Should().BeApproximately(14, 0.000001);
        beatmap.SpecialColours.Should().BeEmpty();
    }

    [TestMethod]
    public void Picturate_WithSelectedSlider_AddsSliderballFramesToGeneratedPath()
    {
        // Arrange
        RgbaImage image = new(2, 2,
            [255, 255, 255, 255, 0, 0, 0, 255, 0, 0, 0, 255, 255, 255, 255, 255]);
        HitObject selected = new("256,192,100,2,0,L|270:192,1,14,0|0,0:0|0:0,0:0:0:0:")
        {
            TemporalLength = 4,
        };
        SliderPicturatorEngineOptions options = new()
        {
            SelectedSlider = selected,
            ViewportSize = 1,
        };

        // Act
        (List<Vector2> path, double frameDistance) = SliderPicturatorEngine.Picturate(image, 4, options);

        // Assert
        frameDistance.Should().BeGreaterThan(0);
        path.Should().HaveCountGreaterThan(10);
        path.Should().Contain(selected.SliderPath.SliderballPositionAt(1, 4).Rounded());
    }

    [TestMethod]
    public void Recolor_WithBlackBorderAndTrackPixels_ClassifiesEachColourSeparately()
    {
        // Arrange
        RgbaImage image = new(3, 1,
            [0, 0, 0, 255, 255, 0, 0, 255, 100, 100, 100, 255]);
        SliderPicturatorEngineOptions options = new()
        {
            CurrentTrackColor = RgbaColour.FromRgb(100, 100, 100),
            BorderColor = RgbaColour.FromRgb(255, 0, 0),
            BackgroundColor = RgbaColour.FromRgb(0, 0, 0),
            BlackOn = true,
            BorderOn = true,
            Quality = 1,
        };

        // Act
        var (recoloured, segments) = SliderPicturatorEngine.Recolor(image, options);

        // Assert
        recoloured.GetPixel(0, 0).Should().Be(RgbaColour.FromRgb(0, 0, 0));
        recoloured.GetPixel(1, 0).Should().Be(RgbaColour.FromRgb(255, 0, 0));
        var track = recoloured.GetPixel(2, 0);
        track.R.Should().Be(track.G);
        track.G.Should().Be(track.B);
        track.R.Should().BeGreaterThan(0);
        segments.Should().BeGreaterThan(3);
    }

    [TestMethod]
    public void Picturate_WithBlueChannelDisabled_IgnoresBlueOnlyDifferences()
    {
        // Arrange
        RgbaImage blueAtFirstPixel = new(2, 1,
            [20, 40, 0, 255, 80, 100, 0, 255]);
        RgbaImage blueAtSecondPixel = new(2, 1,
            [20, 40, 255, 255, 80, 100, 255, 255]);
        SliderPicturatorEngineOptions options = new()
        {
            RedOn = true,
            GreenOn = true,
            BlueOn = false,
            Quality = 1,
        };

        // Act
        var first = SliderPicturatorEngine.Picturate(blueAtFirstPixel, 4, options);
        var second = SliderPicturatorEngine.Picturate(blueAtSecondPixel, 4, options);

        // Assert
        first.Path.Should().Equal(second.Path);
        first.FrameDistance.Should().Be(second.FrameDistance);
    }

    [TestMethod]
    public void Recolor_WithSelectedSlider_AddsFrameSegmentsProportionalToSliderDuration()
    {
        // Arrange
        RgbaImage image = new(2, 1, [255, 255, 255, 255, 0, 0, 0, 255]);
        SliderPicturatorEngineOptions options = new() { Quality = 1 };
        long baseSegments = SliderPicturatorEngine.Recolor(image, options).SegmentCount;
        HitObject selectedSlider = new("256,192,100,2,0,L|270:192,1,14,0|0,0:0|0:0,0:0:0:0:")
        {
            TemporalLength = 4,
        };
        options.SelectedSlider = selectedSlider;

        // Act
        long shortDurationSegments = SliderPicturatorEngine.Recolor(image, options).SegmentCount;
        selectedSlider.TemporalLength = 8;
        long longDurationSegments = SliderPicturatorEngine.Recolor(image, options).SegmentCount;

        // Assert
        shortDurationSegments.Should().BeGreaterThan(baseSegments);
        (longDurationSegments - baseSegments).Should().Be(2 * (shortDurationSegments - baseSegments));
    }
}
