using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Graph.Interpolation.Interpolators;

[TestClass]
public sealed class HalfSineInterpolatorTests
{
    [TestMethod]
    public void GetInterpolation_WithZeroParameter_UsesLinearDegenerateCase()
    {
        // Arrange
        HalfSineInterpolator interpolator = new() { P = 0 };

        // Act
        double value = interpolator.GetInterpolation(0.25);
        double slope = interpolator.GetDerivative(0.25);
        double area = interpolator.GetIntegral(0, 1);

        // Assert
        value.Should().BeApproximately(0.25, 0.000001);
        slope.Should().BeApproximately(1, 0.000001);
        area.Should().BeApproximately(0.5, 0.000001);
    }

    [TestMethod]
    public void GetInterpolation_WithOppositeParameters_MirrorsCurve()
    {
        // Arrange
        HalfSineInterpolator positive = new() { P = 1 };
        HalfSineInterpolator negative = new() { P = -1 };

        // Act
        double positiveValue = positive.GetInterpolation(0.5);
        double negativeValue = negative.GetInterpolation(0.5);
        double positiveArea = positive.GetIntegral(0, 1);
        double negativeArea = negative.GetIntegral(0, 1);

        // Assert
        positiveValue.Should().BeApproximately(Math.Sqrt(0.5), 0.000001);
        negativeValue.Should().BeApproximately(1 - Math.Sqrt(0.5), 0.000001);
        positiveArea.Should().BeApproximately(1 - negativeArea, 0.000001);
    }

    [TestMethod]
    public void GetDerivative_WithParameterOutsideRange_ClampsToSupportedCurve()
    {
        // Arrange
        HalfSineInterpolator clamped = new() { P = 1 };
        HalfSineInterpolator beyondRange = new() { P = 2 };

        // Act
        double expected = clamped.GetDerivative(0.25);
        double actual = beyondRange.GetDerivative(0.25);

        // Assert
        actual.Should().BeApproximately(expected, 0.000001);
    }

    [TestMethod]
    public void GetDerivative_WithOppositeParameters_ReturnsDifferentCurvedSlopes()
    {
        // Arrange
        HalfSineInterpolator positive = new() { P = 1 };
        HalfSineInterpolator negative = new() { P = -1 };

        // Act
        double positiveSlope = positive.GetDerivative(0.25);
        double negativeSlope = negative.GetDerivative(0.25);

        // Assert
        positiveSlope.Should().BeApproximately(Math.PI / 2 * Math.Cos(Math.PI / 8), 0.000001);
        negativeSlope.Should().BeApproximately(Math.PI / 2 * Math.Cos(3 * Math.PI / 8), 0.000001);
    }

    [TestMethod]
    public void GetIntegral_WithNonzeroInterval_ReturnsDifferenceOfPrimitives()
    {
        // Arrange
        HalfSineInterpolator positive = new() { P = 1 };
        HalfSineInterpolator negative = new() { P = -1 };
        double expectedPositive = 2 / Math.PI * (Math.Cos(Math.PI / 8) - Math.Cos(3 * Math.PI / 8));
        double expectedNegative = 0.5 - expectedPositive;

        // Act
        double positiveArea = positive.GetIntegral(0.25, 0.75);
        double negativeArea = negative.GetIntegral(0.25, 0.75);

        // Assert
        positiveArea.Should().BeApproximately(expectedPositive, 0.000001);
        negativeArea.Should().BeApproximately(expectedNegative, 0.000001);
    }
}
