using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.ToolHelpers.Sliders;

[TestClass]
public sealed class BezierConverterTests
{
    [TestMethod]
    public void ConvertToBezier_WithLinearPath_PreservesRequestedLengthAndEndpoints()
    {
        // Arrange
        Vector2[] controlPoints = [new(0, 0), new(100, 0), new(100, 100)];
        SliderPath source = new(PathType.Linear, controlPoints, 150);

        // Act
        SliderPath converted = BezierConverter.ConvertToBezier(source);

        // Assert
        converted.Type.Should().Be(PathType.Bezier);
        converted.ExpectedDistance.Should().Be(150);
        converted.Distance.Should().Be(150);
        Vector2.Distance(converted.PositionAt(0), controlPoints[0]).Should().BeLessThan(0.000001);
        converted.PositionAt(1).Should().Be(source.PositionAt(1));
        converted.ControlPoints.Should().Equal(new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 0), new Vector2(100, 100));
    }

    [TestMethod]
    public void ConvertToBezier_WithPerfectCurve_PreservesRequestedLengthAndEndpoints()
    {
        // Arrange
        Vector2[] controlPoints = [new(0, 0), new(50, 50), new(100, 0)];
        SliderPath source = new(PathType.PerfectCurve, controlPoints, 100);

        // Act
        SliderPath converted = BezierConverter.ConvertToBezier(source);

        // Assert
        converted.Type.Should().Be(PathType.Bezier);
        converted.ExpectedDistance.Should().Be(100);
        converted.Distance.Should().Be(100);
        Vector2.Distance(converted.PositionAt(0), controlPoints[0]).Should().BeLessThan(0.000001);
        Vector2.Distance(converted.PositionAt(1), source.PositionAt(1)).Should().BeLessThan(1);
    }

    [TestMethod]
    public void ConvertToBezier_WithCatmullPath_PreservesRequestedLengthAndEndpoints()
    {
        // Arrange
        Vector2[] controlPoints = [new(0, 0), new(30, 60), new(70, -20), new(100, 0)];
        SliderPath source = new(PathType.Catmull, controlPoints, 125);

        // Act
        SliderPath converted = BezierConverter.ConvertToBezier(source);

        // Assert
        converted.Type.Should().Be(PathType.Bezier);
        converted.ExpectedDistance.Should().Be(125);
        converted.Distance.Should().Be(125);
        converted.PositionAt(0).Should().Be(controlPoints[0]);
        Vector2.Distance(converted.PositionAt(1), source.PositionAt(1)).Should().BeLessThan(1);
    }

    [TestMethod]
    public void ConvertToBezier_WithBezierPath_ReturnsTheSamePathValue()
    {
        // Arrange
        SliderPath source = new(PathType.Bezier, [new Vector2(0, 0), new Vector2(20, 40), new Vector2(50, 0)], 60);

        // Act
        SliderPath converted = BezierConverter.ConvertToBezier(source);

        // Assert
        converted.Should().Be(source);
    }

    [TestMethod]
    public void ConvertToBezier_WithBSplinePath_ConvertsItsApproximationAndPreservesRequestedLength()
    {
        // Arrange
        Vector2[] anchors = [new(0, 0), new(20, 40), new(50, 0), new(80, 30)];
        SliderPath full = new(PathType.BSpline, anchors);
        SliderPath source = new(PathType.BSpline, anchors, full.Distance * 0.75);

        // Act
        SliderPath converted = BezierConverter.ConvertToBezier(source);

        // Assert
        converted.Type.Should().Be(PathType.Bezier);
        converted.ExpectedDistance.Should().Be(source.ExpectedDistance);
        converted.Distance.Should().BeApproximately(source.Distance, 0.001);
        converted.ControlPoints.Count.Should().BeGreaterThan(anchors.Length);
        Vector2.Distance(converted.PositionAt(0), full.PositionAt(0)).Should().BeLessThan(0.001);
        Vector2.Distance(converted.PositionAt(1), source.PositionAt(1)).Should().BeLessThan(0.001);
    }

    [TestMethod]
    public void ConvertToBezierAnchors_WithBSplinePath_ProducesLengthEquivalentBezierAnchors()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(20, 40), new(50, 0), new(80, 30)];
        SliderPath source = new(PathType.BSpline, [.. anchors]);

        // Act
        List<Vector2> converted = BezierConverter.ConvertToBezierAnchors(anchors, PathType.BSpline);
        SliderPath convertedPath = new(PathType.Bezier, [.. converted]);

        // Assert
        converted.Count.Should().BeGreaterThan(anchors.Count);
        convertedPath.Distance.Should().BeApproximately(source.Distance, 0.001);
        Vector2.Distance(converted[0], anchors[0]).Should().BeLessThan(0.001);
        Vector2.Distance(converted[^1], anchors[^1]).Should().BeLessThan(0.001);
    }

    [TestMethod]
    public void ConvertCircleToBezierAnchors_WithUnstableCircle_ReturnsOriginalAnchors()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(50, 0), new(100, 0)];

        // Act
        List<Vector2> converted = BezierConverter.ConvertCircleToBezierAnchors(anchors);

        // Assert
        converted.Should().BeSameAs(anchors);
    }

    [TestMethod]
    public void ConvertCircleToBezierAnchors_WithStableArc_PreservesArcEndpoints()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(50, 50), new(100, 0)];

        // Act
        List<Vector2> converted = BezierConverter.ConvertCircleToBezierAnchors(anchors);

        // Assert
        converted.Should().HaveCountGreaterThan(3);
        Vector2.Distance(converted[0], anchors[0]).Should().BeLessThan(0.02);
        Vector2.Distance(converted[^1], anchors[^1]).Should().BeLessThan(0.02);
    }

    [DataTestMethod]
    [DataRow(35)]
    [DataRow(90)]
    [DataRow(150)]
    [DataRow(240)]
    public void ConvertCircleToBezierAnchors_WithDifferentArcSweeps_PreservesEndpoints(int sweepDegrees)
    {
        // Arrange
        double sweep = sweepDegrees * Math.PI / 180;
        List<Vector2> anchors = [
            new(1, 0),
            new(Math.Cos(sweep / 2), Math.Sin(sweep / 2)),
            new(Math.Cos(sweep), Math.Sin(sweep))];

        // Act
        List<Vector2> converted = BezierConverter.ConvertCircleToBezierAnchors(anchors);

        // Assert
        converted.Should().HaveCountGreaterThan(3);
        Vector2.Distance(converted[0], anchors[0]).Should().BeLessThan(0.02);
        Vector2.Distance(converted[^1], anchors[^1]).Should().BeLessThan(0.02);
    }

    [TestMethod]
    public void ConvertCatmullToBezierAnchors_WithThreePoints_ProducesJoinedCubicSegments()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(50, 40), new(100, 0)];

        // Act
        List<Vector2> converted = BezierConverter.ConvertCatmullToBezierAnchors(anchors);

        // Assert
        converted.Should().HaveCount(8);
        converted[0].Should().Be(anchors[0]);
        converted[^1].Should().Be(anchors[^1]);
        converted[3].Should().Be(anchors[1]);
        converted[4].Should().Be(anchors[1]);
    }

    [TestMethod]
    public void ConvertCatmullToBezierAnchors_WithFourPoints_PreservesEachSpanAndItsHandles()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(6, 0), new(12, 6), new(18, 0)];

        // Act
        List<Vector2> converted = BezierConverter.ConvertCatmullToBezierAnchors(anchors);

        // Assert
        converted.Should().Equal(
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(4, -1),
            new Vector2(6, 0),
            new Vector2(6, 0),
            new Vector2(8, 1),
            new Vector2(10, 6),
            new Vector2(12, 6),
            new Vector2(12, 6),
            new Vector2(14, 6),
            new Vector2(16, 2),
            new Vector2(18, 0));
    }

    [TestMethod]
    public void ConvertLinearToBezierAnchors_WithMultipleEdges_DuplicatesRedAnchorBoundaries()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(10, 0), new(10, 20)];

        // Act
        List<Vector2> converted = BezierConverter.ConvertLinearToBezierAnchors(anchors);

        // Assert
        converted.Should().Equal(
            new Vector2(0, 0),
            new Vector2(10, 0),
            new Vector2(10, 0),
            new Vector2(10, 20));
    }
}
