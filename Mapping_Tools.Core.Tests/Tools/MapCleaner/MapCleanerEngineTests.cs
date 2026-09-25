using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.MapCleaner;
using Mapping_Tools.Core.Tools.MapCleaner.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.MapCleaner;

[TestClass]
public sealed class MapCleanerEngineTests
{
    [TestMethod]
    public void Clean_WithAcceptedFixture_ReproducesLegacySemanticCounts()
    {
        // Arrange
        var beatmap = Load("standard-feature-rich.osu");
        MapCleanerEngineOptions options = new()
        {
            VolumeSliders = true,
            SampleSetSliders = false,
            VolumeSpinners = true,
            ResnapObjects = true,
            ResnapBookmarks = true,
            AnalyzeSamples = false,
            BeatDivisors = [new RationalBeatDivisor(12), new RationalBeatDivisor(16)],
        };

        // Act
        var result = MapCleanerEngine.Clean(beatmap, options);

        // Assert
        result.TimingPointsRemoved.Should().Be(16);
        result.ObjectsResnapped.Should().Be(20);
        beatmap.BeatmapTiming.TimingPoints.Should().HaveCount(815);
        beatmap.HitObjects.Should().HaveCount(924);
        beatmap.GetBookmarks().Should().HaveCount(20);
        beatmap.GetLines().Should().Equal(
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", "expected-map-cleaner.osu")));
    }

    [TestMethod]
    public void Clean_WithRemoveHitsounds_ClearsCircleAndSliderEdgeHitsounds()
    {
        // Arrange
        Beatmap beatmap = CreateBeatmap(
            new HitObject("256,192,1000,1,2,0:0:0:0:"),
            new HitObject("256,192,1500,2,0,L|356:192,1,100") { EdgeHitsounds = [2, 8] });
        MapCleanerEngineOptions options = new()
        {
            RemoveHitsounds = true,
            ResnapObjects = false,
            ResnapBookmarks = false,
        };

        // Act
        MapCleanerEngine.Clean(beatmap, options);

        // Assert
        beatmap.HitObjects[0].Hitsounds.Should().Be(1);
        beatmap.HitObjects[1].EdgeHitsounds.Should().OnlyContain(value => value == 0);
        beatmap.HitObjects.Select(item => item.Time).Should().Equal(1000, 1500);
    }

    [TestMethod]
    public void Clean_WithWholeBeatResnapping_MovesOffGridObjectAndBookmark()
    {
        // Arrange
        Beatmap beatmap = CreateBeatmap(new HitObject("256,192,1020,1,0,0:0:0:0:"));
        beatmap.SetBookmarks([1020]);
        MapCleanerEngineOptions options = new()
        {
            ResnapObjects = true,
            ResnapBookmarks = true,
            BeatDivisors = [new RationalBeatDivisor(1)],
        };

        // Act
        var result = MapCleanerEngine.Clean(beatmap, options);

        // Assert
        result.ObjectsResnapped.Should().Be(1);
        beatmap.HitObjects[0].Time.Should().Be(1000);
        beatmap.GetBookmarks().Should().Equal(1000);
    }

    private static Beatmap CreateBeatmap(params HitObject[] objects)
    {
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        return new Beatmap(objects.ToList(), [redline], redline);
    }

    private static Beatmap Load(string fileName)
    {
        return new Beatmap(
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", fileName)).ToList());
    }
}
