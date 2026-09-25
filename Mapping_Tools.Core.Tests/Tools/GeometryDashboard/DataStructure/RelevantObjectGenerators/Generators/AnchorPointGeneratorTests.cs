using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class AnchorPointGeneratorTests
{
    [TestMethod]
    public void GetRelevantObjects_WithCircle_ReturnsNoGeneratedPoints()
    {
        // Arrange
        AnchorPointGenerator generator = new();
        RelevantHitObject circle = new(new HitObject("256,192,100,1,0,0:0:0:0:"));

        // Act
        IEnumerable<RelevantPoint>? points = generator.GetRelevantObjects(circle);

        // Assert
        points.Should().BeNull();
    }

    [TestMethod]
    public void GetRelevantObjects_WithThreeSliderAnchors_InterpolatesFirstMiddleAndLastTimes()
    {
        // Arrange
        AnchorPointGenerator generator = new();
        HitObject slider = new("0,0,100,2,0,L|10:0|20:0,1,20") { EndTime = 300 };
        RelevantHitObject relevantSlider = new(slider);

        // Act
        RelevantPoint[] points = generator.GetRelevantObjects(relevantSlider)!.ToArray();

        // Assert
        points.Select(point => point.Child).Should().Equal(
            new Vector2(0, 0), new Vector2(10, 0), new Vector2(20, 0));
        points.Select(point => point.CustomTime).Should().Equal(100, 200, 300);
    }
}
