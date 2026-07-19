using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class BeatmapTests {
    [DataTestMethod]
    [DataRow("EmptyTestMap.osu")]
    [DataRow("ComplicatedTestMap.osu")]
    public void BeatmapDocument_ParsesAndRoundTripsFixture(string filename) {
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", filename);
        string expected = File.ReadAllText(path);

        var beatmap = new Beatmap(File.ReadAllLines(path).ToList());
        string actual = string.Join(Environment.NewLine, beatmap.GetLines());

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void QueryTimeCode_SelectsRequestedComboObjects() {
        var beatmap = new Beatmap(new List<HitObject> {
            new("64,96,1000,5,0,0:0:0:0:"),
            new("128,96,1100,1,0,0:0:0:0:"),
            new("192,96,1200,1,0,0:0:0:0:")
        }, new List<TimingPoint>(), globalSv: 1.4);

        var matches = beatmap.QueryTimeCode("00:01:000 (1,2) - ").ToList();

        CollectionAssert.AreEqual(new[] { beatmap.HitObjects[0], beatmap.HitObjects[1] }, matches);
    }
}
