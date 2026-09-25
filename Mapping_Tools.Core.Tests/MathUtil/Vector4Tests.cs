using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.MathUtil;

[TestClass]
public sealed class Vector4Tests
{
    [TestMethod]
    public void Normalized_WithNonUnitVector_PreservesDirectionAndUnitLength()
    {
        // Arrange
        Vector4 vector = new(0, 3, 4, 0);

        // Act
        Vector4 normalized = vector.Normalized();

        // Assert
        normalized.X.Should().Be(0);
        normalized.Y.Should().BeApproximately(0.6, 0.000001);
        normalized.Z.Should().BeApproximately(0.8, 0.000001);
        normalized.W.Should().Be(0);
        normalized.Length.Should().BeApproximately(1, 0.000001);
        vector.Length.Should().Be(5);
    }

    [TestMethod]
    public void Dot_WithMixedSigns_UsesAllFourCoordinates()
    {
        // Arrange
        Vector4 first = new(1, 2, 3, 4);
        Vector4 second = new(-1, 0, 2, 0.5);

        // Act
        double dot = Vector4.Dot(first, second);

        // Assert
        dot.Should().Be(7);
    }

    [TestMethod]
    public void Lerp_WithInteriorBlend_InterpolatesAllFourCoordinates()
    {
        // Arrange
        Vector4 first = new(0, 4, -4, 8);
        Vector4 second = new(8, 0, 4, 0);

        // Act
        Vector4 interpolated = Vector4.Lerp(first, second, 0.25);

        // Assert
        interpolated.Should().Be(new Vector4(2, 3, -2, 6));
    }

    [TestMethod]
    public void Clamp_WithMixedOutOfRangeCoordinates_ClampsEachCoordinate()
    {
        // Arrange
        Vector4 value = new(-1, 2, 12, 5);
        Vector4 minimum = new(0, 0, 0, 0);
        Vector4 maximum = new(10, 10, 10, 4);

        // Act
        Vector4 clamped = Vector4.Clamp(value, minimum, maximum);

        // Assert
        clamped.Should().Be(new Vector4(0, 2, 10, 4));
    }
}
