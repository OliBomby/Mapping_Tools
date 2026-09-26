using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

[TestClass]
public class EventTests
{
    [TestMethod]
    public void DecodeStoryboardEvent_LegacyNumericColour_ParsesAndRoundTrips()
    {
        // Arrange
        const string line = "3,100,163,162,255";

        // Act
        var actual = BeatmapTestData.DecodeStoryboardEvent(line);

        // Assert
        var colour = actual.Should().BeOfType<Colour>().Subject;
        colour.Color.Should().Be(RgbaColour.FromRgb(163, 162, 255));
        BeatmapTestData.EncodeStoryboardEvent(actual, 14).Should().Be(line);
    }
}
