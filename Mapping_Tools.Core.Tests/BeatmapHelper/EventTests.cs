using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

[TestClass]
public class EventTests
{
    [TestMethod]
    public void MakeEvent_LegacyNumericColourTransformation_PreservesOriginalLine()
    {
        // Arrange
        const string line = "3,100,163,162,255";

        // Act
        Event actual = Event.MakeEvent(line);

        // Assert
        Colour colour = actual.Should().BeOfType<Colour>().Subject;
        colour.Color.Should().Be(RgbaColour.FromRgb(163, 162, 255));
        actual.GetLine().Should().Be(line);
    }
}
