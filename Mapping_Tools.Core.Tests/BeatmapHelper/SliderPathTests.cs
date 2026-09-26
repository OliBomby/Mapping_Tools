using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

[TestClass]
public class SliderPathTests
{
    [TestMethod]
    public void TypedControlPoints_CreateIndependentSegments()
    {
        // Arrange
        var controlPoints = new[]
        {
            new PathControlPoint(new Vector2(42, 179), PathType.Bezier),
            new PathControlPoint(new Vector2(135, 234)),
            new PathControlPoint(new Vector2(219, 171), PathType.Bezier),
            new PathControlPoint(new Vector2(194, 100), PathType.Bezier),
            new PathControlPoint(new Vector2(266, 53)),
            new PathControlPoint(new Vector2(345, 48)),
            new PathControlPoint(new Vector2(405, 117)),
        };

        // Act
        var sliderPath = new SliderPath(controlPoints);

        // Assert
        sliderPath.SegmentStarts.Count.Should().Be(3);
    }

    [TestMethod]
    public void ExpectedDistance_ShortensAndLengthensLinearPath()
    {
        // Arrange
        var controlPoints = new[] { new Vector2(0, 0), new Vector2(100, 0) };

        // Act
        var shortened = new SliderPath(PathType.Linear, controlPoints, 40);
        var lengthened = new SliderPath(PathType.Linear, controlPoints, 150);

        // Assert
        shortened.Distance.Should().BeApproximately(40, 0.0001);
        shortened.PositionAt(1).Should().Be(new Vector2(40, 0));
        lengthened.Distance.Should().BeApproximately(150, 0.0001);
        lengthened.PositionAt(1).Should().Be(new Vector2(150, 0));
    }

    [TestMethod]
    public void EmptyPath_HasZeroDistanceAndPosition()
    {
        // Arrange
        // Act
        var sliderPath = new SliderPath(PathType.Bezier, Array.Empty<Vector2>());

        // Assert
        sliderPath.Distance.Should().Be(0);
        sliderPath.PositionAt(0.5).Should().Be(Vector2.Zero);
    }
}
