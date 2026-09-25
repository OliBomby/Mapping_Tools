using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper.SliderPathStuff;

[TestClass]
public sealed class PathApproximatorTests
{
    [TestMethod]
    public void ApproximateCatmull_MultipleSegmentsPreserveBothEndpoints()
    {
        // Arrange
        List<Vector2> controlPoints = [new(0, 0), new(30, 50), new(60, 0)];

        // Act
        List<Vector2> path = PathApproximator.ApproximateCatmull(controlPoints);

        // Assert
        path.Should().HaveCount(200);
        path[0].Should().Be(controlPoints[0]);
        path[^1].Should().Be(controlPoints[^1]);
    }

    [TestMethod]
    public void ApproximateCircularArc_ThreePointArcPassesThroughEndpointsAndFollowsControlSide()
    {
        // Arrange
        List<Vector2> controlPoints = [new(0, 0), new(50, 50), new(100, 0)];

        // Act
        List<Vector2> path = PathApproximator.ApproximateCircularArc(controlPoints);

        // Assert
        path.Should().HaveCountGreaterThan(2);
        Vector2.Distance(path[0], controlPoints[0]).Should().BeLessThan(0.000001);
        Vector2.Distance(path[^1], controlPoints[^1]).Should().BeLessThan(0.000001);
        path.Max(point => point.Y).Should().BeGreaterThan(40);
    }

    [TestMethod]
    public void ApproximateCircularArc_DegenerateControlPoints_ReturnsNoArc()
    {
        // Arrange
        List<Vector2> collinear = [new(0, 0), new(50, 0), new(100, 0)];
        List<Vector2> duplicate = [new(0, 0), new(0, 0), new(100, 0)];

        // Act
        List<Vector2> collinearPath = PathApproximator.ApproximateCircularArc(collinear);
        List<Vector2> duplicatePath = PathApproximator.ApproximateCircularArc(duplicate);

        // Assert
        collinearPath.Should().BeEmpty();
        duplicatePath.Should().BeEmpty();
    }

    [TestMethod]
    public void ApproximateBSpline_DegreeAbovePointCountClampsAndPreservesEndpoints()
    {
        // Arrange
        List<Vector2> controlPoints = [new(0, 0), new(40, 80), new(100, 0)];

        // Act
        List<Vector2> path = PathApproximator.ApproximateBSpline(controlPoints, degree: 8);

        // Assert
        path.Should().HaveCountGreaterThan(3);
        path[0].Should().Be(controlPoints[0]);
        path[^1].Should().Be(controlPoints[^1]);
        path.Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
    }

    [TestMethod]
    public void ApproximateBSpline_WithNoPoints_ReturnsEmptyAndRejectsInvalidDegree()
    {
        // Arrange
        List<Vector2> noPoints = [];
        List<Vector2> onePoint = [new(12, 34)];

        // Act
        List<Vector2> emptyPath = PathApproximator.ApproximateBSpline(noPoints, degree: 1);
        List<Vector2> onePointPath = PathApproximator.ApproximateBSpline(onePoint, degree: 1);
        Action invalidDegree = () => PathApproximator.ApproximateBSpline(onePoint, degree: 0);

        // Assert
        emptyPath.Should().BeEmpty();
        onePointPath.Should().Equal(onePoint);
        invalidDegree.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void ApproximateBSpline_WithMultipleKnotSpansJoinsSpansAndPreservesEndpoints()
    {
        // Arrange
        List<Vector2> controlPoints =
        [
            new(0, 0),
            new(20, 60),
            new(50, -20),
            new(80, 60),
            new(100, 0),
        ];

        // Act
        List<Vector2> path = PathApproximator.ApproximateBSpline(controlPoints, degree: 2);

        // Assert
        path.Should().HaveCountGreaterThan(controlPoints.Count);
        path[0].Should().Be(controlPoints[0]);
        path[^1].Should().Be(controlPoints[^1]);
        path.Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
        (path.Max(point => point.Y) - path.Min(point => point.Y)).Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void ApproximateBezier_WithCurvedControlPolygonReturnsFinitePolyline()
    {
        // Arrange
        List<Vector2> controlPoints = [new(0, 0), new(50, 100), new(100, 0)];

        // Act
        List<Vector2> path = PathApproximator.ApproximateBezier(controlPoints);

        // Assert
        path.Should().HaveCountGreaterThan(3);
        path[0].Should().Be(controlPoints[0]);
        path[^1].Should().Be(controlPoints[^1]);
        path.Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
        path.Max(point => point.Y).Should().BeGreaterThan(40);
    }

    [TestMethod]
    public void ApproximateCatmull_WithInteriorSegmentPreservesControlPointJoins()
    {
        // Arrange
        List<Vector2> controlPoints = [new(0, 0), new(30, 50), new(60, 0), new(90, 30)];

        // Act
        List<Vector2> path = PathApproximator.ApproximateCatmull(controlPoints);

        // Assert
        path.Should().HaveCount((controlPoints.Count - 1) * 100);
        path[0].Should().Be(controlPoints[0]);
        path[^1].Should().Be(controlPoints[^1]);
        path.Should().Contain(controlPoints[1]);
        path.Should().Contain(controlPoints[2]);
    }

    [TestMethod]
    public void CatmullFindPoint_AtSegmentBoundariesReturnsAdjacentControlPoints()
    {
        // Arrange
        Vector2 p0 = new(0, 0);
        Vector2 p1 = new(10, 20);
        Vector2 p2 = new(30, 40);
        Vector2 p3 = new(50, 60);

        // Act
        Vector2 start = PathApproximator.CatmullFindPoint(ref p0, ref p1, ref p2, ref p3, 0);
        Vector2 end = PathApproximator.CatmullFindPoint(ref p0, ref p1, ref p2, ref p3, 1);

        // Assert
        start.Should().Be(p1);
        end.Should().Be(p2);
    }

    [TestMethod]
    public void ApproximateCircularArc_WithRadiusBelowToleranceUsesTwoEndpoints()
    {
        // Arrange
        List<Vector2> controlPoints = [new(0, 0), new(0.01, 0.01), new(0.02, 0)];

        // Act
        List<Vector2> path = PathApproximator.ApproximateCircularArc(controlPoints);

        // Assert
        path.Should().HaveCount(2);
        Vector2.Distance(path[0], controlPoints[0]).Should().BeLessThan(0.000001);
        Vector2.Distance(path[^1], controlPoints[^1]).Should().BeLessThan(0.000001);
    }
}
