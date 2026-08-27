using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.Sliderator;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.Sliderator;

[TestClass]
public sealed class SlideratorEngineTests
{
    [TestMethod]
    public void Apply_WithConstantPositionGraph_ReusesSourceShapeAndExportsOneSlider()
    {
        // Arrange
        var (beatmap, source) = CreateSliderBeatmap();
        var options = CreateOptions();
        options.ExportTime = 1000;
        options.NewVelocity = 1 / 4.2;
        options.GraphState = SlideratorEngineOptions.CreatePositionGraph(options.GraphBeats);

        // Act
        var result = SlideratorEngine.Apply(beatmap, source, options);

        // Assert
        result.Simplified.Should().BeTrue();
        result.ObjectCount.Should().Be(1);
        beatmap.HitObjects.Count.Should().Be(2);
        beatmap.HitObjects[1].IsSlider.Should().BeTrue();
        beatmap.HitObjects[1].GetAllCurvePoints().Should().Equal(source.GetAllCurvePoints());
    }

    [TestMethod]
    public void Apply_WithVelocityGraphAndStreamOutput_ExportsVariableDensityCircles()
    {
        // Arrange
        var (beatmap, source) = CreateSliderBeatmap();
        var options = CreateOptions();
        options.GraphBeats = 1;
        options.BeatsPerMinute = 600;
        options.ExportTime = 1000;
        options.ExportAsNormal = false;
        options.ExportAsStream = true;
        options.GraphModeSetting = SlideratorGraphMode.Velocity;
        options.GraphState = new GraphState(
            [
                new GraphAnchor(new Vector2(0, 1)),
                new GraphAnchor(new Vector2(1, 2)),
            ],
            0,
            0,
            1,
            2);

        // Act
        var result = SlideratorEngine.Apply(beatmap, source, options);

        // Assert
        result.Simplified.Should().BeFalse();
        result.ObjectCount.Should().BeGreaterThan(1);
        beatmap.HitObjects.Skip(1).Should().OnlyContain(hitObject => hitObject.IsCircle);
        beatmap.HitObjects.Skip(1).Select(hitObject => hitObject.Time).Should().BeInAscendingOrder();
    }

    [TestMethod]
    public void Invisiblate_WithOneMillisecond_ReturnsStableControlPoints()
    {
        // Arrange
        Vector2[] sliderballPositions = [new(64, 64), new(65, 64)];

        // Act
        (var controlPoints, double frameDistance) = SliderInvisiblator.Invisiblate(
            1,
            sliderballPositions);

        // Assert
        controlPoints.Length.Should().BeGreaterThan(2);
        frameDistance.Should().BeGreaterThan(0);
        controlPoints[^1].Should().Be(sliderballPositions[^1]);
    }

    [TestMethod]
    public void Apply_WithInvisibleOutput_DelegatesVelocityToTimingPoints()
    {
        // Arrange
        var (beatmap, source) = CreateSliderBeatmap();
        var options = CreateOptions();
        options.ExportTime = 1000;
        options.GraphBeats = 1;
        options.BeatsPerMinute = 600;
        options.ExportAsNormal = false;
        options.ExportAsInvisibleSlider = true;
        options.GraphState = SlideratorEngineOptions.CreatePositionGraph(options.GraphBeats);

        // Act
        var result = SlideratorEngine.Apply(beatmap, source, options);

        // Assert
        result.Simplified.Should().BeFalse();
        beatmap.HitObjects.Skip(1).Should().ContainSingle();
        beatmap.HitObjects.Skip(1).Single().SliderVelocity.Should().Be(double.NaN);
    }

    [TestMethod]
    public void Apply_WhenCancellationIsRequestedBeforeGeneration_LeavesBeatmapUnchanged()
    {
        // Arrange
        var (beatmap, source) = CreateSliderBeatmap();
        var options = CreateOptions();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Action act = () => SlideratorEngine.Apply(beatmap, source, options, cancellationToken: cancellation.Token);

        // Assert
        act.Should().Throw<OperationCanceledException>();
        beatmap.HitObjects.Should().ContainSingle();
    }

    private static SlideratorEngineOptions CreateOptions()
    {
        return new SlideratorEngineOptions
        {
            GlobalSv = 1.4,
            GraphBeats = 3,
            BeatsPerMinute = 180,
            PixelLength = 100,
            BeatSnapDivisor = 4,
            VelocityLimit = 10,
            MinDendrite = 2,
            ExportAsNormal = true,
            ExportModeSetting = SlideratorExportMode.Add,
            GraphModeSetting = SlideratorGraphMode.Position,
        };
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
