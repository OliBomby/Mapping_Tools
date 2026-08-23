using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.ToolHelpers.Sliders;

[TestClass]
public class BezierSubdivisionTests
{
    [TestMethod]
    public void BezierSubdivision_Transformations_PreserveExpectedGeometry()
    {
        // Arrange
        // Act
        var points = new List<Vector2> { new(0, 0), new(1, 1), new(2, 0) };
        var subdivision = new BezierSubdivision(points);

        // Assert
        subdivision.Flatness().Should().BeApproximately(1, 0.001);
        subdivision.ScaleLeft(0.5);
        (subdivision.Points[0] - new Vector2(1, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision.Points[1] - new Vector2(1.5, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision.Points[2] - new Vector2(2, 0)).Length.Should().BeApproximately(0, 0.001);
        subdivision.ScaleRight(-1);
        (subdivision.Points[0] - new Vector2(1, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision.Points[1] - new Vector2(0.5, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision.Points[2] - new Vector2(0, 0)).Length.Should().BeApproximately(0, 0.001);
        subdivision.Flatness().Should().BeApproximately(0.25, 0.001);
        subdivision.Length().Should().BeApproximately(1.207106, 0.001);
        var subdivision2 = subdivision.Prev();
        (subdivision2.Points[0] - new Vector2(2, 0)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[1] - new Vector2(1.5, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[2] - new Vector2(1, 0.5)).Length.Should().BeApproximately(0, 0.001);
        subdivision.Reverse();
        subdivision2 = subdivision.Next();
        (subdivision2.Points[0] - new Vector2(1, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[1] - new Vector2(1.5, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[2] - new Vector2(2, 0)).Length.Should().BeApproximately(0, 0.001);
        subdivision2 = subdivision.Parent();
        (subdivision2.Points[0] - new Vector2(0, 0)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[1] - new Vector2(1, 1)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[2] - new Vector2(2, 0)).Length.Should().BeApproximately(0, 0.001);
        subdivision2.Children(out subdivision, out subdivision2);
        (subdivision.Points[0] - new Vector2(0, 0)).Length.Should().BeApproximately(0, 0.001);
        (subdivision.Points[1] - new Vector2(0.5, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision.Points[2] - new Vector2(1, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[0] - new Vector2(1, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[1] - new Vector2(1.5, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[2] - new Vector2(2, 0)).Length.Should().BeApproximately(0, 0.001);
        subdivision.Increase();
        (subdivision.Points[0] - new Vector2(0, 0)).Length.Should().BeApproximately(0, 0.001);
        (subdivision.Points[1] - new Vector2(0.33333, 0.33333)).Length.Should().BeApproximately(0, 0.001);
        (subdivision.Points[2] - new Vector2(0.66667, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (subdivision.Points[3] - new Vector2(1, 0.5)).Length.Should().BeApproximately(0, 0.001);
    }

    [TestMethod]
    public void Subdivide_MixedBezierSegments_CreatesExpectedSegments()
    {
        // Arrange
        var points = new List<Vector2> { new(0, 0), new(1, 2), new(2, 0) };
        var subdivision = new BezierSubdivision(points);
        var points2 = new List<Vector2> { new(2, 0), new(4, 1), new(2, 6), new(1, 2) };
        var subdivision2 = new BezierSubdivision(points2, 0, 1);
        var slider = new LinkedList<BezierSubdivision>();
        slider.AddLast(subdivision);
        slider.AddLast(subdivision2);

        // Act
        BezierSubdivision.Subdivide(ref slider);
        var current = slider.First;
        // Assert
        (current.Value.Points[0] - new Vector2(0, 0)).Length.Should().BeApproximately(0, 0.001);
        (current.Value.Points[1] - new Vector2(0.25, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (current.Value.Points[2] - new Vector2(0.5, 0.75)).Length.Should().BeApproximately(0, 0.001);
        current = current.Next;
        current.Should().NotBeNull();
        (current.Value.Points[0] - new Vector2(0.5, 0.75)).Length.Should().BeApproximately(0, 0.001);
        (current.Value.Points[1] - new Vector2(0.75, 1)).Length.Should().BeApproximately(0, 0.001);
        (current.Value.Points[2] - new Vector2(1, 1)).Length.Should().BeApproximately(0, 0.001);
        current = current.Next;
        current.Should().NotBeNull();
        (current.Value.Points[0] - new Vector2(1, 1)).Length.Should().BeApproximately(0, 0.001);
        (current.Value.Points[1] - new Vector2(1.25, 1)).Length.Should().BeApproximately(0, 0.001);
        (current.Value.Points[2] - new Vector2(1.5, 0.75)).Length.Should().BeApproximately(0, 0.001);
        current = current.Next;
        current.Should().NotBeNull();
        (current.Value.Points[0] - new Vector2(1.5, 0.75)).Length.Should().BeApproximately(0, 0.001);
        (current.Value.Points[1] - new Vector2(1.75, 0.5)).Length.Should().BeApproximately(0, 0.001);
        (current.Value.Points[2] - new Vector2(2, 0)).Length.Should().BeApproximately(0, 0.001);
        current = current.Next;
        current.Should().NotBeNull();
        subdivision2 = current.Value;
        for (int i = subdivision2.Level; i > 0; i--) subdivision2 = subdivision2.Parent();
        (subdivision2.Points[0] - new Vector2(2, 0)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[1] - new Vector2(4, 1)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[2] - new Vector2(2, 6)).Length.Should().BeApproximately(0, 0.001);
        (subdivision2.Points[3] - new Vector2(1, 2)).Length.Should().BeApproximately(0, 0.001);
    }

    [TestMethod]
    public void LengthToT_VariousLengths_ReturnsExpectedParameters()
    {
        // Arrange
        // Act
        var points = new List<Vector2> { new(0, 0), new(4, 6), new(2, 1) };
        var subdivision = new BezierSubdivision(points);
        // Assert
        subdivision.LengthToT(2).Should().BeApproximately(0.1608, 0.01);
        subdivision.LengthToT(4).Should().BeApproximately(0.4568, 0.01);
        subdivision.LengthToT(8).Should().BeApproximately(1.1077, 0.01);
        subdivision.LengthToT(32).Should().BeApproximately(2.0559, 0.01);
    }

    [TestMethod]
    public void SubdividedApproximationLength_BezierCurve_MatchesSliderPathDistance()
    {
        // Arrange
        // Act
        var points = new List<Vector2> { new(0, 0), new(100, 100), new(200, 0) };
        var subdivision = new BezierSubdivision(points);
        double length = new SliderPath(PathType.Bezier, points.ToArray()).Distance;
        double length2 = subdivision.SubdividedApproximationLength();
        // Assert
        length2.Should().BeApproximately(length, 0.001);
        subdivision.LengthToT(length / 2).Should().BeApproximately(0.5, 0.01);
        subdivision.LengthToT(length).Should().BeApproximately(1, 0.01);
        subdivision.LengthToT(0).Should().BeApproximately(0, 0.01);
    }
}
