using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Graph.Interpolation.Interpolators;

[TestClass]
public sealed class WaveInterpolatorTests
{
    [TestMethod]
    public void Function_WithPositiveParameter_UsesSineWave()
    {
        // Arrange
        WaveInterpolator interpolator = new() { P = 1 };

        // Act
        double quarter = interpolator.Function(0.25);
        double middle = interpolator.Function(0.5);
        double end = interpolator.Function(1);

        // Assert
        quarter.Should().BeApproximately((1 - Math.Cos(Math.PI / 4)) / 2, 0.000001);
        middle.Should().BeApproximately(0.5, 0.000001);
        end.Should().BeApproximately(1, 0.000001);
    }

    [TestMethod]
    public void Function_WithNegativeParameter_UsesTriangleWave()
    {
        // Arrange
        WaveInterpolator interpolator = new() { P = -1 };

        // Act
        double quarter = interpolator.Function(0.25);
        double middle = interpolator.Function(0.5);
        double end = interpolator.Function(1);

        // Assert
        quarter.Should().BeApproximately(0.25, 0.000001);
        middle.Should().BeApproximately(0.5, 0.000001);
        end.Should().BeApproximately(1, 0.000001);
    }

    [TestMethod]
    public void GetDerivative_WithSineAndTriangleModes_ReturnsExpectedSlope()
    {
        // Arrange
        WaveInterpolator sine = new() { P = 1 };
        WaveInterpolator triangle = new() { P = -1 };

        // Act
        double sineSlope = sine.GetDerivative(0.5);
        double triangleRisingSlope = triangle.GetDerivative(0.5);
        double triangleFallingSlope = triangle.GetDerivative(1.5);

        // Assert
        sineSlope.Should().BeApproximately(Math.PI / 2, 0.000001);
        triangleRisingSlope.Should().Be(1);
        triangleFallingSlope.Should().Be(-1);
    }

    [TestMethod]
    public void GetIntegral_WithEitherWaveMode_ReturnsAreaOverCompletePeriod()
    {
        // Arrange
        WaveInterpolator sine = new() { P = 1 };
        WaveInterpolator triangle = new() { P = -1 };

        // Act
        double sineArea = sine.GetIntegral(0, 2);
        double triangleArea = triangle.GetIntegral(0, 2);

        // Assert
        sineArea.Should().BeApproximately(1, 0.000001);
        triangleArea.Should().BeApproximately(1, 0.000001);
    }

    [TestMethod]
    public void GetIntegral_WithNonzeroPartialInterval_DistinguishesWaveShapes()
    {
        // Arrange
        WaveInterpolator sine = new() { P = 1 };
        WaveInterpolator triangle = new() { P = -1 };
        double sineExpected = 0.25 - (Math.Sin(0.6 * Math.PI) - Math.Sin(0.1 * Math.PI)) / (2 * Math.PI);
        double triangleExpected = (0.6 * 0.6 - 0.1 * 0.1) / 2;

        // Act
        double sineArea = sine.GetIntegral(0.1, 0.6);
        double triangleArea = triangle.GetIntegral(0.1, 0.6);

        // Assert
        sineArea.Should().BeApproximately(sineExpected, 0.000001);
        triangleArea.Should().BeApproximately(triangleExpected, 0.000001);
    }

    [TestMethod]
    public void Function_WithTriangleWaveAfterPeak_UsesFallingEdge()
    {
        // Arrange
        WaveInterpolator interpolator = new() { P = -1 };

        // Act
        double value = interpolator.Function(1.5);
        double area = interpolator.GetIntegral(1, 1.5);

        // Assert
        value.Should().BeApproximately(0.5, 0.000001);
        area.Should().BeApproximately(0.375, 0.000001);
    }

    [TestMethod]
    public void GetInverse_WithEitherWaveMode_ReturnsPointMatchingRequestedValue()
    {
        // Arrange
        WaveInterpolator sine = new() { P = 1 };
        WaveInterpolator triangle = new() { P = -1 };

        // Act
        double sineTime = sine.GetInverse(0.25).First();
        double triangleTime = triangle.GetInverse(0.25).First();

        // Assert
        sineTime.Should().BeApproximately(1d / 3, 0.000001);
        triangleTime.Should().BeApproximately(0.25, 0.000001);
        sine.Function(sineTime).Should().BeApproximately(0.25, 0.000001);
        triangle.Function(triangleTime).Should().BeApproximately(0.25, 0.000001);
    }

    [TestMethod]
    public void GetInverse_WithEitherWaveMode_ReturnsDescendingRoot()
    {
        // Arrange
        WaveInterpolator sine = new() { P = 1 };
        WaveInterpolator triangle = new() { P = -1 };

        // Act
        double sineTime = sine.GetInverse(0.25).Skip(1).First();
        double triangleTime = triangle.GetInverse(0.25).Skip(1).First();

        // Assert
        sineTime.Should().BeApproximately(5d / 3, 0.000001);
        triangleTime.Should().BeApproximately(1.75, 0.000001);
        sine.Function(sineTime).Should().BeApproximately(0.25, 0.000001);
        triangle.Function(triangleTime).Should().BeApproximately(0.25, 0.000001);
    }
}
