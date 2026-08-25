using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.PropertyTransformer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class PropertyTransformerEngineTests
{
    [TestMethod]
    public void Apply_WithBookmarkOffset_TransformsAllBookmarksAndReportsCompletion()
    {
        // Arrange
        var beatmap = Load("standard-feature-rich.osu");
        double[] originalBookmarks = beatmap.GetBookmarks().ToArray();
        PropertyTransformerEngineOptions options = new()
        {
            BookmarkTimeOffset = 5,
        };
        RecordingProgress progress = new();

        // Act
        PropertyTransformerEngine.Apply(beatmap, options, progress);

        // Assert
        beatmap.GetBookmarks().Should().Equal(originalBookmarks.Select(bookmark => bookmark + 5));
        progress.Values.Should().ContainInOrder(0.2, 0.25, 0.3, 0.4, 0.5, 0.55, 0.6, 0.7, 0.8, 0.9, 1);
    }

    [TestMethod]
    public void Apply_WithFiltersAndClipping_UsesLegacyBoundsAndInclusiveTimeRange()
    {
        // Arrange
        var beatmap = Load("standard-feature-rich.osu");
        double[] originalBookmarks = beatmap.GetBookmarks().ToArray();
        PropertyTransformerEngineOptions options = new()
        {
            BookmarkTimeMultiplier = 2,
            BookmarkTimeOffset = 100,
            EnableFilters = true,
            MatchFilter = [originalBookmarks[0]],
            MinTimeFilter = originalBookmarks[0],
            MaxTimeFilter = originalBookmarks[0],
            ClipProperties = true,
            TimingpointBpmMultiplier = 1000,
        };

        // Act
        PropertyTransformerEngine.Apply(beatmap, options);

        // Assert
        beatmap.GetBookmarks()[0].Should().Be(originalBookmarks[0] * 2 + 100);
        beatmap.GetBookmarks().Skip(1).Should().Equal(originalBookmarks.Skip(1));
        beatmap.BeatmapTiming.TimingPoints
            .Where(point => point.Uninherited)
            .Select(point => point.GetBpm())
            .Should().OnlyContain(bpm => bpm <= 10000);
    }

    private static Beatmap Load(string fileName)
    {
        return new Beatmap(
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", fileName)).ToList());
    }

    private sealed class RecordingProgress : IProgress<double>
    {
        public List<double> Values { get; } = [];

        public void Report(double value)
        {
            Values.Add(value);
        }
    }
}
