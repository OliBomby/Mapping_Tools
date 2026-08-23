using System.Globalization;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ToolHelpers.Sliders.Newgen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.ToolHelpers.Sliders.NewGen;

[TestClass]
public class PathHelperTests
{
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
            (hint.Anchors.Count > 1).Should().BeTrue($"hint {i} does not have enough anchors");
        }

        result.Path.Count(o => o.Red).Should().Be(2);
    }

    [TestMethod]
    public void Interpolate_WithAndWithoutRedSuccessor_InsertsExpectedPoints()
    {
        // Arrange
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var path = new LinkedList<PathPoint>(new[]
        {
            new PathPoint(new Vector2(-9, 0)),
            new PathPoint(new Vector2(1, 0)),
            new PathPoint(new Vector2(2, 1)),
            new PathPoint(new Vector2(12, 1)),
        });
        PathHelper.Recalculate(path);

        var path2 = new LinkedList<PathPoint>(new[]
        {
            new PathPoint(new Vector2(-9, 0)),
            new PathPoint(new Vector2(1, 0)),
            new PathPoint(new Vector2(2, 1), red: true),
            new PathPoint(new Vector2(12, 1)),
        });
        PathHelper.Recalculate(path2);

        // Act
        var p1 = path.First!.Next;
        PathHelper.Interpolate(p1, Enumerable.Range(1, 9).Select(i => i / 10d));

        var p2 = path2.First!.Next;
        PathHelper.Interpolate(p2, Enumerable.Range(1, 9).Select(i => i / 10d));

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
        var path = new LinkedList<PathPoint>(new[]
        {
            new PathPoint(new Vector2(-9, 0)),
            new PathPoint(new Vector2(1, 0)),
            new PathPoint(new Vector2(2, 1)),
            new PathPoint(new Vector2(12, 1)),
        });
        PathHelper.Recalculate(path);

        // Act
        var start = path.First!.Next;
        var middle = start!.Next;
        var end = path.Last;
        int added = path.Subdivide(start, end, 5);

        // Assert
        added.Should().Be(4);

        (start!.Next!.Value > start.Value).Should().BeTrue();
        start.Next.Should().BeSameAs(middle);
        (start.Next.Next!.Value > start.Next.Value).Should().BeTrue();
        (start.Next.Next.Next!.Value > start.Next.Next.Value).Should().BeTrue();
        (start.Next.Next.Next.Next!.Value > start.Next.Next.Next.Value).Should().BeTrue();
        (start.Next.Next.Next.Next.Next!.Value > start.Next.Next.Next.Next.Value).Should().BeTrue();
        (start.Next.Next.Next.Next.Next.Next!.Value > start.Next.Next.Next.Next.Next.Value).Should().BeTrue();
        start.Next.Next.Next.Next.Next.Next.Should().BeSameAs(end);
    }
}
