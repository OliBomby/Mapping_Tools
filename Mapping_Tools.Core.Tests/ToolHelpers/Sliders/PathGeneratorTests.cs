using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.ToolHelpers.Sliders;

[TestClass]
public sealed class PathGeneratorTests
{
    [TestMethod]
    public void SetPath_RemovesConsecutiveDuplicatesAndInterpolatesPositionAndDistance()
    {
        // Arrange
        PathGenerator generator = new(
            [new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10)]);

        // Act
        Vector2 middlePosition = generator.GetContinuousPosition(1.5);
        double middleDistance = generator.GetContinuousDistance(1.5);
        double middleIndex = generator.GetIndexAtDistance(15);

        // Assert
        middlePosition.Should().Be(new Vector2(10, 5));
        middleDistance.Should().Be(15);
        middleIndex.Should().Be(1.5);
    }

    [TestMethod]
    public void GetIndexAtDistance_HandlesExactVertexAndInteriorDistance()
    {
        // Arrange
        PathGenerator generator = new([new Vector2(0, 0), new Vector2(3, 0), new Vector2(3, 4)]);

        // Act
        double exactVertex = generator.GetIndexAtDistance(3);
        double interior = generator.GetIndexAtDistance(5);

        // Assert
        exactVertex.Should().Be(1);
        interior.Should().Be(1.5);
    }

    [TestMethod]
    public void GetNonInflectionSegments_WithSameStartAndEnd_ReturnsThatSingleIndex()
    {
        // Arrange
        PathGenerator generator = new([new Vector2(0, 0), new Vector2(10, 0)]);

        // Act
        var segments = generator.GetNonInflectionSegments(0.5, 0.5);

        // Assert
        segments.Should().ContainSingle()
            .Which.Should().Be(Tuple.Create(0.5, 0.5));
    }

    [TestMethod]
    public void GetNonInflectionSegments_WithReversedRangeReversesForwardSegments()
    {
        // Arrange
        PathGenerator generator = new(
            [new Vector2(0.1, 0.1), new Vector2(1.1, 0.1), new Vector2(2.1, 1.1), new Vector2(3.1, 1.1), new Vector2(4.1, 2.1)]);

        // Act
        var forward = generator.GetNonInflectionSegments(0, 4, Math.PI / 3);
        var reversed = generator.GetNonInflectionSegments(4, 0, Math.PI / 3);
        var expectedReversed = forward.AsEnumerable()
            .Reverse()
            .Select(segment => Tuple.Create(segment.Item2, segment.Item1));

        // Assert
        forward.Should().NotBeEmpty();
        reversed.Should().Equal(expectedReversed);
    }

    [TestMethod]
    public void GeneratePath_ForStraightSamples_EmitsOnlyPathEndpoints()
    {
        // Arrange
        PathGenerator generator = new([new Vector2(0, 0), new Vector2(10, 0), new Vector2(20, 0)]);

        // Act
        Vector2[] anchors = generator.GeneratePath().ToArray();

        // Assert
        anchors.Should().Equal(new Vector2(0, 0), new Vector2(20, 0));
    }

    [DataTestMethod]
    [DataRow(PathGenerator.ApproximationMode.TangentIntersection)]
    [DataRow(PathGenerator.ApproximationMode.DoubleMiddle)]
    [DataRow(PathGenerator.ApproximationMode.Best)]
    public void GeneratePath_ApproximationMode_PreservesSampledEndpoints(PathGenerator.ApproximationMode mode)
    {
        // Arrange
        List<Vector2> samples =
        [
            new(0.1, 0.1),
            new(1.1, 0.1),
            new(2.1, 1.1),
            new(3.1, 1.1),
            new(4.1, 2.1),
        ];
        PathGenerator generator = new(samples);

        // Act
        Vector2[] anchors = generator.GeneratePath(0, samples.Count - 1, Math.PI, mode).ToArray();

        // Assert
        anchors.First().Should().Be(samples.First());
        anchors.Last().Should().Be(samples.Last());
        anchors.Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
    }

    [TestMethod]
    public void CalculatePathLength_WithRepeatedRedAnchor_SumsIndependentSegments()
    {
        // Arrange
        List<Vector2> anchors =
        [
            new(0, 0),
            new(10, 0),
            new(10, 0),
            new(10, 10),
        ];

        // Act
        double length = PathGenerator.CalculatePathLength(anchors);

        // Assert
        length.Should().BeApproximately(20, 0.001);
    }
}
