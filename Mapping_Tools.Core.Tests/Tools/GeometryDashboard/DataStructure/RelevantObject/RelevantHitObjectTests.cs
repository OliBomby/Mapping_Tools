using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObject;

[TestClass]
public sealed class RelevantHitObjectTests
{
    [TestMethod]
    public void Difference_WithDifferentSliderTypes_ReturnsPositiveInfinity()
    {
        // Arrange
        RelevantHitObject linear = new(new HitObject("64,96,1000,2,0,L|164:96,1,100,0|0,0:0|0:0,0:0:0:0:"));
        RelevantHitObject bezier = new(new HitObject("64,96,1000,2,0,B|164:96,1,100,0|0,0:0|0:0,0:0:0:0:"));

        // Act
        double difference = linear.Difference(bezier);

        // Assert
        difference.Should().Be(double.PositiveInfinity);
    }

    [TestMethod]
    public void Difference_WithSameSliderTypeAndPointCount_ReturnsMeanSquaredCoordinateDifference()
    {
        // Arrange
        RelevantHitObject first = new(new HitObject("64,96,1000,2,0,L|164:96|164:116,1,100,0|0,0:0|0:0,0:0:0:0:"));
        RelevantHitObject second = new(new HitObject("66,96,1000,2,0,L|164:98|164:116,1,100,0|0,0:0|0:0,0:0:0:0:"));

        // Act
        double difference = first.Difference(second);

        // Assert
        difference.Should().BeApproximately(8d / 3, 0.0001);
    }
}
