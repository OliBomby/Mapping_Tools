using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class ComboColourTests {
    [TestMethod]
    public void RgbLine_RoundTripsWithoutAlpha() {
        var colour = new ComboColour("Combo1 : 12,34,56");

        Assert.AreEqual(RgbaColour.FromRgb(12, 34, 56), colour.Color);
        Assert.AreEqual("12,34,56", colour.ToString());
    }

    [TestMethod]
    public void RgbaLine_RoundTripsWithAlpha() {
        var colour = new ComboColour("SliderTrackOverride : 12,34,56,78");

        Assert.AreEqual(RgbaColour.FromArgb(78, 12, 34, 56), colour.Color);
        Assert.AreEqual("12,34,56,78", colour.ToString());
    }

    [TestMethod]
    public void ColourJson_PreservesLegacyArgbString() {
        var expected = RgbaColour.FromArgb(0x7F, 0x12, 0x34, 0x56);

        string json = JsonConvert.SerializeObject(expected);
        var actual = JsonConvert.DeserializeObject<RgbaColour>(json);

        Assert.AreEqual("\"#7F123456\"", json);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ColorChange_RaisesPropertyChanged() {
        var colour = new ComboColour(1, 2, 3);
        string? changedProperty = null;
        colour.PropertyChanged += (_, args) => changedProperty = args.PropertyName;

        colour.Color = RgbaColour.White;

        Assert.AreEqual(nameof(ComboColour.Color), changedProperty);
    }
}
