using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ObjectVisualiser;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.ObjectVisualiser;

[TestClass]
public sealed class ObjectVisualiserTransformTests
{
    [TestMethod]
    public void Fit_WithPlayfieldBounds_PreservesYAxisAndCentersContent()
    {
        // Arrange
        ObjectVisualiserBounds bounds = new(0, 0, 512, 384);

        // Act
        ObjectVisualiserTransform transform = ObjectVisualiserTransform.Fit(bounds, new Vector2(1024, 768));

        // Assert
        transform.Scale.Should().Be(2);
        transform.WorldToViewport(new Vector2(0, 0)).Should().Be(new Vector2(0, 0));
        transform.WorldToViewport(new Vector2(512, 384)).Should().Be(new Vector2(1024, 768));
    }

    [TestMethod]
    public void ZoomAt_WithAnchorPoint_LeavesAnchorStationary()
    {
        // Arrange
        ObjectVisualiserTransform transform = new(2, new Vector2(10, 20));
        Vector2 anchor = new(100, 80);

        // Act
        ObjectVisualiserTransform zoomed = transform.ZoomAt(anchor, 2);

        // Assert
        zoomed.WorldToViewport(transform.ViewportToWorld(anchor)).Should().Be(anchor);
        zoomed.Scale.Should().Be(4);
    }

    [TestMethod]
    public void PanBy_WithViewportDelta_ChangesOnlyOffset()
    {
        // Arrange
        ObjectVisualiserTransform transform = new(3, new Vector2(4, 5));

        // Act
        ObjectVisualiserTransform panned = transform.PanBy(new Vector2(7, -2));

        // Assert
        panned.Scale.Should().Be(3);
        panned.Offset.Should().Be(new Vector2(11, 3));
    }

    [TestMethod]
    public void DistanceTo_WithPointOnPolyline_ReturnsZero()
    {
        // Arrange
        ObjectVisualiserPath path = new([new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10)]);

        // Act
        double distance = path.DistanceTo(new Vector2(10, 4));

        // Assert
        distance.Should().Be(0);
    }

    [TestMethod]
    public void Constructor_WithMutablePointSource_TakesAnImmutableSnapshot()
    {
        // Arrange
        List<Vector2> points = [new Vector2(0, 0), new Vector2(10, 0)];
        ObjectVisualiserPath path = new(points);

        // Act
        points[1] = new Vector2(100, 0);

        // Assert
        path.Length.Should().Be(10);
        path.Points.Should().Equal(new Vector2(0, 0), new Vector2(10, 0));
    }
}
