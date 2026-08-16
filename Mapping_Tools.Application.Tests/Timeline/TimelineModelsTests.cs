using Mapping_Tools.Application.Timeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Timeline;

[TestClass]
public sealed class TimelineModelsTests
{
    [TestMethod]
    public void Constructor_WithEmptyRange_UsesLegacyMinimumSpanAndElevenTicks()
    {
        // Arrange
        TimelineScale scale = new(0, 0);

        // Act
        IReadOnlyList<double> ticks = scale.GetTicks();

        // Assert
        scale.EndTime.Should().Be(20);
        ticks.Should().HaveCount(11);
        ticks.First().Should().Be(0);
        ticks.Last().Should().Be(20);
    }

    [TestMethod]
    public void ToUnit_WithBoundaryAndOutsideTimes_ClampsToViewport()
    {
        // Arrange
        TimelineScale scale = new(100, 1100);

        // Act
        double[] positions = new double[] { -100, 100, 600, 1100, 2000 }
            .Select(scale.ToUnit)
            .ToArray();

        // Assert
        positions.Should().Equal(0, 0, 0.5, 1, 1);
    }

    [TestMethod]
    public void FindNearest_WithOverlappingMarkers_UsesStableTimeOrder()
    {
        // Arrange
        TimelineScale scale = new(0, 1000);
        TimelineMarker later = new(505, TimelineMarkerKind.Removed);
        TimelineMarker earlier = new(495, TimelineMarkerKind.Added);

        // Act
        TimelineMarker? marker = scale.FindNearest([later, earlier], 50, 100, 2);

        // Assert
        marker.Should().BeSameAs(earlier);
    }

    [TestMethod]
    public void FormatMarker_WithLongTimestamp_DoesNotWrapMinutes()
    {
        // Arrange
        double timestamp = TimeSpan.FromMinutes(61).Add(TimeSpan.FromMilliseconds(5)).TotalMilliseconds;

        // Act
        string value = TimelineScale.FormatMarker(timestamp);

        // Assert
        value.Should().Be("61:00:005");
    }

}
