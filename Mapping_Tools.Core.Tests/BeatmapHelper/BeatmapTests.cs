using Mapping_Tools.Core.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

[TestClass]
public class BeatmapTests
{
    [DataTestMethod]
    [DataRow("EmptyTestMap.osu")]
    [DataRow("ComplicatedTestMap.osu")]
    [DataRow("Camellia - Body F10ating in the Zero Gravity Space (Orange_) [Nonsubmersible].osu")]
    [DataRow("THE ORAL CIGARETTES - GET BACK (Nikakis) [Sotarks_ Cataclysm].osu")]
    public void BeatmapDocument_ParsesAndRoundTripsFixture(string filename)
    {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", filename);
        string expected = File.ReadAllText(path);

        // Act
        var beatmap = new Beatmap([.. File.ReadAllLines(path)]);
        // Repository fixtures use LF line endings regardless of the host platform.
        string actual = string.Join("\r\n", beatmap.GetLines());

        // Assert
        actual.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow("catch.osu", 2)]
    [DataRow("mania.osu", 3)]
    [DataRow("taiko.osu", 1)]
    public void BeatmapDocument_ParsesAdditionalModeFixture(string filename, int expectedMode)
    {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", filename);

        // Act
        var beatmap = new Beatmap([.. File.ReadAllLines(path)]);

        // Assert
        beatmap.General["Mode"].IntValue.Should().Be(expectedMode);
        beatmap.HitObjects.Should().NotBeEmpty();
    }

    [TestMethod]
    public void QueryTimeCode_SelectsRequestedComboObjects()
    {
        // Arrange
        var beatmap = new Beatmap([
            new HitObject("64,96,1000,5,0,0:0:0:0:"),
            new HitObject("128,96,1100,1,0,0:0:0:0:"),
            new HitObject("192,96,1200,1,0,0:0:0:0:"),
        ], [], globalSv: 1.4);

        // Act
        var matches = beatmap.QueryTimeCode("00:01:000 (1,2) - ").ToList();

        // Assert
        matches.Should().Equal(beatmap.HitObjects[0], beatmap.HitObjects[1]);
    }

    [TestMethod]
    public void GetBookmarkedObjects_IncludesObjectsAtBothLeniencyBoundaries()
    {
        // Arrange
        HitObject circle = new("64,96,1000,1,0,0:0:0:0:");
        HitObject hold = new("128,192,2000,128,0,3000:0:0:0:0:");
        HitObject unmarked = new("192,96,4000,1,0,0:0:0:0:");
        Beatmap beatmap = new([circle, hold, unmarked], [], globalSv: 1.4);
        beatmap.Bookmarks = [995.4, 3004.6];

        // Act
        List<HitObject> bookmarked = beatmap.GetBookmarkedObjects();

        // Assert
        beatmap.GetBookmarks().Should().Equal(995, 3005);
        bookmarked.Should().Equal(circle, hold);
    }

    [TestMethod]
    public void GetHitObjectsWithRangeInRange_IncludesObjectsThatOverlapEitherBoundary()
    {
        // Arrange
        HitObject before = new("64,96,1000,1,0,0:0:0:0:");
        HitObject overlapping = new("128,192,2000,128,0,3000:0:0:0:0:");
        HitObject after = new("192,96,4000,1,0,0:0:0:0:");
        Beatmap beatmap = new([before, overlapping, after], [], globalSv: 1.4);

        // Act
        List<HitObject> matches = beatmap.GetHitObjectsWithRangeInRange(2500, 4000);

        // Assert
        matches.Should().Equal(overlapping, after);
    }

    [TestMethod]
    public void UpdateStacking_OnCoincidentCircles_OffsetsEarlierCircleByOneStack()
    {
        // Arrange
        HitObject first = new("64,96,1000,1,0,0:0:0:0:");
        HitObject second = new("64,96,1050,1,0,0:0:0:0:");
        Beatmap beatmap = new([first, second], [], globalSv: 1.4);

        // Act
        beatmap.UpdateStacking();

        // Assert
        first.StackCount.Should().Be(1);
        second.StackCount.Should().Be(0);
        first.StackedPos.Should().NotBe(first.Pos);
        second.StackedPos.Should().Be(second.Pos);
    }

    [TestMethod]
    public void DeepCopy_MutatingCopiedHitObjectsAndTimingLeavesOriginalUnchanged()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, Mapping_Tools.Core.BeatmapHelper.Enums.SampleSet.Normal,
            0, 100, true, false, false);
        HitObject originalObject = new("64,96,1000,1,0,0:0:0:0:");
        Beatmap original = new([originalObject], [redline], redline);

        // Act
        Beatmap copy = original.DeepCopy();
        copy.HitObjects[0].Time = 2000;
        copy.BeatmapTiming.TimingPoints[0].Offset = 500;

        // Assert
        original.HitObjects[0].Time.Should().Be(1000);
        original.BeatmapTiming.TimingPoints[0].Offset.Should().Be(0);
        copy.HitObjects[0].Time.Should().Be(2000);
        copy.BeatmapTiming.TimingPoints[0].Offset.Should().Be(500);
    }
}
