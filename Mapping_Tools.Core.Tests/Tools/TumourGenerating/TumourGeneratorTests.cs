using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Core.Tools.TumourGenerating;
using Mapping_Tools.Core.Tools.TumourGenerating.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.TumourGenerating;

[TestClass]
public sealed class TumourGeneratorTests
{
    [TestMethod]
    public void PlaceTumour_DefaultTriangleAndOverlap_PreservesHintsAndReconstruction()
    {
        // Arrange
        const int resolution = 10;
        HitObject hitObject = new("0,0,384,2,0,B|192:0|192:0|192:192,1,384");
        var pathWithHints = PathHelper.CreatePathWithHints(hitObject.GetSliderPath());
        TumourGenerator generator = new() { Resolution = resolution };
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
        TumourGenerator leftGenerator = new();
        TumourGenerator rightGenerator = new();

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
            new TumourGenerator { TumourLayers = [layer] }
                .TumourGenerate(hitObject);
            return hitObject;
        }).ToList();

        // Assert
        generated.Should().HaveCount(3);
        generated.Should().OnlyContain(hitObject =>
            hitObject.IsSlider && double.IsFinite(hitObject.PixelLength) && hitObject.GetSliderPath().Distance > 0);
    }
}
