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
    public void MoveAnchorsToLength_WithMixedPath_CutsOnlyTheActivePerfectSegment()
    {
        // Arrange
        PathControlPoint[] source = CreateMixedPath();
        double perfectLength = new SliderPath(PathType.PerfectCurve,
            [new Vector2(100, 0), new Vector2(150, 50), new Vector2(200, 0)]).Distance;
        double targetLength = 100 + perfectLength / 2;

        // Act
        List<PathControlPoint> moved = SliderPathUtil.MoveAnchorsToLength(source, targetLength);

        // Assert
        moved.Select(point => point.Type).Should().Equal(PathType.Linear, PathType.PerfectCurve, null, null);
        moved[0].Position.Should().Be(Vector2.Zero);
        moved[1].Position.Should().Be(new Vector2(100, 0));
        new SliderPath([.. moved]).Distance.Should().BeApproximately(targetLength, 0.01);
        source.Select(point => point.Position).Should().Equal(
            Vector2.Zero, new Vector2(100, 0), new Vector2(150, 50), new Vector2(200, 0), new Vector2(300, 0));
    }

    [TestMethod]
    public void MoveAnchorsToLength_WithCatmullThenBezier_PreservesCompletedCatmullSegment()
    {
        // Arrange
        PathControlPoint[] source = [
            new(Vector2.Zero, PathType.Catmull),
            new(new Vector2(30, 60)),
            new(new Vector2(70, -20)),
            new(new Vector2(100, 0), PathType.Bezier),
            new(new Vector2(150, 50)),
            new(new Vector2(200, 0)),
        ];
        double catmullLength = new SliderPath(PathType.Catmull,
            source.Take(4).Select(point => point.Position).ToArray()).Distance;
        double targetLength = catmullLength + 30;

        // Act
        List<PathControlPoint> moved = SliderPathUtil.MoveAnchorsToLength(source, targetLength);

        // Assert
        moved[0].Type.Should().Be(PathType.Catmull);
        moved.Take(4).Select(point => point.Position).Should().Equal(source.Take(4).Select(point => point.Position));
        moved[3].Type.Should().Be(PathType.Bezier);
        new SliderPath([.. moved]).Distance.Should().BeApproximately(targetLength, 0.01);
    }


    [TestMethod]
    public void MoveAnchorsToCompletion_WithMixedPath_KeepsCompletedPerfectSegment()
    {
        // Arrange
        PathControlPoint[] source = CreateMixedPath();
        SliderPath fullPath = new(source);
        double completion = (fullPath.Distance - 50) / fullPath.Distance;

        // Act
        List<PathControlPoint> moved = SliderPathUtil.MoveAnchorsToCompletion(source, completion);

        // Assert
        moved.Select(point => point.Type).Should().Equal(PathType.Linear, PathType.PerfectCurve, null, PathType.Linear, null);
        Vector2.Distance(moved[^1].Position, new Vector2(250, 0)).Should().BeLessThan(0.000001);
        new SliderPath([.. moved]).Distance.Should().BeApproximately(completion * fullPath.Distance, 0.01);
    }

    [TestMethod]
    public void MoveAnchorsToLength_WithMixedPath_ExtendsAfterOriginalSegments()
    {
        // Arrange
        PathControlPoint[] source = CreateMixedPath();
        SliderPath fullPath = new(source);
        double targetLength = fullPath.Distance + 50;

        // Act
        List<PathControlPoint> moved = SliderPathUtil.MoveAnchorsToLength(source, targetLength);

        // Assert
        moved.Take(source.Length - 1).Select(point => (point.Position, point.Type))
            .Should().Equal(source.Take(source.Length - 1).Select(point => (point.Position, point.Type)));
        moved[^2].Type.Should().Be(PathType.Linear);
        moved[^1].Position.Should().Be(new Vector2(350, 0));
        new SliderPath([.. moved]).Distance.Should().BeApproximately(targetLength, 0.01);
    }

    private static PathControlPoint[] CreateMixedPath() =>
    [
        new(Vector2.Zero, PathType.Linear),
        new(new Vector2(100, 0), PathType.PerfectCurve),
        new(new Vector2(150, 50)),
        new(new Vector2(200, 0), PathType.Linear),
        new(new Vector2(300, 0)),
    ];

    [TestMethod]
    public void MoveAnchorsToLength_WithLongerLinearPath_ExtendsFinalAnchor()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(100, 0)];

        // Act
        List<Vector2> moved = MoveToLength(anchors, PathType.Linear, 150, out PathType pathType);

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
        List<Vector2> moved = MoveToLength(anchors, PathType.Linear, 150, out PathType pathType);

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
        List<Vector2> moved = MoveToLength(anchors, PathType.Linear, 100, out PathType pathType);

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
        List<Vector2> moved = MoveToLength(anchors, PathType.Linear, 40, out PathType pathType);

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
        List<Vector2> moved = MoveToCompletion(anchors, PathType.Linear, 0.75, out PathType pathType);

        // Assert
        pathType.Should().Be(PathType.Linear);
        moved.Should().Equal(new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 50));
    }

    [TestMethod]
    public void GetRedAnchorCompletions_WithTypedBezierAnchor_ReturnsSegmentBoundary()
    {
        // Arrange
        SliderPath path = new(
        [
            new PathControlPoint(new Vector2(0, 0), PathType.Bezier),
            new PathControlPoint(new Vector2(100, 0), PathType.Bezier),
            new PathControlPoint(new Vector2(200, 0)),
        ]);

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
        List<PathControlPoint> moved = SliderPathUtil.MoveAnchorsToLength(ToControlPoints(anchors, PathType.Bezier), 150);

        // Assert
        moved[0].Type.Should().Be(PathType.Bezier);
        moved[^2].Type.Should().Be(PathType.Linear);
        moved.Select(point => point.Position).Should().Equal(
            new Vector2(0, 0),
            new Vector2(100, 0),
            new Vector2(100, 0),
            new Vector2(150, 0));
        new SliderPath([.. moved]).Distance.Should().BeApproximately(150, 0.01);
        anchors.Should().Equal(new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 0));
    }

    [DataTestMethod]
    [DataRow((int)PathType.Catmull)]
    [DataRow((int)PathType.PerfectCurve)]
    public void MoveAnchorsToLength_ExtendingCurvedPath_PreservesSourceTypeAndAddsLinearSegment(int pathTypeValue)
    {
        // Arrange
        PathType sourceType = (PathType)pathTypeValue;
        List<Vector2> anchors = [new(0, 0), new(50, 50), new(100, 0)];
        SliderPath source = new(sourceType, [.. anchors]);
        double targetLength = source.Distance + 25;

        // Act
        List<PathControlPoint> moved = SliderPathUtil.MoveAnchorsToLength(ToControlPoints(anchors, sourceType), targetLength);

        // Assert
        moved[0].Type.Should().Be(sourceType);
        moved[^2].Type.Should().Be(PathType.Linear);
        Vector2.Distance(moved[0].Position, anchors[0]).Should().BeLessThan(0.000001);
        Vector2.Distance(moved[^1].Position, source.PositionAt(1) + (source.CalculatedPath[^1] - source.CalculatedPath[^2]).Normalized() * 25)
            .Should().BeLessThan(1);
        new SliderPath([.. moved]).Distance.Should().BeApproximately(targetLength, 1);
    }

    [TestMethod]
    public void MoveAnchorsToLength_ExtendingBSplinePath_PreservesItsTypeAndReachesRequestedLength()
    {
        // Arrange
        List<Vector2> anchors = [new(0, 0), new(30, 60), new(70, -20), new(100, 0)];
        SliderPath source = new(PathType.BSpline, [.. anchors]);
        double targetLength = source.Distance + 20;
        SliderPath requested = new(PathType.BSpline, [.. anchors], targetLength);

        // Act
        List<PathControlPoint> moved = SliderPathUtil.MoveAnchorsToLength(ToControlPoints(anchors, PathType.BSpline), targetLength);

        // Assert
        moved[0].Type.Should().Be(PathType.BSpline);
        moved[^2].Type.Should().Be(PathType.Linear);
        Vector2.Distance(moved[0].Position, anchors[0]).Should().BeLessThan(0.000001);
        Vector2.Distance(moved[^1].Position, requested.PositionAt(1)).Should().BeLessThan(0.000001);
        new SliderPath([.. moved]).Distance.Should().BeApproximately(targetLength, 0.01);
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
        List<PathControlPoint> moved = SliderPathUtil.MoveAnchorsToLength(
            ToControlPoints(anchors, PathType.BSpline), source.Distance, targetLength);

        // Assert
        moved[0].Type.Should().Be(PathType.Bezier);
        moved[0].Position.Should().Be(anchors[0]);
        Vector2.Distance(moved[^1].Position, truncated.PositionAt(1)).Should().BeLessThan(0.001);
        new SliderPath([.. moved]).Distance.Should().BeApproximately(targetLength, 0.01);
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
        List<Vector2> moved = MoveToLength(
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
        List<PathControlPoint> moved = SliderPathUtil.MoveAnchorsToLength(
            ToControlPoints(anchors, PathType.Catmull), source.Distance, targetLength);

        // Assert
        moved[0].Type.Should().Be(PathType.Bezier);
        moved[0].Position.Should().Be(anchors[0]);
        Vector2.Distance(moved[^1].Position, truncated.PositionAt(1)).Should().BeLessThan(1);
        new SliderPath([.. moved]).Distance.Should().BeApproximately(targetLength, 1);
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
    public void ChopAnchors_WithTypedBezierBoundaries_ReturnsThreeIndependentSegments()
    {
        // Arrange
        SliderPath path = new(
        [
            new PathControlPoint(new Vector2(0, 0), PathType.Bezier),
            new PathControlPoint(new Vector2(10, 0), PathType.Bezier),
            new PathControlPoint(new Vector2(10, 10), PathType.Bezier),
            new PathControlPoint(new Vector2(20, 10)),
        ]);

        // Act
        BezierSubdivision[] subdivisions = SliderPathUtil.ChopAnchors(path).ToArray();

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

    private static List<Vector2> MoveToLength(List<Vector2> anchors, PathType type, double newLength, out PathType resultType)
    {
        var moved = SliderPathUtil.MoveAnchorsToLength(ToControlPoints(anchors, type), newLength);
        resultType = moved[0].Type!.Value;
        return moved.Select(point => point.Position).ToList();
    }

    private static List<Vector2> MoveToLength(List<Vector2> anchors, PathType type, double fullLength, double newLength, out PathType resultType)
    {
        var moved = SliderPathUtil.MoveAnchorsToLength(ToControlPoints(anchors, type), fullLength, newLength);
        resultType = moved[0].Type!.Value;
        return moved.Select(point => point.Position).ToList();
    }

    private static List<Vector2> MoveToCompletion(List<Vector2> anchors, PathType type, double completion, out PathType resultType)
    {
        var moved = SliderPathUtil.MoveAnchorsToCompletion(ToControlPoints(anchors, type), completion);
        resultType = moved[0].Type!.Value;
        return moved.Select(point => point.Position).ToList();
    }

    private static PathControlPoint[] ToControlPoints(List<Vector2> anchors, PathType type) =>
        anchors.Select((point, index) => new PathControlPoint(point, index == 0 ? type : null)).ToArray();
}
