using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class SymmetryGeneratorTests
{
    [TestMethod]
    public void GetRelevantObjects_WithSelectedLockedAxis_ReflectsPointAcrossLine()
    {
        // Arrange
        SymmetryGenerator generator = new();
        RelevantLine axis = HorizontalAxis();
        RelevantPoint point = new(new Vector2(5, 7));

        // Act
        RelevantPoint? reflected = generator.GetRelevantObjects(axis, point);

        // Assert
        reflected.Should().NotBeNull();
        reflected.Child.X.Should().BeApproximately(5, 0.000001);
        reflected.Child.Y.Should().BeApproximately(-7, 0.000001);
    }

    [TestMethod]
    public void GetRelevantObjects_WithUnlockedAxis_RejectsReflection()
    {
        // Arrange
        SymmetryGenerator generator = new();
        RelevantLine axis = new(Line2.FromPoints(new Vector2(0, 0), new Vector2(10, 0)))
        {
            IsSelected = true,
        };

        // Act
        RelevantPoint? reflected = generator.GetRelevantObjects(axis, new RelevantPoint(new Vector2(5, 7)));

        // Assert
        reflected.Should().BeNull();
    }

    [TestMethod]
    public void GetRelevantObjects_WithCircle_PreservesRadiusAndReflectsCentre()
    {
        // Arrange
        SymmetryGenerator generator = new();
        RelevantLine axis = HorizontalAxis();
        RelevantCircle circle = new(new Circle(new Vector2(5, 7), 3));

        // Act
        RelevantCircle? reflected = generator.GetRelevantObjects(axis, circle);

        // Assert
        reflected.Should().NotBeNull();
        reflected.Child.Centre.X.Should().BeApproximately(5, 0.000001);
        reflected.Child.Centre.Y.Should().BeApproximately(-7, 0.000001);
        reflected.Child.Radius.Should().Be(3);
    }

    [TestMethod]
    public void GetRelevantObjects_WithLineAndAxisInEitherOrder_ReflectsBothEndpoints()
    {
        // Arrange
        SymmetryGenerator generator = new();
        RelevantLine axis = HorizontalAxis();
        RelevantLine line = new(Line2.FromPoints(new Vector2(5, 7), new Vector2(10, 9)));

        // Act
        RelevantLine? forward = generator.GetRelevantObjects(axis, line);
        RelevantLine? reverse = generator.GetRelevantObjects(line, axis);

        // Assert
        forward.Should().NotBeNull();
        reverse.Should().NotBeNull();
        forward.Child.PositionVector.X.Should().BeApproximately(5, 0.000001);
        forward.Child.PositionVector.Y.Should().BeApproximately(-7, 0.000001);
        forward.Child.DirectionVector.X.Should().BeApproximately(5, 0.000001);
        forward.Child.DirectionVector.Y.Should().BeApproximately(-2, 0.000001);
        reverse.Child.PositionVector.Should().Be(forward.Child.PositionVector);
        reverse.Child.DirectionVector.Should().Be(forward.Child.DirectionVector);
    }

    private static RelevantLine HorizontalAxis()
    {
        return new RelevantLine(Line2.FromPoints(new Vector2(0, 0), new Vector2(10, 0)))
        {
            IsSelected = true,
            IsLocked = true,
        };
    }
}
