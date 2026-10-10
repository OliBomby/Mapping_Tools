using Avalonia;
using Mapping_Tools.Desktop.Controls.Timeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls.Timeline;

[TestClass]
public sealed class TimelineControlTests
{
    [TestMethod]
    public void MarkerAt_WithMarkerAndReservedArea_UsesLegacyWidth()
    {
        // Arrange
        TimelineMarker expected = new(500, TimelineMarkerKind.Accent);
        TimelineControl control = new()
        {
            StartTime = 0,
            EndTime = 1000,
            Markers = [expected],
        };
        control.Arrange(new Rect(0, 0, 200, 100));

        // Act
        var marker = control.MarkerAt(46);
        var reservedAreaMarker = control.MarkerAt(145);

        // Assert
        marker.Should().BeSameAs(expected);
        reservedAreaMarker.Should().BeNull();
    }

    [TestMethod]
    public void Measure_WithoutExplicitHeight_RequestsOnlyDrawnHeight()
    {
        // Arrange
        TimelineControl control = new();

        // Act
        control.Measure(new Size(300, double.PositiveInfinity));

        // Assert
        control.DesiredSize.Height.Should().Be(66);
    }

    [TestMethod]
    public void FormatToolTip_UsesLegacyTimestampOnly()
    {
        // Arrange
        TimelineMarker marker = new(61_005, TimelineMarkerKind.Removed);

        // Act
        string tooltip = TimelineControl.FormatToolTip(marker);

        // Assert
        tooltip.Should().Be("01:01:005");
    }
}
