using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.SliderCompletionator;
using Mapping_Tools.Core.Tools.SliderCompletionator.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class SliderCompletionatorEngineTests
{
    [TestMethod]
    public void Apply_WithDurationAndPreservedLength_RecalculatesVelocity()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorOptions options = new()
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
        SliderCompletionatorOptions options = new()
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
    public void Apply_WithCurrentEditorTime_UsesEditorTimeForEndTime()
    {
        // Arrange
        var (beatmap, slider) = CreateSliderBeatmap();
        SliderCompletionatorOptions options = new()
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
        SliderCompletionatorOptions options = new()
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
        SliderCompletionatorOptions options = new() { Length = double.NaN };

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
        SliderCompletionatorOptions options = new()
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
