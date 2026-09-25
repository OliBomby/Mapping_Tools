using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.Sliderator;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.Sliderator;

[TestClass]
[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
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

    [DataTestMethod]
    [DataRow((int)PathType.Catmull)]
    [DataRow((int)PathType.BSpline)]
    public void Apply_WithConstantPositionGraph_PreservesSplineSourcePathType(int pathTypeValue)
    {
        // Arrange
        var (beatmap, source) = CreateSliderBeatmap();
        PathType pathType = (PathType)pathTypeValue;
        source.SliderType = pathType;
        var options = CreateOptions();
        options.ExportTime = 1000;
        options.NewVelocity = 1 / 4.2;
        options.GraphState = SlideratorEngineOptions.CreatePositionGraph(options.GraphBeats);

        // Act
        SlideratorApplyResult result = SlideratorEngine.Apply(beatmap, source, options);

        // Assert
        result.Simplified.Should().BeTrue();
        HitObject exported = beatmap.HitObjects[1];
        exported.SliderType.Should().Be(pathType);
        exported.GetAllCurvePoints().Should().Equal(source.GetAllCurvePoints());
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
    public void Invisiblate_WithCommaDecimalCulture_ReturnsFiniteFrameDistanceAndControlPoints()
    {
        // Arrange
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        Vector2[] sliderballPositions = Enumerable.Range(0, 101)
            .Select(index => new Vector2(64 + index, 64))
            .ToArray();

        // Act
        (Vector2[] controlPoints, double frameDistance) result;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            result = SliderInvisiblator.Invisiblate(100, sliderballPositions);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        // Assert
        result.frameDistance.Should().BeGreaterThan(1);
        result.controlPoints.Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
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

    [TestMethod]
    public void Apply_WithSliderOverride_ReplacesObjectAtExportTimeWithoutMutatingSource()
    {
        // Arrange
        var (beatmap, source) = CreateSliderBeatmap();
        var options = CreateOptions();
        options.ExportTime = source.Time;
        options.ExportModeSetting = SlideratorExportMode.Override;
        options.NewVelocity = 1 / 4.2;
        options.GraphState = SlideratorEngineOptions.CreatePositionGraph(options.GraphBeats);

        // Act
        var result = SlideratorEngine.Apply(beatmap, source, options);

        // Assert
        result.Simplified.Should().BeTrue();
        beatmap.HitObjects.Should().ContainSingle();
        beatmap.HitObjects[0].Should().NotBeSameAs(source);
        beatmap.HitObjects[0].IsSlider.Should().BeTrue();
        source.PixelLength.Should().Be(100);
    }

    [TestMethod]
    public void Apply_WithStreamOverride_ReplacesExistingObjectWithOrderedCircles()
    {
        // Arrange
        var (beatmap, source) = CreateSliderBeatmap();
        var options = CreateOptions();
        options.ExportTime = source.Time;
        options.ExportModeSetting = SlideratorExportMode.Override;
        options.ExportAsNormal = false;
        options.ExportAsStream = true;
        options.GraphBeats = 1;
        options.BeatsPerMinute = 600;
        options.GraphModeSetting = SlideratorGraphMode.Velocity;
        options.GraphState = new GraphState(
            [new GraphAnchor(new Vector2(0, 1)), new GraphAnchor(new Vector2(1, 2))],
            0,
            0,
            1,
            2);

        // Act
        var result = SlideratorEngine.Apply(beatmap, source, options);

        // Assert
        result.ObjectCount.Should().BeGreaterThan(1);
        beatmap.HitObjects.Should().NotContain(source);
        beatmap.HitObjects.Should().OnlyContain(hitObject => hitObject.IsCircle && !hitObject.IsSlider);
        beatmap.HitObjects.Select(hitObject => hitObject.Time).Should().BeInAscendingOrder();
    }

    [TestMethod]
    public void Validate_WithVelocityAboveLimit_AllowsNonNormalOutputButRejectsNormalSlider()
    {
        // Arrange
        var (_, source) = CreateSliderBeatmap();
        var normalOptions = CreateOptions();
        normalOptions.NewVelocity = 2;
        normalOptions.VelocityLimit = 1;
        var nonNormalOptions = CreateOptions();
        nonNormalOptions.NewVelocity = 2;
        nonNormalOptions.VelocityLimit = 1;
        nonNormalOptions.ExportAsNormal = false;

        // Act
        Action validateNormal = () => SlideratorEngine.Validate(normalOptions, source);
        Action validateNonNormal = () => SlideratorEngine.Validate(nonNormalOptions, source);

        // Assert
        validateNormal.Should().Throw<ArgumentException>();
        validateNonNormal.Should().NotThrow();
    }

    [TestMethod]
    public void Validate_WithCircleSource_ThrowsInvalidOperationException()
    {
        // Arrange
        HitObject circle = new("64,64,0,1,0");

        // Act
        Action act = () => SlideratorEngine.Validate(CreateOptions(), circle);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void GetMaximumVelocity_UsesPositionSlopeOrVelocityGraphExtremes()
    {
        // Arrange
        SlideratorEngineOptions positionOptions = CreateOptions();
        positionOptions.GraphBeats = 2;
        positionOptions.GraphState = SlideratorEngineOptions.CreatePositionGraph(2);
        SlideratorEngineOptions velocityOptions = CreateOptions();
        velocityOptions.GraphModeSetting = SlideratorGraphMode.Velocity;
        velocityOptions.GraphState = new GraphState(
            [new GraphAnchor(new Vector2(0, 1)), new GraphAnchor(new Vector2(1, 2))],
            0,
            0,
            1,
            2);

        // Act
        double positionMaximum = SlideratorEngine.GetMaximumVelocity(positionOptions);
        double velocityMaximum = SlideratorEngine.GetMaximumVelocity(velocityOptions);

        // Assert
        positionMaximum.Should().BeApproximately(0.5 / 1.4, 0.0001);
        velocityMaximum.Should().Be(2);
    }

    [DataTestMethod]
    [DataRow("BeatsPerMinute", 0d)]
    [DataRow("GraphBeats", 0d)]
    [DataRow("GlobalSv", 0d)]
    [DataRow("PixelLength", 0d)]
    [DataRow("MinDendrite", 0d)]
    [DataRow("NewVelocity", double.NaN)]
    [DataRow("BeatSnapDivisor", 0d)]
    [DataRow("BeatSnapDivisor", 17d)]
    public void Validate_WithIllegalNumericSetting_ThrowsArgumentException(string setting, double value)
    {
        // Arrange
        var (_, source) = CreateSliderBeatmap();
        SlideratorEngineOptions options = CreateOptions();
        switch (setting)
        {
            case "BeatsPerMinute":
                options.BeatsPerMinute = value;
                break;
            case "GraphBeats":
                options.GraphBeats = value;
                break;
            case "GlobalSv":
                options.GlobalSv = value;
                break;
            case "PixelLength":
                options.PixelLength = value;
                break;
            case "MinDendrite":
                options.MinDendrite = value;
                break;
            case "NewVelocity":
                options.NewVelocity = value;
                break;
            case "BeatSnapDivisor":
                options.BeatSnapDivisor = (int)value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(setting));
        }

        // Act
        Action act = () => SlideratorEngine.Validate(options, source);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Validate_WithNegativeGraphCompletion_RejectsPathBeforeGeneration()
    {
        // Arrange
        var (_, source) = CreateSliderBeatmap();
        SlideratorEngineOptions options = CreateOptions();
        options.GraphState = new GraphState(
            [new GraphAnchor(new Vector2(0, -0.5)), new GraphAnchor(new Vector2(3, 1))],
            0,
            -0.5,
            3,
            1);

        // Act
        Action act = () => SlideratorEngine.Validate(options, source);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Negative position*");
    }

    [TestMethod]
    public void Invisiblate_WithNoMovement_ReturnsFiniteOutputAndRoundsInputInPlace()
    {
        // Arrange
        Vector2[] sliderballPositions = [new(64.4, 64.6), new(64.4, 64.6), new(64.4, 64.6)];

        // Act
        (Vector2[] controlPoints, double frameDistance) result = SliderInvisiblator.Invisiblate(2, sliderballPositions);

        // Assert
        double.IsFinite(result.frameDistance).Should().BeTrue();
        result.frameDistance.Should().BeGreaterThan(0);
        result.controlPoints.Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
        sliderballPositions.Should().OnlyContain(point =>
            Precision.AlmostEquals(point.X, Math.Round(point.X)) && Precision.AlmostEquals(point.Y, Math.Round(point.Y)));
        result.controlPoints[^1].Should().Be(sliderballPositions[^1]);
    }

    [TestMethod]
    public void Invisiblate_WithInsufficientPositions_ThrowsArgumentException()
    {
        // Arrange
        Vector2[] sliderballPositions = [new(64, 64)];

        // Act
        Action act = () => SliderInvisiblator.Invisiblate(1, sliderballPositions);

        // Assert
        act.Should().Throw<ArgumentException>();
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
