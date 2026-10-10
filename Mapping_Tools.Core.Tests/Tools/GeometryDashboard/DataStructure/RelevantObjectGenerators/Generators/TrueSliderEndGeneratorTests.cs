using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class TrueSliderEndGeneratorTests
{
    [TestMethod]
    [DataRow(500.0, 1, 156.79998779296875, 1464.0)]
    [DataRow(72.0, 1, 114.0, 1036.0)]
    [DataRow(60.0, 1, 114.0, 1030.0)]
    [DataRow(61.0, 1, 113.18033, 1030.0)]
    [DataRow(60.5, 1, 114.0, 1030.0)]
    [DataRow(500.5, 1, 156.79998779296875, 1464.0)]
    [DataRow(500.0, 2, 71.2, 1964.0)]
    [DataRow(500.0, 3, 156.79998779296875, 2464.0)]
    [DataRow(30.0, 2, 164.0, 1030.0)]
    [DataRow(10.0, 10, 104.0, 1064.0)]
    [DataRow(0.0, 1, 64.0, 1000.0)]
    public void GetRelevantObjects_WithSliderTiming_ReturnsSliderBallPositionAtTailJudgement(
        double spanDuration, int spanCount, double expectedX, double expectedTime)
    {
        // Arrange
        HitObject slider = BeatmapTestData.DecodeHitObject("64,96,1000,2,0,L|164:96,1,100,0|0,0:0|0:0,0:0:0:0:");
        slider.TemporalLength = spanDuration;
        slider.Repeat = spanCount;
        TrueSliderEndGenerator generator = new();

        // Act
        RelevantPoint? result = generator.GetRelevantObjects(new RelevantHitObject(slider));

        // Assert
        result.Should().NotBeNull();
        result.Child.X.Should().BeApproximately(expectedX, 0.00001);
        result.Child.Y.Should().Be(96);
        result.CustomTime.Should().BeApproximately(expectedTime, 0.000001);
    }

    [TestMethod]
    public void GetRelevantObjects_WithTruncatedSliderPath_UsesDeclaredPixelLength()
    {
        // Arrange
        HitObject slider = BeatmapTestData.DecodeHitObject("64,96,1000,2,0,L|164:96,1,50,0|0,0:0|0:0,0:0:0:0:");
        slider.TemporalLength = 500;
        TrueSliderEndGenerator generator = new();

        // Act
        RelevantPoint? result = generator.GetRelevantObjects(new RelevantHitObject(slider));

        // Assert
        result.Should().NotBeNull();
        result.Child.X.Should().BeApproximately(110.4, 0.00001);
        result.Child.Y.Should().Be(96);
        result.CustomTime.Should().Be(1464);
    }

    [TestMethod]
    [DataRow("64,96,1000,1,0,0:0:0:0:")]
    [DataRow("64,96,1000,8,0,1500,0:0:0:0:")]
    public void GetRelevantObjects_WithNonSlider_ReturnsNull(string encodedHitObject)
    {
        // Arrange
        HitObject hitObject = BeatmapTestData.DecodeHitObject(encodedHitObject);
        TrueSliderEndGenerator generator = new();

        // Act
        RelevantPoint? result = generator.GetRelevantObjects(new RelevantHitObject(hitObject));

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void GetRelevantObjects_WithEmptySliderPath_ReturnsNull()
    {
        // Arrange
        HitObject slider = new() { IsSlider = true, Repeat = 1, TemporalLength = 500 };
        TrueSliderEndGenerator generator = new();

        // Act
        RelevantPoint? result = generator.GetRelevantObjects(new RelevantHitObject(slider));

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void GetRelevantObjects_WithFastUnequalSegments_UsesLegacyTrackingPosition()
    {
        // Arrange
        HitObject slider = BeatmapTestData.DecodeHitObject("64,96,1000,2,0,L|66:96|164:96,1,100,0|0,0:0|0:0,0:0:0:0:");
        slider.TemporalLength = 4;
        TrueSliderEndGenerator generator = new();

        // Act
        var result = generator.GetRelevantObjects(new RelevantHitObject(slider));

        // Assert
        result.Should().NotBeNull();
        result.CustomTime.Should().Be(1002);
        result.Child.X.Should().Be(115);
        result.Child.Y.Should().Be(96);
    }
}
