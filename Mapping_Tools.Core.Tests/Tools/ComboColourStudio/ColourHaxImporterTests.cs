using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.ComboColours;
using Mapping_Tools.Core.BeatmapHelper.Contexts;
using Mapping_Tools.Core.BeatmapHelper.IO.Editor;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Mapping_Tools.Core.Tests.Tools.ComboColourStudio;

[TestClass]
public class ColourHaxImporterTests {
    [TestMethod]
    public void IsSubSequenceTest() {
        Assert.IsTrue(ColourHaxImporter.IsSubSequence(new []{1,2,3}, new []{1,2,3,4}));
        Assert.IsTrue(ColourHaxImporter.IsSubSequence(new []{1,2,3}, new []{1,2,3,4,6,5,2}));
        Assert.IsTrue(ColourHaxImporter.IsSubSequence(new int[]{}, new []{1,2,3,4}));
        Assert.IsFalse(ColourHaxImporter.IsSubSequence(new []{1,2,3}, new []{1,2,2,4}));
        Assert.IsFalse(ColourHaxImporter.IsSubSequence(new []{1,2,3}, new []{1,2}));
    }

    [TestMethod]
    public void ImportExportColourHaxTest() {
        var path = Path.Combine("Resources",
            "SAMString - Forget The Promise (DeviousPanda) [Elysium].osu");
        var beatmap = new BeatmapEditor(path).ReadFile();
        const int maxBurstLength = 1;

        var imported = new ColourHaxImporter {MaxBurstLength = maxBurstLength}.ImportColourHax(beatmap);

        Assert.AreEqual(new ComboColour(244, 73, 0), imported.ComboColours[0]);
        Assert.AreEqual(new ComboColour(0, 85, 170), imported.ComboColours[1]);
        Assert.AreEqual(new ComboColour(51, 36, 113), imported.ComboColours[2]);
        Assert.AreEqual(new ComboColour(0, 130, 117), imported.ComboColours[3]);

        // Create a copy with no combo colouring and export to it
        var beatmapEmpty = new BeatmapEditor(path).ReadFile();
        beatmapEmpty.ComboColoursList.Clear();
        foreach (var ho in beatmapEmpty.HitObjects) {
            ho.ComboSkip = 0;
        }
        beatmapEmpty.CalculateHitObjectComboStuff();

        ColourHaxExporter.ExportColourHax(imported, beatmapEmpty);
        beatmapEmpty.CalculateHitObjectComboStuff();

        // foreach (var colourPoint in imported.ColourPoints) {
        //     Console.WriteLine(colourPoint.Time);
        //     foreach (var i in colourPoint.ColourSequence) {
        //         Console.WriteLine(i);
        //     }
        // }

        // Check if it matches the original combo colouring
        for (int i = 0; i < beatmap.HitObjects.Count; i++) {
            var original = beatmap.HitObjects[i].GetContext<ComboContext>();
            var actual = beatmapEmpty.HitObjects[i].GetContext<ComboContext>();

            Assert.AreEqual(original.ColourIndex, actual.ColourIndex, Precision.DOUBLE_EPSILON,
                $"Colour index check failed with {beatmap.HitObjects[i]} and {beatmapEmpty.HitObjects[i]}");
            Assert.AreEqual(original.Colour, actual.Colour,
                $"Colour check failed with {beatmap.HitObjects[i]} and {beatmapEmpty.HitObjects[i]}");
        }
    }
}