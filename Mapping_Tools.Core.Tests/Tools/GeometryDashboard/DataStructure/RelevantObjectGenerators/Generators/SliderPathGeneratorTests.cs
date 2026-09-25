using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class SliderPathGeneratorTests
{
    [TestMethod]
    public void GetRelevantObjects_WithConfiguredDensitySamplesPositionAndTimeAlongPath()
    {
        // Arrange
        HitObject slider = new("64,96,1000,2,0,L|164:96,1,100,0|0,0:0|0:0,0:0:0:0:")
        {
            TemporalLength = 500,
        };
        SliderPathGenerator generator = new();
        ((SliderPathGeneratorSettings)generator.Settings).PointDensity = 0.05;

        // Act
        RelevantPoint[]? result = generator.GetRelevantObjects(new RelevantHitObject(slider));

        // Assert
        result.Should().NotBeNull();
        result.Select(point => point.Child).Should().Equal(
            new Vector2(64, 96),
            new Vector2(89, 96),
            new Vector2(114, 96),
            new Vector2(139, 96),
            new Vector2(164, 96));
        result.Select(point => point.CustomTime).Should().Equal(1000, 1125, 1250, 1375, 1500);
    }

    [TestMethod]
    public void GetRelevantObjects_WithOneRequestedSample_UsesSliderStart()
    {
        // Arrange
        HitObject slider = new("64,96,1000,2,0,L|164:96,1,100,0|0,0:0|0:0,0:0:0:0:")
        {
            TemporalLength = 500,
        };
        SliderPathGenerator generator = new();
        ((SliderPathGeneratorSettings)generator.Settings).PointDensity = 0.01;

        // Act
        RelevantPoint[]? result = generator.GetRelevantObjects(new RelevantHitObject(slider));

        // Assert
        result.Should().ContainSingle();
        result[0].Child.Should().Be(new Vector2(64, 96));
        result[0].CustomTime.Should().Be(1000);
    }

    [TestMethod]
    public void GetRelevantObjects_WithNonSlider_ReturnsNull()
    {
        // Arrange
        HitObject circle = new("64,96,1000,1,0,0:0:0:0:");
        SliderPathGenerator generator = new();

        // Act
        RelevantPoint[]? result = generator.GetRelevantObjects(new RelevantHitObject(circle));

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void GetRelevantObjects_WithDensityBelowOneSample_ReturnsEmptyArray()
    {
        // Arrange
        HitObject slider = new("64,96,1000,2,0,L|164:96,1,100,0|0,0:0|0:0,0:0:0:0:");
        SliderPathGenerator generator = new();
        ((SliderPathGeneratorSettings)generator.Settings).PointDensity = 0.009;

        // Act
        RelevantPoint[]? result = generator.GetRelevantObjects(new RelevantHitObject(slider));

        // Assert
        result.Should().BeEmpty();
    }
}
