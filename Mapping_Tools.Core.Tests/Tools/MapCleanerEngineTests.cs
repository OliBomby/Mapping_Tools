using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Tools.MapCleaner;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class MapCleanerEngineTests
{
    [TestMethod]
    public void Clean_WithAcceptedFixture_ReproducesLegacySemanticCounts()
    {
        // Arrange
        Beatmap beatmap = Load("standard-feature-rich.osu");
        MapCleanerOptions options = new()
        {
            VolumeSliders = true,
            SampleSetSliders = false,
            VolumeSpinners = true,
            ResnapObjects = true,
            ResnapBookmarks = true,
            AnalyzeSamples = false,
            BeatDivisors = [new RationalBeatDivisor(12), new RationalBeatDivisor(16)]
        };

        // Act
        MapCleanerResult result = MapCleanerEngine.Clean(beatmap, options);

        // Assert
        result.TimingPointsRemoved.Should().Be(16);
        result.ObjectsResnapped.Should().Be(20);
        beatmap.BeatmapTiming.TimingPoints.Should().HaveCount(815);
        beatmap.HitObjects.Should().HaveCount(924);
        beatmap.GetBookmarks().Should().HaveCount(20);
        beatmap.GetLines().Should().Equal(
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", "expected-map-cleaner.osu")));
    }

    private static Beatmap Load(string fileName) => new(
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", fileName)).ToList());
}
