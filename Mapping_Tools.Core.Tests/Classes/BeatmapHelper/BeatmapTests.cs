using Mapping_Tools.Core.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class BeatmapTests {
    [DataTestMethod]
    [DataRow("EmptyTestMap.osu")]
    [DataRow("ComplicatedTestMap.osu")]
    [DataRow("Camellia - Body F10ating in the Zero Gravity Space (Orange_) [Nonsubmersible].osu")]
    [DataRow("THE ORAL CIGARETTES - GET BACK (Nikakis) [Sotarks_ Cataclysm].osu")]
    public void BeatmapDocument_ParsesAndRoundTripsFixture(string filename) {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", filename);
        string expected = File.ReadAllText(path);

        // Act
        var beatmap = new Beatmap(File.ReadAllLines(path).ToList());
        // Repository fixtures use LF line endings regardless of the host platform.
        string actual = string.Join("\r\n", beatmap.GetLines());

        // Assert
        actual.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow("catch.osu", 2)]
    [DataRow("mania.osu", 3)]
    [DataRow("taiko.osu", 1)]
    public void BeatmapDocument_ParsesAdditionalModeFixture(string filename, int expectedMode) {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", filename);

        // Act
        var beatmap = new Beatmap(File.ReadAllLines(path).ToList());

        // Assert
        beatmap.General["Mode"].IntValue.Should().Be(expectedMode);
        beatmap.HitObjects.Should().NotBeEmpty();
    }

    [TestMethod]
    public void QueryTimeCode_SelectsRequestedComboObjects() {
        // Arrange
        var beatmap = new Beatmap(new List<HitObject> {
            new("64,96,1000,5,0,0:0:0:0:"),
            new("128,96,1100,1,0,0:0:0:0:"),
            new("192,96,1200,1,0,0:0:0:0:")
        }, new List<TimingPoint>(), globalSv: 1.4);

        // Act
        var matches = beatmap.QueryTimeCode("00:01:000 (1,2) - ").ToList();

        // Assert
        matches.Should().Equal(new[] { beatmap.HitObjects[0], beatmap.HitObjects[1] });
    }
}
