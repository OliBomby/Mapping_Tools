using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Core.Tools.TumourGenerator;
using Mapping_Tools.Core.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.Tools.TumourGenerator.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.TumourGenerator;

[TestClass]
public sealed class TumourGeneratorEngineTests
{
    [TestMethod]
    public void PlaceTumour_DefaultTriangleAndOverlap_PreservesHintsAndReconstruction()
    {
        // Arrange
        const int resolution = 10;
        HitObject hitObject = new("0,0,384,2,0,B|192:0|192:0|192:192,1,384");
        var pathWithHints = PathHelper.CreatePathWithHints(hitObject.GetSliderPath());
        TumourGeneratorEngine generator = new() { Resolution = resolution };
        var layer = TumourLayer.GetDefaultLayer();
        layer.TumourLength = TumourLayer.GetGraphState(10);
        layer.TumourScale = TumourLayer.GetGraphState(5);
        var sourcePath = pathWithHints.Path;
        var start = PathHelper.FindFirstOccurrenceExact(sourcePath.First!, 100, epsilon: 0.5);
        var end = PathHelper.FindLastOccurrenceExact(start, 110, epsilon: 0.5);
        var end2 = PathHelper.FindLastOccurrenceExact(start, 115, epsilon: 0.5);

        // Act
        generator.PlaceTumour(pathWithHints, layer, 0, start, end, 0, 1, 100, 110, false, 384);
        for (var currentPoint = start; currentPoint is not null && currentPoint != end; currentPoint = currentPoint.Next)
        {
            var position = currentPoint.Value.Pos;
            if (position.X is >= 100 and <= 105)
                position.Y.Should().BeApproximately(-position.X + 100, Precision.DOUBLE_EPSILON);
            else if (position.X is > 105 and <= 110) position.Y.Should().BeApproximately(position.X - 110, Precision.DOUBLE_EPSILON);
        }

        pathWithHints.ReconstructionHints.Should().HaveCount(4);
        var middle = PathHelper.FindFirstOccurrence(start, 105);
        middle.Value.CumulativeLength.Should().BeApproximately(105, Precision.DOUBLE_EPSILON);
        generator.PlaceTumour(pathWithHints, layer, 0, middle, end2, 0, 1, 105, 115, false, 384);
        for (var currentPoint = start; currentPoint is not null && currentPoint != end2; currentPoint = currentPoint.Next)
        {
            var position = currentPoint.Value.Pos;
            if (position.X is >= 100 and <= 105)
                position.Y.Should().BeApproximately(-position.X + 100, Precision.DOUBLE_EPSILON);
            else if (position.X is > 105 and <= 110)
                position.Y.Should().BeApproximately(-5, Precision.DOUBLE_EPSILON);
            else if (position.X is > 110 and <= 115) position.Y.Should().BeApproximately(position.X - 115, Precision.DOUBLE_EPSILON);
        }

        pathWithHints.ReconstructionHints[0].Layer.Should().Be(-1);
        pathWithHints.ReconstructionHints[1].Layer.Should().Be(0);
        pathWithHints.ReconstructionHints[1].Anchors.Should().NotBeNull();
        pathWithHints.ReconstructionHints[2].Layer.Should().Be(0);
        pathWithHints.ReconstructionHints[2].Anchors.Should().BeNull();
        pathWithHints.ReconstructionHints[3].Layer.Should().Be(0);
        pathWithHints.ReconstructionHints[3].Anchors.Should().NotBeNull();
        pathWithHints.ReconstructionHints[4].Layer.Should().Be(-1);
        pathWithHints.ReconstructionHints[5].Layer.Should().Be(-1);
        var (anchors, pathType) = new Reconstructor().Reconstruct(pathWithHints);

        // Assert
        pathWithHints.ReconstructionHints.Should().HaveCount(6);
        pathWithHints.ReconstructionHints[1].Anchors.Should().NotBeNull();
        pathWithHints.ReconstructionHints[2].Anchors.Should().BeNull();
        pathWithHints.ReconstructionHints[3].Anchors.Should().NotBeNull();
        pathType.Should().Be(PathType.Bezier);
        anchors.Should().Equal(
            new Vector2(0, 0),
            new Vector2(100, 0),
            new Vector2(100, 0),
            new Vector2(105, -5),
            new Vector2(105, -5),
            new Vector2(110, -5),
            new Vector2(110, -5),
            new Vector2(115, 0),
            new Vector2(115, 0),
            new Vector2(192, 0),
            new Vector2(192, 0),
            new Vector2(192, 192));
    }

    [TestMethod]
    public void TumourGenerate_SidednessAndWrapping_ChangesGeneratedPathWithoutChangingInput()
    {
        // Arrange
        HitObject leftInput = new("0,0,0,2,0,L|256:0,1,256");
        var rightInput = leftInput.DeepCopy();
        var leftLayer = TumourLayer.GetDefaultLayer();
        leftLayer.TumourCount = 1;
        leftLayer.TumourStart = 0.25;
        leftLayer.TumourEnd = 0.75;
        var rightLayer = leftLayer.Copy();
        rightLayer.TumourSidedness = TumourSidedness.Right;
        var leftPath = PathHelper.CreatePathWithHints(leftInput.GetSliderPath());
        var rightPath = PathHelper.CreatePathWithHints(rightInput.GetSliderPath());
        TumourGeneratorEngine leftGenerator = new();
        TumourGeneratorEngine rightGenerator = new();

        // Act
        leftGenerator.PlaceTumour(
            leftPath,
            leftLayer,
            0,
            leftPath.Path.First!,
            leftPath.Path.Last!,
            0,
            1,
            0,
            256,
            false,
            256);
        rightGenerator.PlaceTumour(
            rightPath,
            rightLayer,
            0,
            rightPath.Path.First!,
            rightPath.Path.Last!,
            0,
            1,
            0,
            256,
            true,
            256);

        // Assert
        leftPath.Path.Select(point => point.Pos).Should().NotBeEquivalentTo(
            rightPath.Path.Select(point => point.Pos));
        leftPath.ReconstructionHints.Should().NotBeEmpty();
        rightPath.ReconstructionHints.Should().NotBeEmpty();
    }

    [TestMethod]
    public void TumourGenerate_EachWrappingMode_ProducesFiniteSliderGeometry()
    {
        // Arrange
        var wrappingModes = Enum.GetValues<WrappingMode>();

        // Act
        var generated = wrappingModes.Select(wrappingMode =>
        {
            HitObject hitObject = new("0,0,0,2,0,B|128:0|128:128,1,256");
            var layer = TumourLayer.GetDefaultLayer();
            layer.WrappingMode = wrappingMode;
            layer.TumourCount = 1;
            layer.TumourStart = 0.2;
            layer.TumourEnd = 0.8;
            new TumourGeneratorEngine { TumourLayers = [layer] }
                .TumourGenerate(hitObject);
            return hitObject;
        }).ToList();

        // Assert
        generated.Should().HaveCount(3);
        generated.Should().OnlyContain(hitObject =>
            hitObject.IsSlider && double.IsFinite(hitObject.PixelLength) && hitObject.GetSliderPath().Distance > 0);
    }

    [TestMethod]
    public void Validate_WithMissingLayers_ThrowsValidationException()
    {
        // Arrange
        TumourGeneratorEngineOptions options = new();

        // Act
        var act = () => TumourGeneratorEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least one layer*");
    }

    [TestMethod]
    public void Validate_WithNonFiniteGraphAnchor_ThrowsValidationException()
    {
        // Arrange
        var layer = TumourLayer.GetDefaultLayer();
        layer.TumourScale.Anchors[0].Pos = new Vector2(double.NaN, 0);
        TumourGeneratorEngineOptions options = new() { TumourLayers = [layer] };

        // Act
        var act = () => TumourGeneratorEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*graph anchor*");
    }

    [TestMethod]
    public void Validate_WithDefaultOptions_DoesNotThrow()
    {
        // Arrange
        TumourGeneratorEngineOptions options = new()
        {
            TumourLayers = [TumourLayer.GetDefaultLayer()],
        };

        // Act
        var act = () => TumourGeneratorEngine.Validate(options);

        // Assert
        act.Should().NotThrow();
    }

    [DataTestMethod]
    [DataRow(-1d)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    public void Validate_WithNegativeOrNonFiniteScale_ThrowsValidationException(double scale)
    {
        // Arrange
        TumourGeneratorEngineOptions options = new()
        {
            Scale = scale,
            TumourLayers = [TumourLayer.GetDefaultLayer()],
        };

        // Act
        Action act = () => TumourGeneratorEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*scale must be finite and non-negative*");
    }

    [DataTestMethod]
    [DataRow("Template")]
    [DataRow("Wrapping")]
    [DataRow("Sidedness")]
    public void Validate_WithUnknownLayerMode_ThrowsValidationException(string mode)
    {
        // Arrange
        var layer = TumourLayer.GetDefaultLayer();
        switch (mode)
        {
            case "Template":
                layer.TumourTemplateEnum = (TumourTemplate)int.MaxValue;
                break;
            case "Wrapping":
                layer.WrappingMode = (WrappingMode)int.MaxValue;
                break;
            case "Sidedness":
                layer.TumourSidedness = (TumourSidedness)int.MaxValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        TumourGeneratorEngineOptions options = new() { TumourLayers = [layer] };

        // Act
        Action act = () => TumourGeneratorEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*unknown layer mode*");
    }

    [DataTestMethod]
    [DataRow("Count")]
    [DataRow("Start")]
    [DataRow("End")]
    public void Validate_WithInvalidLayerRange_ThrowsValidationException(string range)
    {
        // Arrange
        var layer = TumourLayer.GetDefaultLayer();
        switch (range)
        {
            case "Count":
                layer.TumourCount = -1;
                break;
            case "Start":
                layer.TumourStart = double.NaN;
                break;
            case "End":
                layer.TumourEnd = double.PositiveInfinity;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(range));
        }

        TumourGeneratorEngineOptions options = new() { TumourLayers = [layer] };

        // Act
        Action act = () => TumourGeneratorEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*invalid layer range*");
    }

    [DataTestMethod]
    [DataRow("X")]
    [DataRow("Y")]
    public void Validate_WithReversedGraphBounds_ThrowsValidationException(string axis)
    {
        // Arrange
        var layer = TumourLayer.GetDefaultLayer();
        layer.TumourLength = axis == "X"
            ? new GraphState([], 1, 0, 0, 1)
            : new GraphState([], 0, 1, 1, 0);
        TumourGeneratorEngineOptions options = new() { TumourLayers = [layer] };

        // Act
        Action act = () => TumourGeneratorEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*invalid graph*");
    }

    [DataTestMethod]
    [DataRow("Descending")]
    [DataRow("NonFiniteTension")]
    public void Validate_WithInvalidGraphAnchor_ThrowsValidationException(string failure)
    {
        // Arrange
        var layer = TumourLayer.GetDefaultLayer();
        if (failure == "Descending")
        {
            layer.TumourScale = new GraphState(
                [new GraphAnchor(new Vector2(1, 0)), new GraphAnchor(new Vector2(0, 1))],
                0,
                0,
                1,
                1);
        }
        else
        {
            layer.TumourScale.Anchors[0].Tension = double.PositiveInfinity;
        }

        TumourGeneratorEngineOptions options = new() { TumourLayers = [layer] };

        // Act
        Action act = () => TumourGeneratorEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*invalid graph anchor*");
    }

    [TestMethod]
    public void Validate_WithNullLayerOrGraph_ThrowsArgumentNullException()
    {
        // Arrange
        TumourGeneratorEngineOptions nullLayerOptions = new() { TumourLayers = [null!] };
        var layer = TumourLayer.GetDefaultLayer();
        layer.TumourDistance = null!;
        TumourGeneratorEngineOptions nullGraphOptions = new() { TumourLayers = [layer] };

        // Act
        Action nullLayer = () => TumourGeneratorEngine.Validate(nullLayerOptions);
        Action nullGraph = () => TumourGeneratorEngine.Validate(nullGraphOptions);

        // Assert
        nullLayer.Should().Throw<ArgumentNullException>();
        nullGraph.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void TumourGenerate_WithCircleOrNoLayers_ReturnsFalseWithoutChangingInput()
    {
        // Arrange
        HitObject circle = new("64,64,0,1,2");
        HitObject slider = new("0,0,0,2,0,L|256:0,1,256");
        string circleLine = circle.GetLine();
        string sliderLine = slider.GetLine();

        // Act
        bool circleGenerated = new TumourGeneratorEngine { TumourLayers = [TumourLayer.GetDefaultLayer()] }
            .TumourGenerate(circle);
        bool sliderGenerated = new TumourGeneratorEngine().TumourGenerate(slider);

        // Assert
        circleGenerated.Should().BeFalse();
        sliderGenerated.Should().BeFalse();
        circle.GetLine().Should().Be(circleLine);
        slider.GetLine().Should().Be(sliderLine);
    }

    [TestMethod]
    public void TumourGenerate_WhenCancelledBeforePlacement_LeavesSliderUnchanged()
    {
        // Arrange
        HitObject slider = new("0,0,0,2,0,L|256:0,1,256");
        string originalLine = slider.GetLine();
        TumourGeneratorEngine generator = new() { TumourLayers = [TumourLayer.GetDefaultLayer()] };
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Action act = () => generator.TumourGenerate(slider, cancellation.Token);

        // Assert
        act.Should().Throw<OperationCanceledException>();
        slider.GetLine().Should().Be(originalLine);
    }

    [TestMethod]
    public void TumourGenerate_WithRelativeRangeAndMiddleAnchors_RecordsLengthAndReconstructsLinearPath()
    {
        // Arrange
        HitObject slider = new("0,0,0,2,0,L|256:0,1,256");
        var layer = TumourLayer.GetDefaultLayer();
        layer.UseAbsoluteRange = false;
        layer.TumourStart = 0.25;
        layer.TumourEnd = 0.75;
        layer.TumourCount = 1;
        TumourGeneratorEngine generator = new()
        {
            JustMiddleAnchors = true,
            TumourLayers = [layer],
        };

        // Act
        bool generated = generator.TumourGenerate(slider);

        // Assert
        generated.Should().BeTrue();
        generator.LayerLengths.Should().ContainSingle().Which.Should().BeApproximately(256, 0.01);
        slider.SliderType.Should().Be(PathType.Linear);
        slider.GetSliderPath().Distance.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void TumourGenerate_WithInactiveLayer_LeavesLayerLengthListEmpty()
    {
        // Arrange
        HitObject slider = new("0,0,0,2,0,L|256:0,1,256");
        var inactiveLayer = TumourLayer.GetDefaultLayer();
        inactiveLayer.IsActive = false;
        TumourGeneratorEngine generator = new() { TumourLayers = [inactiveLayer] };

        // Act
        bool generated = generator.TumourGenerate(slider);

        // Assert
        generated.Should().BeTrue();
        generator.LayerLengths.Should().BeEmpty();
        slider.GetSliderPath().Distance.Should().BeGreaterThan(0);
    }

    [DataTestMethod]
    [DataRow(TumourTemplate.Triangle)]
    [DataRow(TumourTemplate.Square)]
    [DataRow(TumourTemplate.Circle)]
    [DataRow(TumourTemplate.Parabola)]
    public void TumourGenerate_WithEachTemplate_ProducesFiniteSliderPath(TumourTemplate template)
    {
        // Arrange
        HitObject slider = new("0,0,0,2,0,L|256:0,1,256");
        var layer = TumourLayer.GetDefaultLayer();
        layer.TumourTemplateEnum = template;
        layer.TumourCount = 1;
        layer.TumourStart = 64;
        layer.TumourEnd = 192;
        layer.TumourParameter = TumourLayer.GetGraphState(10);
        TumourGeneratorEngine generator = new() { TumourLayers = [layer] };

        // Act
        bool generated = generator.TumourGenerate(slider);

        // Assert
        generated.Should().BeTrue();
        slider.GetSliderPath().Distance.Should().BeGreaterThan(0);
        slider.GetAllCurvePoints().Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
    }

    [TestMethod]
    public void TumourGenerate_WithRandomSidednessAndSeed_MatchesSeededSideSelection()
    {
        // Arrange
        const int seed = 712;
        bool randomChoosesRight = new Random(seed).NextDouble() < 0.5;
        HitObject randomSlider = new("0,0,0,2,0,L|256:0,1,256");
        HitObject expectedSlider = new("0,0,0,2,0,L|256:0,1,256");
        var randomLayer = TumourLayer.GetDefaultLayer();
        randomLayer.TumourSidedness = TumourSidedness.Random;
        randomLayer.RandomSeed = seed;
        randomLayer.TumourCount = 1;
        randomLayer.TumourStart = 64;
        randomLayer.TumourEnd = 192;
        var expectedLayer = randomLayer.Copy();
        expectedLayer.TumourSidedness = randomChoosesRight ? TumourSidedness.Right : TumourSidedness.Left;

        // Act
        new TumourGeneratorEngine { TumourLayers = [randomLayer] }.TumourGenerate(randomSlider);
        new TumourGeneratorEngine { TumourLayers = [expectedLayer] }.TumourGenerate(expectedSlider);

        // Assert
        randomSlider.GetAllCurvePoints().Should().Equal(expectedSlider.GetAllCurvePoints());
    }
}
