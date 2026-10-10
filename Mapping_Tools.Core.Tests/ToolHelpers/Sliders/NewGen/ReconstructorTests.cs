using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.ToolHelpers.Sliders.NewGen;

[TestClass]
public sealed class ReconstructorTests
{
    [TestMethod]
    public void Reconstruct_WithMixedHints_PreservesPerfectAndBezierSegments()
    {
        // Arrange
        SliderPath sourcePath = new([
            new PathControlPoint(new Vector2(0, 0), PathType.PerfectCurve),
            new PathControlPoint(new Vector2(50, 50)),
            new PathControlPoint(new Vector2(100, 0), PathType.Bezier),
            new PathControlPoint(new Vector2(150, -50)),
            new PathControlPoint(new Vector2(200, 0)),
        ]);
        PathWithHints path = PathHelper.CreatePathWithHints(sourcePath);
        Reconstructor reconstructor = new();

        // Act
        List<PathControlPoint> controlPoints = reconstructor.Reconstruct(path);

        // Assert
        controlPoints.Where(point => point.Type.HasValue).Select(point => point.Type)
            .Should().Equal(PathType.PerfectCurve, PathType.Bezier);
        new SliderPath([.. controlPoints]).Distance.Should().BeApproximately(sourcePath.Distance, 0.01);
    }

    [TestMethod]
    public void Reconstruct_WithFullBSplineHint_PreservesItsTypeAndEndpoints()
    {
        // Arrange
        PathWithHints path = CreatePathWithHint(
            new Vector2(0, 0),
            new Vector2(20, 0),
            [new Vector2(0, 0), new Vector2(10, 0)],
            PathType.BSpline);
        Reconstructor reconstructor = new();

        // Act
        List<PathControlPoint> controlPoints = reconstructor.Reconstruct(path);

        // Assert
        controlPoints[0].Type.Should().Be(PathType.BSpline);
        controlPoints.Select(point => point.Position).Should().Equal(new Vector2(0, 0), new Vector2(20, 0));
    }

    [TestMethod]
    public void Reconstruct_WithPartiallyUsedBezierHint_CutsAnchorsToTheHintInterval()
    {
        // Arrange
        PathWithHints path = CreatePathWithHint(
            new Vector2(25, 0),
            new Vector2(75, 0),
            [new Vector2(0, 0), new Vector2(100, 0)],
            PathType.Bezier,
            startP: 0.25,
            endP: 0.75);
        Reconstructor reconstructor = new();

        // Act
        List<PathControlPoint> controlPoints = reconstructor.Reconstruct(path);

        // Assert
        controlPoints[0].Type.Should().Be(PathType.Bezier);
        controlPoints[0].Position.Should().Be(new Vector2(25, 0));
        controlPoints[^1].Position.Should().Be(new Vector2(75, 0));
        new SliderPath([.. controlPoints])
            .Distance.Should().BeApproximately(50, 0.001);
    }

    [TestMethod]
    public void Reconstruct_WithDebugConstruction_ReturnsSampledPointsAsLinearAnchors()
    {
        // Arrange
        PathWithHints path = new();
        path.Path.AddLast(new PathPoint(new Vector2(0, 0)));
        path.Path.AddLast(new PathPoint(new Vector2(10, 5)));
        path.Path.AddLast(new PathPoint(new Vector2(20, 0)));
        Reconstructor reconstructor = new() { DebugConstruction = true };

        // Act
        List<PathControlPoint> controlPoints = reconstructor.Reconstruct(path);

        // Assert
        controlPoints[0].Type.Should().Be(PathType.Linear);
        controlPoints.Select(point => point.Position).Should().Equal(new Vector2(0, 0), new Vector2(10, 5), new Vector2(20, 0));
    }

    [TestMethod]
    public void Reconstruct_WithEmptyHintControlPoints_UsesBezierEndpointsAsFallback()
    {
        // Arrange
        PathWithHints path = CreatePathWithHint(
            new Vector2(0, 0),
            new Vector2(20, 0),
            [],
            PathType.BSpline);
        Reconstructor reconstructor = new();

        // Act
        List<PathControlPoint> controlPoints = reconstructor.Reconstruct(path);

        // Assert
        controlPoints[0].Type.Should().Be(PathType.Bezier);
        controlPoints.Select(point => point.Position).Should().Equal(new Vector2(0, 0), new Vector2(20, 0));
    }

    [TestMethod]
    public void Reconstruct_WithoutHints_GeneratesBezierAnchorsFromSampledPath()
    {
        // Arrange
        PathWithHints path = new();
        path.Path.AddLast(new PathPoint(new Vector2(0, 0)));
        path.Path.AddLast(new PathPoint(new Vector2(10, 10)));
        path.Path.AddLast(new PathPoint(new Vector2(20, 0)));
        PathHelper.Recalculate(path.Path);
        Reconstructor reconstructor = new();

        // Act
        List<PathControlPoint> controlPoints = reconstructor.Reconstruct(path);

        // Assert
        controlPoints[0].Type.Should().Be(PathType.Bezier);
        controlPoints[0].Position.Should().Be(new Vector2(0, 0));
        controlPoints[^1].Position.Should().Be(new Vector2(20, 0));
        controlPoints.Should().OnlyContain(point => double.IsFinite(point.Position.X) && double.IsFinite(point.Position.Y));
    }

    private static PathWithHints CreatePathWithHint(
        Vector2 startPosition,
        Vector2 endPosition,
        List<Vector2> hintAnchors,
        PathType pathType,
        double startP = 0,
        double endP = 1)
    {
        PathWithHints path = new();
        LinkedListNode<PathPoint> start = path.Path.AddLast(new PathPoint(startPosition));
        LinkedListNode<PathPoint> end = path.Path.AddLast(new PathPoint(endPosition));
        PathHelper.Recalculate(path.Path);
        path.AddReconstructionHint(new ReconstructionHint(
            start,
            end,
            0,
            hintAnchors.Select((position, index) => new PathControlPoint(position,
                index == 0 ? pathType : null)).ToList(),
            startP,
            endP));
        return path;
    }
}
