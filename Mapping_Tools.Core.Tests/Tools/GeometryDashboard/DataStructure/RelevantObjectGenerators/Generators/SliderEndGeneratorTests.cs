using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class SliderEndGeneratorTests
{
    [TestMethod]
    public void GetRelevantObjects_WithExpectedSliderLength_ReturnsPlayableEndAtSliderEndTime()
    {
        // Arrange
        HitObject slider = new("64,96,1000,2,0,L|164:96,1,50,0|0,0:0|0:0,0:0:0:0:")
        {
            TemporalLength = 500,
        };
        SliderEndGenerator generator = new();

        // Act
        RelevantPoint? result = generator.GetRelevantObjects(new RelevantHitObject(slider));

        // Assert
        result.Should().NotBeNull();
        result!.Child.Should().Be(new Vector2(114, 96));
        result.CustomTime.Should().Be(1500);
    }

    [TestMethod]
    public void GetRelevantObjects_WithCircle_ReturnsNoSliderEnd()
    {
        // Arrange
        HitObject circle = new("64,96,1000,1,0,0:0:0:0:");
        SliderEndGenerator generator = new();

        // Act
        RelevantPoint? result = generator.GetRelevantObjects(new RelevantHitObject(circle));

        // Assert
        result.Should().BeNull();
    }
}
