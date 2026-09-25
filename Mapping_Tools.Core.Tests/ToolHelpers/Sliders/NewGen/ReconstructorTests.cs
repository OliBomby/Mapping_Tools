using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.ToolHelpers.Sliders.NewGen;

[TestClass]
public sealed class ReconstructorTests
{
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
        (List<Vector2> anchors, PathType pathType) = reconstructor.Reconstruct(path);

        // Assert
        pathType.Should().Be(PathType.BSpline);
        anchors.Should().Equal(new Vector2(0, 0), new Vector2(20, 0));
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
        (List<Vector2> anchors, PathType pathType) = reconstructor.Reconstruct(path);

        // Assert
        pathType.Should().Be(PathType.Bezier);
        anchors[0].Should().Be(new Vector2(25, 0));
        anchors[^1].Should().Be(new Vector2(75, 0));
        new Mapping_Tools.Core.BeatmapHelper.SliderPathStuff.SliderPath(pathType, [.. anchors])
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
        (List<Vector2> anchors, PathType pathType) = reconstructor.Reconstruct(path);

        // Assert
        pathType.Should().Be(PathType.Linear);
        anchors.Should().Equal(new Vector2(0, 0), new Vector2(10, 5), new Vector2(20, 0));
    }

    [TestMethod]
    public void Reconstruct_WithEmptyHintAnchors_UsesHintEndpointsAsFallbackAnchors()
    {
        // Arrange
        PathWithHints path = CreatePathWithHint(
            new Vector2(0, 0),
            new Vector2(20, 0),
            [],
            PathType.BSpline);
        Reconstructor reconstructor = new();

        // Act
        (List<Vector2> anchors, PathType pathType) = reconstructor.Reconstruct(path);

        // Assert
        pathType.Should().Be(PathType.BSpline);
        anchors.Should().Equal(new Vector2(0, 0), new Vector2(20, 0));
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
        (List<Vector2> anchors, PathType pathType) = reconstructor.Reconstruct(path);

        // Assert
        pathType.Should().Be(PathType.Bezier);
        anchors[0].Should().Be(new Vector2(0, 0));
        anchors[^1].Should().Be(new Vector2(20, 0));
        anchors.Should().OnlyContain(point => double.IsFinite(point.X) && double.IsFinite(point.Y));
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
            hintAnchors,
            pathType,
            startP,
            endP));
        return path;
    }
}
