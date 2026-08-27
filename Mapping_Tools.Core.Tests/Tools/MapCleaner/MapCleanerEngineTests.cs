using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
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

    private static Beatmap Load(string fileName)
    {
        return new Beatmap(
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", fileName)).ToList());
    }
}
