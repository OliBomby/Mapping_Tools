using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class ScaleRotateGeneratorTests
{
    [TestMethod]
    public void GetRelevantObjects_WithLockedSelectedOrigin_RotatesAndScalesPoint()
    {
        // Arrange
        ScaleRotateGenerator generator = new();
        ((ScaleRotateGeneratorSettings)generator.Settings).Scalar = 2;
        RelevantPoint origin = new(new Vector2(100, 100)) { IsSelected = true, IsLocked = true };
        RelevantPoint point = new(new Vector2(110, 100));

        // Act
        RelevantPoint? transformed = generator.GetRelevantObjects(origin, point);

        // Assert
        transformed.Should().NotBeNull();
        transformed.Child.X.Should().BeApproximately(80, 0.000001);
        transformed.Child.Y.Should().BeApproximately(100, 0.000001);
    }

    [TestMethod]
    public void GetRelevantObjects_WithOriginAsSecondPoint_UsesSameTransform()
    {
        // Arrange
        ScaleRotateGenerator generator = new();
        RelevantPoint origin = new(new Vector2(100, 100)) { IsSelected = true, IsLocked = true };
        RelevantPoint point = new(new Vector2(110, 100));

        // Act
        RelevantPoint? transformed = generator.GetRelevantObjects(point, origin);

        // Assert
        transformed.Should().NotBeNull();
        transformed.Child.X.Should().BeApproximately(90, 0.000001);
        transformed.Child.Y.Should().BeApproximately(100, 0.000001);
    }

    [TestMethod]
    public void GetRelevantObjects_WithUnlockedOrigin_RejectsTransform()
    {
        // Arrange
        ScaleRotateGenerator generator = new();
        RelevantPoint origin = new(new Vector2(100, 100)) { IsSelected = true };
        RelevantPoint point = new(new Vector2(110, 100));

        // Act
        RelevantPoint? transformed = generator.GetRelevantObjects(origin, point);

        // Assert
        transformed.Should().BeNull();
    }

    [TestMethod]
    public void GetRelevantObjects_WithCircle_ScalesRadiusAndRotatesCentre()
    {
        // Arrange
        ScaleRotateGenerator generator = new();
        ((ScaleRotateGeneratorSettings)generator.Settings).Scalar = 2;
        RelevantPoint origin = new(new Vector2(100, 100)) { IsSelected = true, IsLocked = true };
        RelevantCircle circle = new(new Circle(new Vector2(110, 100), 5));

        // Act
        RelevantCircle? transformed = generator.GetRelevantObjects(origin, circle);

        // Assert
        transformed.Should().NotBeNull();
        transformed.Child.Centre.X.Should().BeApproximately(80, 0.000001);
        transformed.Child.Centre.Y.Should().BeApproximately(100, 0.000001);
        transformed.Child.Radius.Should().Be(10);
    }

    [TestMethod]
    public void GetRelevantObjects_WithLine_RotatesBothAnchorAndDirection()
    {
        // Arrange
        ScaleRotateGenerator generator = new();
        RelevantPoint origin = new(new Vector2(100, 100)) { IsSelected = true, IsLocked = true };
        RelevantLine line = new(Line2.FromPoints(new Vector2(110, 100), new Vector2(120, 100)));

        // Act
        RelevantLine? transformed = generator.GetRelevantObjects(origin, line);

        // Assert
        transformed.Should().NotBeNull();
        transformed.Child.PositionVector.X.Should().BeApproximately(90, 0.000001);
        transformed.Child.PositionVector.Y.Should().BeApproximately(100, 0.000001);
        transformed.Child.DirectionVector.X.Should().BeApproximately(-10, 0.000001);
        transformed.Child.DirectionVector.Y.Should().BeApproximately(0, 0.000001);
    }
}
