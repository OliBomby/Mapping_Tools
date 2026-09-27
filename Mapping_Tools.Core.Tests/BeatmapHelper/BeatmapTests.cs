using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

[TestClass]
public class BeatmapTests
{
    [TestMethod]
    public void Encode_WithMixedSlider_UsesBeatmapFormatVersion()
    {
        // Arrange
        HitObject hitObject = BeatmapTestData.DecodeHitObject("64,96,1200,2,0,P|128:160|192:96|L|256:96,1,240");
        Beatmap beatmap = new() { Version = 14, HitObjects = [hitObject] };
        BeatmapEncoder encoder = new();

        // Act
        string legacyLine = encoder.Encode(beatmap)
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.StartsWith("64,96,1200,"));
        beatmap.Version = 128;
        string lazerLine = encoder.Encode(beatmap)
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.StartsWith("64,96,1200,"));

        // Assert
        legacyLine.Split(',')[5].Should().StartWith("B|");
        lazerLine.Split(',')[5].Should().Be("P|128:160|192:96|L|256:96");
        hitObject.ControlPoints[0].Type.Should().Be(PathType.PerfectCurve);
    }

    [DataTestMethod]
    [DataRow("EmptyTestMap.osu")]
    [DataRow("ComplicatedTestMap.osu")]
    [DataRow("Camellia - Body F10ating in the Zero Gravity Space (Orange_) [Nonsubmersible].osu")]
    [DataRow("THE ORAL CIGARETTES - GET BACK (Nikakis) [Sotarks_ Cataclysm].osu")]
    public void DecodeAndEncode_Fixture_PreservesNormalizedDocument(string filename)
    {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", filename);
        string[] expectedLines = File.ReadAllLines(path);

        // Act
        var beatmap = new BeatmapDecoder().Decode(File.ReadAllText(path));
        string actual = new BeatmapEncoder().Encode(beatmap);
        string[] actualLines = actual.TrimEnd('\r', '\n').Split("\r\n");

        // Assert
        actualLines.Should().Equal(expectedLines);
        actual.Replace("\r\n", "").Should().NotContain("\n");
    }

    [DataTestMethod]
    [DataRow("catch.osu", 2)]
    [DataRow("mania.osu", 3)]
    [DataRow("taiko.osu", 1)]
    public void Decode_AdditionalModeFixture_ParsesMode(string filename, int expectedMode)
    {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", filename);

        // Act
        var beatmap = new BeatmapDecoder().Decode(File.ReadAllText(path));

        // Assert
        beatmap.General["Mode"].IntValue.Should().Be(expectedMode);
        beatmap.HitObjects.Should().NotBeEmpty();
    }

    [TestMethod]
    public void Beatmap_WithoutFirstTimingPoint_DoesNotAddNullTimingPoint()
    {
        // Arrange
        List<HitObject> hitObjects = [];
        List<TimingPoint> timingPoints = [];

        // Act
        Beatmap beatmap = new(hitObjects, timingPoints);

        // Assert
        beatmap.BeatmapTiming.TimingPoints.Should().BeEmpty();
    }

    [TestMethod]
    public void QueryTimeCode_SelectsRequestedComboObjects()
    {
        // Arrange
        var beatmap = new Beatmap([
            BeatmapTestData.DecodeHitObject("64,96,1000,5,0,0:0:0:0:"),
            BeatmapTestData.DecodeHitObject("128,96,1100,1,0,0:0:0:0:"),
            BeatmapTestData.DecodeHitObject("192,96,1200,1,0,0:0:0:0:"),
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
        HitObject circle = BeatmapTestData.DecodeHitObject("64,96,1000,1,0,0:0:0:0:");
        HitObject hold = BeatmapTestData.DecodeHitObject("128,192,2000,128,0,3000:0:0:0:0:");
        HitObject unmarked = BeatmapTestData.DecodeHitObject("192,96,4000,1,0,0:0:0:0:");
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
        HitObject before = BeatmapTestData.DecodeHitObject("64,96,1000,1,0,0:0:0:0:");
        HitObject overlapping = BeatmapTestData.DecodeHitObject("128,192,2000,128,0,3000:0:0:0:0:");
        HitObject after = BeatmapTestData.DecodeHitObject("192,96,4000,1,0,0:0:0:0:");
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
        HitObject first = BeatmapTestData.DecodeHitObject("64,96,1000,1,0,0:0:0:0:");
        HitObject second = BeatmapTestData.DecodeHitObject("64,96,1050,1,0,0:0:0:0:");
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
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal,
            0, 100, true, false, false);
        HitObject originalObject = BeatmapTestData.DecodeHitObject("64,96,1000,1,0,0:0:0:0:");
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
