using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.HitsoundStuff;

[TestClass]
public class HitsoundDomainTests {
    [TestMethod]
    public void HitsoundFilename_ParsesStandardSampleName() {
        const string filename = "drum-hitclap12";

        Assert.AreEqual(SampleSet.Drum, HitsoundFilename.GetSampleSet(filename));
        Assert.AreEqual(Hitsound.Clap, HitsoundFilename.GetHitsound(filename));
        Assert.AreEqual(12, HitsoundFilename.GetIndex(filename));
    }

    [TestMethod]
    public void SampleGeneratingArgs_CopyPreservesGenerationSettings() {
        var source = new SampleGeneratingArgs(
            "samples/piano.sf2",
            volume: 0.75,
            panning: -0.2,
            pitchShift: 0.1,
            bank: 2,
            patch: 3,
            instrument: 4,
            key: 60,
            length: 500);

        SampleGeneratingArgs copy = source.Copy();

        Assert.AreEqual(source, copy);
        Assert.AreNotSame(source, copy);
        Assert.IsTrue(copy.UsesSoundFont);
        StringAssert.Contains(copy.GetFilename(), "piano");
    }

    [TestMethod]
    public void HitsoundZone_DistanceHonoursWildcardAxesAndCopyIsIndependent() {
        var zone = new HitsoundZone(
            false, "centre line", "normal-hitnormal.wav",
            xPos: -1, yPos: 100,
            Hitsound.Normal, SampleSet.Normal, SampleSet.None, 1);

        Assert.AreEqual(20, zone.Distance(new Vector2(400, 80)), 0.0001);

        HitsoundZone copy = zone.Copy();
        copy.YPos = 120;

        Assert.AreEqual(100, zone.YPos);
        Assert.AreEqual(120, copy.YPos);
    }

    [TestMethod]
    public void LayerImportArgs_ExposesFrontendNeutralVisibilityAndReloadRules() {
        var stack = new LayerImportArgs(ImportType.Stack) {
            Path = "map.osu",
            X = -1,
            Y = 192
        };
        var matchingStack = new LayerImportArgs(ImportType.Stack) {
            Path = "map.osu",
            X = 256,
            Y = 192
        };

        Assert.IsTrue(stack.CoordinateVisibility);
        Assert.IsFalse(stack.KeysoundVisibility);
        Assert.IsTrue(stack.ReloadCompatible(matchingStack));
    }

    [TestMethod]
    public void HitsoundLayer_RemoveDuplicatesUsesDomainPrecision() {
        var layer = new HitsoundLayer {
            Times = new List<double> { 1000, 1000, 1250, 1250 }
        };

        layer.RemoveDuplicates();

        CollectionAssert.AreEqual(new List<double> { 1000, 1250 }, layer.Times);
    }

    [TestMethod]
    public void SampleSchema_RoundTripsCustomIndexAssignments() {
        var sample = new SampleGeneratingArgs("kick.wav");
        var schema = new SampleSchema {
            ["normal-hitnormal3"] = new List<SampleGeneratingArgs> { sample }
        };

        List<CustomIndex> indices = schema.GetCustomIndices();
        var restored = new SampleSchema(indices);

        Assert.AreEqual(1, indices.Count);
        Assert.AreEqual(3, indices[0].Index);
        Assert.IsTrue(indices[0].Samples["normal-hitnormal"].Contains(sample));
        Assert.IsTrue(restored.ContainsKey("normal-hitnormal3"));
    }

    [TestMethod]
    public void CustomIndex_CleanInvalidsUsesCallerValidationPolicy() {
        var valid = new SampleGeneratingArgs("valid.wav");
        var invalid = new SampleGeneratingArgs("invalid.wav");
        var customIndex = new CustomIndex(2);
        customIndex.Samples["normal-hitnormal"].Add(valid);
        customIndex.Samples["normal-hitnormal"].Add(invalid);

        customIndex.CleanInvalids(sample => sample.Path == "valid.wav");

        CollectionAssert.AreEquivalent(
            new[] { valid },
            customIndex.Samples["normal-hitnormal"].ToArray());
    }

    [TestMethod]
    public void HitsoundEvent_EncodesWhistleFinishAndClapBits() {
        var hitsound = new HitsoundEvent(
            1000, 1, SampleSet.Normal, SampleSet.Drum, 2,
            whistle: true, finish: false, clap: true);

        Assert.AreEqual(10, hitsound.GetHitsounds());
    }
}
