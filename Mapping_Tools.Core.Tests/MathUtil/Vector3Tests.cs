using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.MathUtil;

[TestClass]
public sealed class Vector3Tests
{
    [TestMethod]
    public void Cross_WithObliqueVectors_ReturnsPerpendicularVector()
    {
        // Arrange
        Vector3 first = new(1, 2, 3);
        Vector3 second = new(4, 5, 6);

        // Act
        Vector3 cross = Vector3.Cross(first, second);

        // Assert
        cross.Should().Be(new Vector3(-3, 6, -3));
        Vector3.Dot(cross, first).Should().Be(0);
        Vector3.Dot(cross, second).Should().Be(0);
    }

    [TestMethod]
    public void Clamp_WithValuesOnBothSidesOfBounds_ClampsEachCoordinateIndependently()
    {
        // Arrange
        Vector3 value = new(-2, 5, 10);
        Vector3 minimum = new(0, 0, 0);
        Vector3 maximum = new(4, 6, 8);

        // Act
        Vector3 clamped = Vector3.Clamp(value, minimum, maximum);

        // Assert
        clamped.Should().Be(new Vector3(0, 5, 8));
    }

    [TestMethod]
    public void BaryCentric_WithInteriorWeights_UsesAllThreeVertices()
    {
        // Arrange
        Vector3 first = new(0, 0, 0);
        Vector3 second = new(10, 0, 2);
        Vector3 third = new(0, 10, 4);

        // Act
        Vector3 point = Vector3.BaryCentric(first, second, third, 0.2, 0.3);

        // Assert
        point.X.Should().BeApproximately(2, 0.000001);
        point.Y.Should().BeApproximately(3, 0.000001);
        point.Z.Should().BeApproximately(1.6, 0.000001);
    }

    [TestMethod]
    public void CalculateAngle_WithPerpendicularVectors_ReturnsRightAngle()
    {
        // Arrange
        Vector3 first = new(2, 0, 0);
        Vector3 second = new(0, -3, 0);

        // Act
        double angle = Vector3.CalculateAngle(first, second);

        // Assert
        angle.Should().BeApproximately(Math.PI / 2, 0.000001);
    }
}
