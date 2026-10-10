using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.MathUtil;

[TestClass]
public sealed class LineSegmentTests
{
    [TestMethod]
    public void Intersection_WithSharedEndpoint_ReturnsEndpoint()
    {
        // Arrange
        LineSegment horizontal = new(new Vector2(0, 0), new Vector2(4, 0));
        LineSegment vertical = new(new Vector2(4, 0), new Vector2(4, 4));

        // Act
        bool intersects = LineSegment.Intersection(ref horizontal, ref vertical, out Vector2 point);

        // Assert
        intersects.Should().BeTrue();
        point.Should().Be(new Vector2(4, 0));
    }

    [TestMethod]
    public void Intersection_WithCrossingInfiniteLinesOutsideSegments_ReturnsFalse()
    {
        // Arrange
        LineSegment horizontal = new(new Vector2(0, 0), new Vector2(2, 0));
        LineSegment vertical = new(new Vector2(3, -1), new Vector2(3, 1));

        // Act
        bool intersects = LineSegment.Intersection(ref horizontal, ref vertical, out Vector2 point);

        // Assert
        intersects.Should().BeFalse();
        double.IsNaN(point.X).Should().BeTrue();
        double.IsNaN(point.Y).Should().BeTrue();
    }

    [TestMethod]
    public void Intersection_WithParallelSegments_ReturnsFalse()
    {
        // Arrange
        LineSegment first = new(new Vector2(0, 0), new Vector2(2, 0));
        LineSegment second = new(new Vector2(0, 1), new Vector2(2, 1));

        // Act
        bool intersects = LineSegment.Intersection(ref first, ref second, out _);

        // Assert
        intersects.Should().BeFalse();
    }

    [TestMethod]
    public void Intersection_WithObliqueSegments_ReturnsNonOriginCrossing()
    {
        // Arrange
        LineSegment rising = new(new Vector2(1, 1), new Vector2(5, 5));
        LineSegment falling = new(new Vector2(1, 5), new Vector2(5, 1));

        // Act
        bool intersects = LineSegment.Intersection(ref rising, ref falling, out Vector2 point);

        // Assert
        intersects.Should().BeTrue();
        point.Should().Be(new Vector2(3, 3));
    }

    [TestMethod]
    public void Intersection_WithCrossingBeyondSecondSegment_ReturnsFalse()
    {
        // Arrange
        LineSegment rising = new(new Vector2(1, 1), new Vector2(5, 5));
        LineSegment shortVertical = new(new Vector2(3, 0), new Vector2(3, 2));

        // Act
        bool intersects = LineSegment.Intersection(ref rising, ref shortVertical, out _);

        // Assert
        intersects.Should().BeFalse();
    }

    [DataRow(-2, 2)]
    [DataRow(2, 0)]
    [DataRow(6, 2)]
    [TestMethod]
    public void Distance_WithPointBeforeOnOrAfterSegment_ClampsProjection(double x, double expected)
    {
        // Arrange
        LineSegment segment = new(new Vector2(0, 0), new Vector2(4, 0));

        // Act
        double distance = LineSegment.Distance(segment, new Vector2(x, 0));

        // Assert
        distance.Should().Be(expected);
    }

    [TestMethod]
    public void Distance_WithZeroLengthSegment_UsesEndpointDistance()
    {
        // Arrange
        LineSegment segment = new(new Vector2(1, 2), new Vector2(1, 2));

        // Act
        double distance = LineSegment.Distance(segment, new Vector2(4, 6));

        // Assert
        distance.Should().Be(5);
    }

    [TestMethod]
    public void Distance_WithObliqueSegmentAndInteriorProjection_ReturnsPerpendicularDistance()
    {
        // Arrange
        LineSegment segment = new(new Vector2(1, 1), new Vector2(5, 5));

        // Act
        double distance = LineSegment.Distance(segment, new Vector2(1, 5));

        // Assert
        distance.Should().BeApproximately(Math.Sqrt(8), 0.000001);
    }
}
