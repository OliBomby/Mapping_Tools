using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SliderCompletionator;
using Mapping_Tools.Core.Tools.SliderCompletionator.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.SliderCompletionator;

[TestClass]
public sealed class SliderCompletionatorEngineTests
{
    [TestMethod]
    public void Apply_WithDurationAndPreservedLength_RecalculatesVelocity()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorEngineOptions options = new()
        {
            Duration = 1,
            Length = -1,
            SliderVelocity = -1,
            FreeVariableSetting = SliderCompletionatorFreeVariable.Velocity,
        };

        // Act
        int completed = SliderCompletionatorEngine.Apply(beatmap, [slider], options);

        // Assert
        completed.Should().Be(1);
        slider.PixelLength.Should().Be(100);
        slider.SliderVelocity.Should().BeApproximately(-140, 0.0001);
        beatmap.BeatmapTiming.CalculateSliderTemporalLength(slider.Time, slider.PixelLength)
            .Should().BeApproximately(500, 0.0001);
    }

    [TestMethod]
    public void Apply_WithLengthAndMoveAnchors_UsesFullPathFraction()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorEngineOptions options = new()
        {
            Duration = -1,
            Length = 0.5,
            SliderVelocity = -1,
            MoveAnchors = true,
            FreeVariableSetting = SliderCompletionatorFreeVariable.Velocity,
        };

        // Act
        SliderCompletionatorEngine.Apply(beatmap, [slider], options);

        // Assert
        slider.PixelLength.Should().BeApproximately(50, 0.0001);
        slider.GetSliderPath(true).Distance.Should().BeApproximately(50, 0.0001);
    }

    [TestMethod]
    public void Apply_WithTruncatedCatmullSlider_ConvertsAndStoresNewPathType()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        slider.CurvePoints = [new Vector2(30, 80), new Vector2(100, 0)];
        slider.SliderType = PathType.Catmull;
        SliderCompletionatorEngineOptions options = new()
        {
            Length = 0.5,
            MoveAnchors = true,
            FreeVariableSetting = SliderCompletionatorFreeVariable.Velocity,
        };

        // Act
        int completed = SliderCompletionatorEngine.Apply(beatmap, [slider], options);

        // Assert
        completed.Should().Be(1);
        slider.SliderType.Should().Be(PathType.Bezier);
        slider.GetSliderPath(fullLength: true).Distance.Should().BeApproximately(slider.PixelLength, 1);
        slider.GetSliderPath().Type.Should().Be(PathType.Bezier);
    }

    [TestMethod]
    public void Apply_WithCurrentEditorTime_UsesEditorTimeForEndTime()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorEngineOptions options = new()
        {
            UseEndTime = true,
            UseCurrentEditorTime = true,
            EndTime = -1,
            Length = -1,
            SliderVelocity = -1,
            FreeVariableSetting = SliderCompletionatorFreeVariable.Velocity,
        };

        // Act
        SliderCompletionatorEngine.Apply(beatmap, [slider], options, 750);

        // Assert
        slider.SliderVelocity.Should().BeApproximately(-210, 0.0001);
    }

    [TestMethod]
    public void Apply_WithLengthAsFreeVariable_RecalculatesLength()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorEngineOptions options = new()
        {
            Duration = 1,
            Length = -1,
            SliderVelocity = 1,
            FreeVariableSetting = SliderCompletionatorFreeVariable.Length,
        };

        // Act
        SliderCompletionatorEngine.Apply(beatmap, [slider], options);

        // Assert
        slider.PixelLength.Should().BeApproximately(140, 0.0001);
        slider.SliderVelocity.Should().BeApproximately(-100, 0.0001);
    }

    [TestMethod]
    public void Apply_WithNonFiniteInput_ThrowsBeforeMutation()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorEngineOptions options = new() { Length = double.NaN };

        // Act
        Action act = () => SliderCompletionatorEngine.Apply(beatmap, [slider], options);

        // Assert
        act.Should().Throw<ArgumentException>();
        slider.PixelLength.Should().Be(100);
    }

    [TestMethod]
    public void Apply_WithZeroLengthAndVelocityFreeVariable_ThrowsBeforeMutation()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorEngineOptions options = new()
        {
            Length = 0,
            FreeVariableSetting = SliderCompletionatorFreeVariable.Velocity,
        };

        // Act
        Action act = () => SliderCompletionatorEngine.Apply(beatmap, [slider], options);

        // Assert
        act.Should().Throw<ArgumentException>();
        slider.PixelLength.Should().Be(100);
        slider.SliderVelocity.Should().Be(-100);
    }

    [TestMethod]
    public void Apply_WithDurationAsFreeVariable_UsesLengthAndVelocityToSetDuration()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorEngineOptions options = new()
        {
            Duration = 1,
            Length = 1,
            SliderVelocity = 1,
            FreeVariableSetting = SliderCompletionatorFreeVariable.Duration,
        };

        // Act
        int completed = SliderCompletionatorEngine.Apply(beatmap, [slider], options);

        // Assert
        completed.Should().Be(1);
        slider.PixelLength.Should().BeApproximately(100, 0.0001);
        slider.SliderVelocity.Should().BeApproximately(-100, 0.0001);
        beatmap.BeatmapTiming.CalculateSliderTemporalLength(slider.Time, slider.PixelLength)
            .Should().BeApproximately(100 * 100 * 500 / (10000 * 1.4), 0.0001);
    }

    [TestMethod]
    public void Apply_WithPreservedEndTimeSentinel_KeepsExistingSliderDuration()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        double originalDuration = beatmap.BeatmapTiming.CalculateSliderTemporalLength(slider.Time, slider.PixelLength);
        SliderCompletionatorEngineOptions options = new()
        {
            UseEndTime = true,
            EndTime = -1,
            Length = -1,
            SliderVelocity = -1,
            FreeVariableSetting = SliderCompletionatorFreeVariable.Velocity,
        };

        // Act
        SliderCompletionatorEngine.Apply(beatmap, [slider], options);

        // Assert
        beatmap.BeatmapTiming.CalculateSliderTemporalLength(slider.Time, slider.PixelLength)
            .Should().BeApproximately(originalDuration, 0.0001);
        slider.SliderVelocity.Should().BeApproximately(-100, 0.0001);
    }

    [TestMethod]
    public void Apply_WithCurrentEditorTimeModeAndMissingTime_ThrowsBeforeMutation()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorEngineOptions options = new()
        {
            UseEndTime = true,
            UseCurrentEditorTime = true,
            Length = -1,
            SliderVelocity = -1,
        };

        // Act
        Action act = () => SliderCompletionatorEngine.Apply(beatmap, [slider], options);

        // Assert
        act.Should().Throw<ArgumentException>();
        slider.PixelLength.Should().Be(100);
        slider.SliderVelocity.Should().Be(-100);
    }

    [TestMethod]
    public void Apply_WithEndTimeBeforeSliderStart_ThrowsBeforeMutation()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorEngineOptions options = new()
        {
            UseEndTime = true,
            UseCurrentEditorTime = true,
            Length = -1,
            SliderVelocity = -1,
        };

        // Act
        Action act = () => SliderCompletionatorEngine.Apply(beatmap, [slider], options, -1);

        // Assert
        act.Should().Throw<ArgumentException>();
        slider.PixelLength.Should().Be(100);
        slider.SliderVelocity.Should().Be(-100);
    }

    [TestMethod]
    public void Apply_WithCircleAndSlider_OnlyCountsAndChangesSlider()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        HitObject circle = new("256,64,250,1,2");
        double originalCircleTime = circle.Time;
        double originalCircleLength = circle.PixelLength;
        SliderCompletionatorEngineOptions options = new()
        {
            Duration = 1,
            Length = -1,
            SliderVelocity = -1,
            FreeVariableSetting = SliderCompletionatorFreeVariable.Velocity,
        };

        // Act
        int completed = SliderCompletionatorEngine.Apply(beatmap, [circle, slider], options);

        // Assert
        completed.Should().Be(1);
        circle.Time.Should().Be(originalCircleTime);
        circle.PixelLength.Should().Be(originalCircleLength);
        slider.SliderVelocity.Should().BeApproximately(-140, 0.0001);
    }

    private static (Beatmap Beatmap, HitObject Slider) CreateSliderBeatmap()
    {
        TimingPoint redline = new(
            0,
            500,
            4,
            SampleSet.Normal,
            0,
            100,
            true,
            false,
            false);
        HitObject slider = new("64,64,0,2,0,L|164:64,1,100");
        Beatmap beatmap = new([slider], [redline], redline);
        beatmap.BeatmapTiming.SliderMultiplier = 1.4;
        return (beatmap, slider);
    }
}
