using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper.SliderPathStuff;

[TestClass]
public sealed class SliderPathTests
{
    [TestMethod]
    public void PositionAt_ClampsProgressToThePathEndpoints()
    {
        // Arrange
        SliderPath path = new(PathType.Linear, [new Vector2(0, 0), new Vector2(10, 0)]);

        // Act
        Vector2 beforeStart = path.PositionAt(-0.5);
        Vector2 afterEnd = path.PositionAt(1.5);

        // Assert
        beforeStart.Should().Be(new Vector2(0, 0));
        afterEnd.Should().Be(new Vector2(10, 0));
    }

    [TestMethod]
    public void GetPathToProgress_ReplacesOutputWithInterpolatedProgressBounds()
    {
        // Arrange
        SliderPath path = new(PathType.Linear, [new Vector2(0, 0), new Vector2(10, 0)]);
        List<Vector2> output = [new(99, 99)];

        // Act
        path.GetPathToProgress(output, 0.25, 0.75);

        // Assert
        output.Should().Equal(new Vector2(2.5, 0), new Vector2(7.5, 0));
    }

    [TestMethod]
    public void GetPathToProgress_WithInteriorVertex_KeepsThatVertexBetweenInterpolatedBounds()
    {
        // Arrange
        SliderPath path = new(PathType.Linear,
            [new Vector2(0, 0), new Vector2(4, 0), new Vector2(10, 0)]);
        List<Vector2> output = [];

        // Act
        path.GetPathToProgress(output, 0.2, 0.8);

        // Assert
        output.Should().Equal(new Vector2(2, 0), new Vector2(4, 0), new Vector2(8, 0));
    }

    [TestMethod]
    public void ControlPoints_ReturnsCopyThatCannotChangeCalculatedPath()
    {
        // Arrange
        SliderPath path = new(PathType.Linear, [new Vector2(0, 0), new Vector2(10, 0)]);

        // Act
        List<Vector2> exposedControlPoints = path.ControlPoints;
        exposedControlPoints[0] = new Vector2(100, 100);

        // Assert
        path.ControlPoints.Should().Equal(new Vector2(0, 0), new Vector2(10, 0));
        path.PositionAt(0).Should().Be(new Vector2(0, 0));
    }

    [TestMethod]
    public void ExpectedDistance_ShortensOrExtendsCalculatedPathToRequestedDistance()
    {
        // Arrange
        Vector2[] controlPoints = [new(0, 0), new(10, 0)];
        SliderPath shortened = new(PathType.Linear, controlPoints, 5);
        SliderPath extended = new(PathType.Linear, controlPoints, 15);

        // Act
        double shortDistance = shortened.Distance;
        Vector2 shortEnd = shortened.PositionAt(1);
        double longDistance = extended.Distance;
        Vector2 longEnd = extended.PositionAt(1);

        // Assert
        shortDistance.Should().Be(5);
        shortEnd.Should().Be(new Vector2(5, 0));
        longDistance.Should().Be(15);
        longEnd.Should().Be(new Vector2(15, 0));
        controlPoints.Should().Equal(new Vector2(0, 0), new Vector2(10, 0));
    }

    [TestMethod]
    public void SliderballPositions_SplitsLinearSegmentAcrossEveryMillisecond()
    {
        // Arrange
        SliderPath path = new(PathType.Linear, [new Vector2(0, 0), new Vector2(10, 0)]);

        // Act
        Vector2[] positions = path.SliderballPositions(4);
        Vector2[] individualPositions = Enumerable.Range(0, 5)
            .Select(milliseconds => path.SliderballPositionAt(milliseconds, 4))
            .ToArray();

        // Assert
        positions.Should().Equal(
            new Vector2(0, 0),
            new Vector2(2.5, 0),
            new Vector2(5, 0),
            new Vector2(7.5, 0),
            new Vector2(10, 0));
        individualPositions.Should().Equal(positions);
    }

    [TestMethod]
    public void SliderballPositionAt_UsesMillisecondRangeWithinEachPathSegment()
    {
        // Arrange
        SliderPath path = new(PathType.Linear,
            [new Vector2(0, 0), new Vector2(5, 0), new Vector2(10, 0)]);

        // Act
        Vector2[] positions = path.SliderballPositions(4);
        Vector2[] individualPositions = Enumerable.Range(0, 5)
            .Select(milliseconds => path.SliderballPositionAt(milliseconds, 4))
            .ToArray();

        // Assert
        positions.Should().Equal(
            new Vector2(0, 0),
            new Vector2(2.5, 0),
            new Vector2(5, 0),
            new Vector2(7.5, 0),
            new Vector2(10, 0));
        individualPositions.Should().Equal(positions);
    }

    [TestMethod]
    public void SliderballPositions_WithUnequalSegments_UsesUniformPathProgressForEachMillisecond()
    {
        // Arrange
        SliderPath path = new(PathType.Linear,
            [new Vector2(0, 0), new Vector2(2, 0), new Vector2(10, 0)]);

        // Act
        Vector2[] positions = path.SliderballPositions(4);
        Vector2[] individualPositions = Enumerable.Range(0, 5)
            .Select(milliseconds => path.SliderballPositionAt(milliseconds, 4))
            .ToArray();

        // Assert
        positions.Should().Equal(
            new Vector2(0, 0),
            new Vector2(2.5, 0),
            new Vector2(5, 0),
            new Vector2(7.5, 0),
            new Vector2(10, 0));
        individualPositions.Should().Equal(positions);
    }

    [TestMethod]
    public void SliderballPositions_WithZeroDuration_ReturnsSliderStart()
    {
        // Arrange
        SliderPath path = new(PathType.Linear, [new Vector2(0, 0), new Vector2(10, 0)]);

        // Act
        Vector2[] positions = path.SliderballPositions(0);

        // Assert
        positions.Should().ContainSingle().Which.Should().Be(new Vector2(0, 0));
        path.SliderballPositionAt(0, 0).Should().Be(new Vector2(0, 0));
    }

    [TestMethod]
    public void PerfectCurve_WithThreePointsUsesArcAndWithExtraPointsFallsBackToBezier()
    {
        // Arrange
        Vector2[] arcPoints = [new(0, 0), new(50, 50), new(100, 0)];
        Vector2[] longerPoints = [new(0, 0), new(30, 50), new(70, 50), new(100, 0)];
        SliderPath arc = new(PathType.PerfectCurve, arcPoints);
        SliderPath fallback = new(PathType.PerfectCurve, longerPoints);

        // Act
        IReadOnlyList<Vector2> arcPath = arc.CalculatedPath;
        IReadOnlyList<Vector2> fallbackPath = fallback.CalculatedPath;

        // Assert
        arcPath.Should().HaveCountGreaterThan(2);
        arcPath.Max(point => point.Y).Should().BeGreaterThan(40);
        fallbackPath.Should().HaveCountGreaterThan(2);
        fallbackPath[0].Should().Be(longerPoints[0]);
        fallbackPath[^1].Should().Be(longerPoints[^1]);
    }

    [DataTestMethod]
    [DataRow((int)PathType.Catmull)]
    [DataRow((int)PathType.BSpline)]
    public void CalculatedPath_WithSplinePathTypes_UsesTheRequestedCurveType(int pathTypeValue)
    {
        // Arrange
        PathType pathType = (PathType)pathTypeValue;
        Vector2[] controlPoints = [new(0, 0), new(30, 60), new(70, -20), new(100, 0)];
        SliderPath path = new(pathType, controlPoints);

        // Act
        IReadOnlyList<Vector2> calculatedPath = path.CalculatedPath;

        // Assert
        path.Type.Should().Be(pathType);
        calculatedPath.Count.Should().BeGreaterThan(2);
        calculatedPath[0].Should().Be(controlPoints[0]);
        calculatedPath[^1].Should().Be(controlPoints[^1]);
        calculatedPath.Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
    }

    [TestMethod]
    public void CalculatedPath_WithRepeatedRedAnchor_TracksEachSubpathStart()
    {
        // Arrange
        SliderPath path = new(PathType.Linear,
            [new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 0), new Vector2(10, 10)]);

        // Act
        IReadOnlyList<Vector2> calculatedPath = path.CalculatedPath;

        // Assert
        path.SegmentStarts.Should().Equal(0, 2);
        calculatedPath.Should().Equal(
            new Vector2(0, 0),
            new Vector2(10, 0),
            new Vector2(10, 10));
    }

    [TestMethod]
    public void PerfectCurve_WithCollinearThreePoints_FallsBackToBezierApproximation()
    {
        // Arrange
        Vector2[] controlPoints = [new(0, 0), new(10, 0), new(20, 0)];
        SliderPath path = new(PathType.PerfectCurve, controlPoints);

        // Act
        IReadOnlyList<Vector2> calculatedPath = path.CalculatedPath;

        // Assert
        calculatedPath.Should().HaveCountGreaterThan(1);
        calculatedPath[0].Should().Be(controlPoints[0]);
        calculatedPath[^1].Should().Be(controlPoints[^1]);
        path.Distance.Should().BeApproximately(20, 0.001);
    }

    [TestMethod]
    public void EmptyPath_ReturnsZeroPositionAndNoCalculatedVertices()
    {
        // Arrange
        SliderPath path = new(PathType.Linear, []);

        // Act
        Vector2 position = path.PositionAt(0.5);

        // Assert
        path.Distance.Should().Be(0);
        path.CalculatedPath.Should().BeEmpty();
        position.Should().Be(Vector2.Zero);
    }

    [TestMethod]
    public void Equals_UsesPathTypeDistanceAndOrderedControlPoints()
    {
        // Arrange
        SliderPath first = new(PathType.Linear, [new Vector2(0, 0), new Vector2(10, 0)], 10);
        SliderPath same = new(PathType.Linear, [new Vector2(0, 0), new Vector2(10, 0)], 10);
        SliderPath differentType = new(PathType.Bezier, [new Vector2(0, 0), new Vector2(10, 0)], 10);
        SliderPath differentDistance = new(PathType.Linear, [new Vector2(0, 0), new Vector2(10, 0)], 9);
        SliderPath differentOrder = new(PathType.Linear, [new Vector2(10, 0), new Vector2(0, 0)], 10);

        // Act
        bool samePath = first.Equals(same);
        bool sameObject = first.Equals((object)same);
        bool equalsNull = first.Equals(null!);
        bool equalsOtherType = first.Equals(new object());

        // Assert
        samePath.Should().BeTrue();
        sameObject.Should().BeTrue();
        equalsNull.Should().BeFalse();
        equalsOtherType.Should().BeFalse();
        (first == same).Should().BeTrue();
        (first != differentType).Should().BeTrue();
        first.Equals(differentType).Should().BeFalse();
        first.Equals(differentDistance).Should().BeFalse();
        first.Equals(differentOrder).Should().BeFalse();
    }

    [TestMethod]
    public void GetHashCode_WithEqualSerializedInputs_AllowsHashSetDeduplication()
    {
        // Arrange
        SliderPath first = new(PathType.BSpline,
            [new Vector2(0, 0), new Vector2(30, 60), new Vector2(100, 0)], 75);
        SliderPath same = new(PathType.BSpline,
            [new Vector2(0, 0), new Vector2(30, 60), new Vector2(100, 0)], 75);
        HashSet<SliderPath> paths = [first, same];

        // Act
        int firstHash = first.GetHashCode();
        int sameHash = same.GetHashCode();

        // Assert
        firstHash.Should().Be(sameHash);
        paths.Should().ContainSingle();
    }
}
