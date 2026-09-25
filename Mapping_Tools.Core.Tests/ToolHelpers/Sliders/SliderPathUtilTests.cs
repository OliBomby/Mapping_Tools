using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.ToolHelpers.Sliders;

[TestClass]
public sealed class SliderPathUtilTests
{
    [TestMethod]
    public void MoveAnchorsToLength_WithLongerLinearPath_ExtendsFinalAnchor()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(100, 0)];

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToLength(anchors, PathType.Linear, 150, out PathType pathType);

        // Assert
        pathType.Should().Be(PathType.Linear);
        moved.Should().Equal(new Vector2(0, 0), new Vector2(150, 0));
        anchors.Should().Equal(new Vector2(0, 0), new Vector2(100, 0));
    }

    [TestMethod]
    public void MoveAnchorsToLength_WithShorterPolyline_KeepsCompletedSegments()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(100, 0), new(100, 100)];

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToLength(anchors, PathType.Linear, 150, out PathType pathType);

        // Assert
        pathType.Should().Be(PathType.Linear);
        moved.Should().Equal(new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 50));
    }

    [TestMethod]
    public void MoveAnchorsToLength_WithUnchangedLength_ReturnsIndependentAnchorList()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(100, 0)];

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToLength(anchors, PathType.Linear, 100, out PathType pathType);

        // Assert
        pathType.Should().Be(PathType.Linear);
        moved.Should().Equal(anchors);
        moved.Should().NotBeSameAs(anchors);
    }

    [TestMethod]
    public void MoveAnchorsToLength_TruncatingTwoPointLinearPath_MovesItsOnlyEndpoint()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(100, 0)];

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToLength(anchors, PathType.Linear, 40, out PathType pathType);

        // Assert
        pathType.Should().Be(PathType.Linear);
        moved.Should().Equal(new Vector2(0, 0), new Vector2(40, 0));
    }

    [TestMethod]
    public void MoveAnchorsToCompletion_WithPolyline_ReturnsAnchorsAtRequestedFraction()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(100, 0), new(100, 100)];

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToCompletion(anchors, PathType.Linear, 0.75, out PathType pathType);

        // Assert
        pathType.Should().Be(PathType.Linear);
        moved.Should().Equal(new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 50));
    }

    [TestMethod]
    public void GetRedAnchorCompletions_WithRepeatedBezierAnchor_ReturnsSegmentBoundary()
    {
        // Arrange
        SliderPath path = new(PathType.Bezier,
            [new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 0), new Vector2(200, 0)]);

        // Act
        double[] completions = SliderPathUtil.GetRedAnchorCompletions(path).ToArray();

        // Assert
        completions.Should().ContainSingle();
        completions[0].Should().BeApproximately(0.5, 0.001);
    }

    [TestMethod]
    public void MoveAnchorsToLength_ExtendingBezierWithRepeatedEndpointReachesRequestedLengthWithoutMutatingSource()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(100, 0), new(100, 0)];

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToLength(anchors, PathType.Bezier, 150, out PathType pathType);

        // Assert
        pathType.Should().Be(PathType.Bezier);
        moved.Should().Equal(
            new Vector2(0, 0),
            new Vector2(101, 0),
            new Vector2(100, 0),
            new Vector2(100, 0),
            new Vector2(150, 0));
        anchors.Should().Equal(new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 0));
    }

    [DataTestMethod]
    [DataRow((int)PathType.Catmull)]
    [DataRow((int)PathType.PerfectCurve)]
    public void MoveAnchorsToLength_ExtendingCurvedPath_ConvertsToBezierAndPreservesRequestedEndpoint(int pathTypeValue)
    {
        // Arrange
        PathType sourceType = (PathType)pathTypeValue;
        List<Vector2> anchors = [new(0, 0), new(50, 50), new(100, 0)];
        SliderPath source = new(sourceType, [.. anchors]);
        double targetLength = source.Distance + 25;

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToLength(anchors, sourceType, targetLength, out PathType resultType);

        // Assert
        resultType.Should().Be(PathType.Bezier);
        Vector2.Distance(moved[0], anchors[0]).Should().BeLessThan(0.000001);
        Vector2.Distance(moved[^1], source.PositionAt(1) + (source.CalculatedPath[^1] - source.CalculatedPath[^2]).Normalized() * 25)
            .Should().BeLessThan(1);
    }

    [TestMethod]
    public void MoveAnchorsToLength_ExtendingBSplinePath_ConvertsToBezierAndReachesRequestedLength()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(30, 60), new(70, -20), new(100, 0)];
        SliderPath source = new(PathType.BSpline, [.. anchors]);
        double targetLength = source.Distance + 20;
        SliderPath requested = new(PathType.BSpline, [.. anchors], targetLength);

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToLength(anchors, PathType.BSpline, targetLength, out PathType resultType);

        // Assert
        resultType.Should().Be(PathType.Bezier);
        Vector2.Distance(moved[0], anchors[0]).Should().BeLessThan(0.000001);
        Vector2.Distance(moved[^1], requested.PositionAt(1)).Should().BeLessThan(0.000001);
        new SliderPath(resultType, [.. moved]).Distance.Should().BeApproximately(targetLength, 0.01);
    }

    [TestMethod]
    public void MoveAnchorsToLength_TruncatingBSplinePath_ConvertsToBezierAndReachesRequestedLength()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(30, 60), new(70, -20), new(100, 0)];
        SliderPath source = new(PathType.BSpline, [.. anchors]);
        double targetLength = source.Distance * 0.6;
        SliderPath truncated = new(PathType.BSpline, [.. anchors], targetLength);

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToLength(
            anchors, PathType.BSpline, source.Distance, targetLength, out PathType resultType);

        // Assert
        resultType.Should().Be(PathType.Bezier);
        moved[0].Should().Be(anchors[0]);
        Vector2.Distance(moved[^1], truncated.PositionAt(1)).Should().BeLessThan(0.001);
        new SliderPath(resultType, [.. moved]).Distance.Should().BeApproximately(targetLength, 0.01);
    }

    [TestMethod]
    public void MoveAnchorsToLength_TruncatingPerfectCurve_PreservesThreePointArcRepresentation()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(50, 50), new(100, 0)];
        SliderPath source = new(PathType.PerfectCurve, [.. anchors]);
        double targetLength = source.Distance / 2;
        SliderPath truncated = new(PathType.PerfectCurve, [.. anchors], targetLength);

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToLength(
            anchors, PathType.PerfectCurve, source.Distance, targetLength, out PathType resultType);

        // Assert
        resultType.Should().Be(PathType.PerfectCurve);
        moved.Should().HaveCount(3);
        moved[0].Should().Be(anchors[0]);
        Vector2.Distance(moved[1], truncated.PositionAt(0.5)).Should().BeLessThan(0.001);
        Vector2.Distance(moved[2], truncated.PositionAt(1)).Should().BeLessThan(0.001);
    }

    [TestMethod]
    public void MoveAnchorsToLength_TruncatingCatmullPath_ConvertsResultToBezier()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(30, 60), new(70, -20), new(100, 0)];
        SliderPath source = new(PathType.Catmull, [.. anchors]);
        double targetLength = source.Distance * 0.6;
        SliderPath truncated = new(PathType.Catmull, [.. anchors], targetLength);

        // Act
        List<Vector2> moved = SliderPathUtil.MoveAnchorsToLength(
            anchors, PathType.Catmull, source.Distance, targetLength, out PathType resultType);

        // Assert
        resultType.Should().Be(PathType.Bezier);
        moved[0].Should().Be(anchors[0]);
        Vector2.Distance(moved[^1], truncated.PositionAt(1)).Should().BeLessThan(1);
        new SliderPath(resultType, [.. moved]).Distance.Should().BeApproximately(targetLength, 1);
    }

    [TestMethod]
    public void ChopAnchors_ForLinearSlider_ReturnsOneSubdivisionPerEdge()
    {
        // Arrange
        SliderPath path = new(PathType.Linear,
            [new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10)]);

        // Act
        BezierSubdivision[] subdivisions = SliderPathUtil.ChopAnchors(path).ToArray();

        // Assert
        subdivisions.Should().HaveCount(2);
        subdivisions.Select(subdivision => subdivision.Length()).Should().Equal(10, 10);
    }

    [TestMethod]
    public void ChopAnchors_ForCatmullSlider_ReturnsOneLinearSubdivisionPerEdge()
    {
        // Arrange
        SliderPath path = new(PathType.Catmull,
            [new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10)]);

        // Act
        BezierSubdivision[] subdivisions = SliderPathUtil.ChopAnchors(path).ToArray();

        // Assert
        subdivisions.Should().HaveCount(2);
        subdivisions[0].Points.Should().Equal(new Vector2(0, 0), new Vector2(10, 0));
        subdivisions[1].Points.Should().Equal(new Vector2(10, 0), new Vector2(10, 10));
    }

    [TestMethod]
    public void ChopAnchors_WithRepeatedBezierBoundaries_ReturnsThreeIndependentSegments()
    {
        // Arrange
        List<Vector2> anchors = [
            new(0, 0), new(10, 0), new(10, 0),
            new(10, 10), new(10, 10), new(20, 10)];

        // Act
        BezierSubdivision[] subdivisions = SliderPathUtil.ChopAnchors(anchors).ToArray();

        // Assert
        subdivisions.Should().HaveCount(3);
        subdivisions[0].Points.Should().Equal(new Vector2(0, 0), new Vector2(10, 0));
        subdivisions[1].Points.Should().Equal(new Vector2(10, 0), new Vector2(10, 10));
        subdivisions[2].Points.Should().Equal(new Vector2(10, 10), new Vector2(20, 10));
    }

    [TestMethod]
    public void CalculateLoss_WithDegenerateAndClampedSegmentsMeasuresNearestDistance()
    {
        // Arrange
        Vector2[] points = [new(0, 0), new(2, 0), new(5, 0)];
        Vector2[] labels = [new(0, 0), new(0, 0), new(4, 0)];

        // Act
        double loss = SliderPathUtil.CalculateLoss(points, labels);

        // Assert
        loss.Should().BeApproximately(1d / 3, 0.0001);
    }
}
