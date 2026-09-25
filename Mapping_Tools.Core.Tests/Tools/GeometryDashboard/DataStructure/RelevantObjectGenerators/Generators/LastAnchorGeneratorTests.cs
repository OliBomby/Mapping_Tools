using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class LastAnchorGeneratorTests
{
    [TestMethod]
    public void GetRelevantObjects_WithSlider_ReturnsFinalCurvePointAtSliderEndTime()
    {
        // Arrange
        LastAnchorGenerator generator = new();
        HitObject slider = new("0,0,100,2,0,L|10:0|20:0,1,20") { EndTime = 300 };

        // Act
        RelevantPoint? point = generator.GetRelevantObjects(new RelevantHitObject(slider));

        // Assert
        point.Should().NotBeNull();
        point.Child.Should().Be(new Vector2(20, 0));
        point.CustomTime.Should().Be(300);
    }

    [TestMethod]
    public void GetRelevantObjects_WithCircle_ReturnsNoPoint()
    {
        // Arrange
        LastAnchorGenerator generator = new();
        HitObject circle = new("0,0,100,1,0,0:0:0:0:");

        // Act
        RelevantPoint? point = generator.GetRelevantObjects(new RelevantHitObject(circle));

        // Assert
        point.Should().BeNull();
    }
}
