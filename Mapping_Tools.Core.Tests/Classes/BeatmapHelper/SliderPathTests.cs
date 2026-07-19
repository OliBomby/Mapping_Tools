using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class SliderPathTests {
    [TestMethod]
    public void RepeatedControlPoints_CreateIndependentSegments() {
        var controlPoints = new[] {
            new Vector2(42, 179),
            new Vector2(135, 234),
            new Vector2(219, 171),
            new Vector2(219, 171),
            new Vector2(194, 100),
            new Vector2(194, 100),
            new Vector2(266, 53),
            new Vector2(345, 48),
            new Vector2(405, 117)
        };

        var sliderPath = new SliderPath(PathType.Bezier, controlPoints);

        Assert.AreEqual(3, sliderPath.SegmentStarts.Count);
    }

    [TestMethod]
    public void ExpectedDistance_ShortensAndLengthensLinearPath() {
        var controlPoints = new[] { new Vector2(0, 0), new Vector2(100, 0) };

        var shortened = new SliderPath(PathType.Linear, controlPoints, 40);
        var lengthened = new SliderPath(PathType.Linear, controlPoints, 150);

        Assert.AreEqual(40, shortened.Distance, 0.0001);
        Assert.AreEqual(new Vector2(40, 0), shortened.PositionAt(1));
        Assert.AreEqual(150, lengthened.Distance, 0.0001);
        Assert.AreEqual(new Vector2(150, 0), lengthened.PositionAt(1));
    }

    [TestMethod]
    public void EmptyPath_HasZeroDistanceAndPosition() {
        var sliderPath = new SliderPath(PathType.Bezier, Array.Empty<Vector2>());

        Assert.AreEqual(0, sliderPath.Distance);
        Assert.AreEqual(Vector2.Zero, sliderPath.PositionAt(0.5));
    }
}
