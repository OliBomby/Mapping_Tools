using System.Globalization;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.ToolHelpers.Sliders.NewGen;

[TestClass]
public class PathHelperTests
{
    [TestMethod]
    public void CreatePathWithHints_WithMixedPathTypes_PreservesEachSegmentType()
    {
        // Arrange
        SliderPath sliderPath = new([
            new PathControlPoint(new Vector2(0, 0), PathType.PerfectCurve),
            new PathControlPoint(new Vector2(50, 50)),
            new PathControlPoint(new Vector2(100, 0), PathType.Bezier),
            new PathControlPoint(new Vector2(150, -50)),
            new PathControlPoint(new Vector2(200, 0)),
        ]);

        // Act
        PathWithHints result = PathHelper.CreatePathWithHints(sliderPath);

        // Assert
        result.ReconstructionHints.Select(hint => hint.ControlPoints![0].Type)
            .Should().Equal(PathType.PerfectCurve, PathType.Bezier);
        result.ReconstructionHints[0].ControlPoints!.Select(point => point.Position).Should().Equal(
            new Vector2(0, 0), new Vector2(50, 50), new Vector2(100, 0));
        result.ReconstructionHints[1].ControlPoints!.Select(point => point.Position).Should().Equal(
            new Vector2(100, 0), new Vector2(150, -50), new Vector2(200, 0));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(4)]
    public void CreatePathWithHints_WithSplinePathType_PreservesSourceTypeAndAnchors(int pathTypeValue)
    {
        // Arrange
        PathType pathType =
            (PathType)pathTypeValue;
        Vector2[] anchors = [new(0, 0), new(40, 60), new(90, -20), new(140, 30)];
        SliderPath sliderPath = new(pathType, anchors);

        // Act
        PathWithHints result = PathHelper.CreatePathWithHints(sliderPath);

        // Assert
        result.ReconstructionHints.Should().ContainSingle();
        result.ReconstructionHints[0].ControlPoints![0].Type.Should().Be(pathType);
        result.ReconstructionHints[0].ControlPoints!.Select(point => point.Position).Should().Equal(anchors);
        result.Path.First!.Value.Pos.Should().Be(anchors[0]);
        result.Path.Last!.Value.Pos.Should().Be(anchors[^1]);
    }

    [TestMethod]
    public void CreatePathWithHints_WithLinearPath_CreatesOneTypedHintPerEdge()
    {
        // Arrange
        Vector2[] anchors = [new(0, 0), new(10, 0), new(10, 10)];
        SliderPath sliderPath = new(
            PathType.Linear,
            anchors);

        // Act
        PathWithHints result = PathHelper.CreatePathWithHints(sliderPath);

        // Assert
        result.Path.Should().OnlyContain(point => point.Red);
        result.ReconstructionHints.Should().HaveCount(2);
        result.ReconstructionHints.Select(hint => hint.ControlPoints![0].Type)
            .Should().Equal(
                PathType.Linear,
                PathType.Linear);
        result.ReconstructionHints[0].ControlPoints!.Select(point => point.Position).Should().Equal(anchors);
        result.ReconstructionHints[1].ControlPoints!.Select(point => point.Position).Should().Equal(anchors);
        result.ReconstructionHints[0].Start.Value.Pos.Should().Be(anchors[0]);
        result.ReconstructionHints[0].End.Value.Pos.Should().Be(anchors[1]);
        result.ReconstructionHints[1].Start.Value.Pos.Should().Be(anchors[1]);
        result.ReconstructionHints[1].End.Value.Pos.Should().Be(anchors[2]);
    }

    [TestMethod]
    public void CreatePathWithHints_StandardPath_MarksExpectedRedAnchors()
    {
        // Arrange
        var slider =
            new HitObject("42,179,300,2,0,B|135:234|219:171|219:171|194:100|194:100|266:53|345:48|405:117,1,500");

        var sliderPath = slider.GetSliderPath();

        // Act
        var result = PathHelper.CreatePathWithHints(sliderPath);

        // Assert
        int i = 0;
        foreach (var pathPoint in result.Path)
        {
            i++;
            if (pathPoint.Pos == new Vector2(219, 171) || pathPoint.Pos == new Vector2(194, 100))
                pathPoint.Red.Should().BeTrue($"point {i} should be a red anchor");
            else
                pathPoint.Red.Should().BeFalse($"point {i} should not be a red anchor");
        }

        result.Path.Count(o => o.Red).Should().Be(2);
    }

    [TestMethod]
    public void CreatePathWithHints_RepeatedRedAnchors_CreatesValidHints()
    {
        // Arrange
        var slider =
            new HitObject(
                "42,179,300,2,0,B|42:179|42:179|42:179|42:179|135:234|219:171|219:171|219:171|219:171|194:100|194:100|194:100|194:100|194:100|194:100|266:53|345:48|405:117|405:117|405:117|405:117|405:117|405:117|405:117,1,450");

        var sliderPath = slider.GetSliderPath();

        // Act
        var result = PathHelper.CreatePathWithHints(sliderPath);

        // Assert
        int i = 0;
        foreach (var pathPoint in result.Path)
        {
            i++;
            if (pathPoint.Pos == new Vector2(219, 171) || pathPoint.Pos == new Vector2(194, 100))
                pathPoint.Red.Should().BeTrue($"point {i} should be a red anchor");
            else
                pathPoint.Red.Should().BeFalse($"point {i} should not be a red anchor");
        }

        i = 0;
        foreach (var hint in result.ReconstructionHints)
        {
            i++;
            hint.ControlPoints.Should().NotBeNull();
            (hint.ControlPoints!.Count > 1).Should().BeTrue($"hint {i} does not have enough anchors");
        }

        result.Path.Count(o => o.Red).Should().Be(2);
    }

    [TestMethod]
    public void Interpolate_WithAndWithoutRedSuccessor_InsertsExpectedPoints()
    {
        // Arrange
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var path = new LinkedList<PathPoint>([
            new PathPoint(new Vector2(-9, 0)),
            new PathPoint(new Vector2(1, 0)),
            new PathPoint(new Vector2(2, 1)),
            new PathPoint(new Vector2(12, 1)),
        ]);
        PathHelper.Recalculate(path);

        var path2 = new LinkedList<PathPoint>([
            new PathPoint(new Vector2(-9, 0)),
            new PathPoint(new Vector2(1, 0)),
            new PathPoint(new Vector2(2, 1), red: true),
            new PathPoint(new Vector2(12, 1)),
        ]);
        PathHelper.Recalculate(path2);

        // Act
        var p1 = path.First!.Next;
        PathHelper.Interpolate(p1!, Enumerable.Range(1, 9).Select(i => i / 10d));

        var p2 = path2.First!.Next;
        PathHelper.Interpolate(p2!, Enumerable.Range(1, 9).Select(i => i / 10d));

        // Assert
        path.Should().HaveCount(13);
        path.Should().OnlyContain(point => !point.Red);
        path2.Should().HaveCount(13);
        path2.Count(point => point.Red).Should().Be(1);
    }

    [TestMethod]
    public void Subdivide_FourPointPath_InsertsOrderedPoints()
    {
        // Arrange
        var path = new LinkedList<PathPoint>([
            new PathPoint(new Vector2(-9, 0)),
            new PathPoint(new Vector2(1, 0)),
            new PathPoint(new Vector2(2, 1)),
            new PathPoint(new Vector2(12, 1)),
        ]);
        PathHelper.Recalculate(path);

        // Act
        var start = path.First!.Next!;
        var middle = start!.Next;
        var end = path.Last!;
        int added = path.Subdivide(start, end, 5);

        // Assert
        added.Should().Be(4);

        (start.Next!.Value > start.Value).Should().BeTrue();
        start.Next.Should().BeSameAs(middle);
        (start.Next.Next!.Value > start.Next.Value).Should().BeTrue();
        (start.Next.Next.Next!.Value > start.Next.Next.Value).Should().BeTrue();
        (start.Next.Next.Next.Next!.Value > start.Next.Next.Next.Value).Should().BeTrue();
        (start.Next.Next.Next.Next.Next!.Value > start.Next.Next.Next.Next.Value).Should().BeTrue();
        (start.Next!.Next!.Next!.Next!.Next!.Next!.Value > start.Next!.Next!.Next!.Next!.Next!.Value).Should().BeTrue();
        start.Next!.Next!.Next!.Next!.Next!.Next.Should().BeSameAs(end);
    }
}
