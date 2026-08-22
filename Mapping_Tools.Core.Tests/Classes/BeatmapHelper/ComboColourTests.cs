using Mapping_Tools.Core.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class ComboColourTests {
    [TestMethod]
    public void RgbLine_RoundTripsWithoutAlpha() {
        // Arrange
        // Act
        var colour = new ComboColour("Combo1 : 12,34,56");

        // Assert
        colour.Color.Should().Be(RgbaColour.FromRgb(12, 34, 56));
        colour.ToString().Should().Be("12,34,56");
    }

    [TestMethod]
    public void RgbaLine_RoundTripsWithAlpha() {
        // Arrange
        // Act
        var colour = new ComboColour("SliderTrackOverride : 12,34,56,78");

        // Assert
        colour.Color.Should().Be(RgbaColour.FromArgb(78, 12, 34, 56));
        colour.ToString().Should().Be("12,34,56,78");
    }

    [TestMethod]
    public void ColourJson_PreservesLegacyArgbString() {
        // Arrange
        var expected = RgbaColour.FromArgb(0x7F, 0x12, 0x34, 0x56);

        // Act
        string json = JsonConvert.SerializeObject(expected);
        var actual = JsonConvert.DeserializeObject<RgbaColour>(json);

        // Assert
        json.Should().Be("\"#7F123456\"");
        actual.Should().Be(expected);
    }

    [TestMethod]
    public void ColorChange_UpdatesPlainColourValue() {
        // Arrange
        var colour = new ComboColour(1, 2, 3);

        // Act
        colour.Color = RgbaColour.White;

        // Assert
        colour.Color.Should().Be(RgbaColour.White);
    }
}
