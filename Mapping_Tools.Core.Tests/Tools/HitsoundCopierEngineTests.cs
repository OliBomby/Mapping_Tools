using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Events;
using Mapping_Tools.Core.Tools.HitsoundCopier;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class HitsoundCopierEngineTests
{
    [TestMethod]
    public void Apply_OverwriteModeWithSubset_ClearsUnmatchedTargetHitsoundsAndCopiesEdges()
    {
        // Arrange
        var source = LoadFixture();
        Beatmap target = new(source.GetLines());
        var selected = source.HitObjects[0];
        int selectedHitsounds = selected.Hitsounds;
        HitsoundCopierOptions options = new() { CopyMode = 0 };

        // Act
        var result = HitsoundCopierEngine.Apply(
            target,
            source,
            [selected],
            options,
            @"C:\maps");

        // Assert
        result.MatchedHitsoundCount.Should().BeGreaterThan(0);
        target.HitObjects[0].Hitsounds.Should().Be(selectedHitsounds);
        target.HitObjects.Skip(1).Should().OnlyContain(item => item.Hitsounds == 0);
    }

    [TestMethod]
    public void Apply_ShiftedStoryboardSamples_UsesTheConfiguredTimingOffset()
    {
        // Arrange
        var source = LoadStoryboardFixture();
        source.StoryboardSoundSamples.Clear();
        source.StoryboardSoundSamples.Add(new StoryboardSoundSample(100, StoryboardLayer.Foreground, "sample.wav", 80));
        Beatmap target = new(source.GetLines());
        double original = source.StoryboardSoundSamples[0].StartTime;
        HitsoundCopierOptions options = new()
        {
            CopyHitsounds = false,
            CopyStoryboardedSamples = true,
            IgnoreHitsoundSatisfiedSamples = false,
            TimingOffset = 12,
        };

        // Act
        HitsoundCopierEngine.Apply(
            target,
            source,
            [],
            options,
            @"C:\maps");

        // Assert
        target.StoryboardSoundSamples.Should().ContainSingle();
        target.StoryboardSoundSamples[0].StartTime.Should().Be(original + 12);
    }

    private static Beatmap LoadFixture()
    {
        return new Beatmap(
            File.ReadAllLines(Path.Combine(
                AppContext.BaseDirectory,
                "Resources",
                "ComplicatedTestMap.osu")).ToList());
    }

    private static Beatmap LoadStoryboardFixture()
    {
        return new Beatmap(
            File.ReadAllLines(Path.Combine(
                AppContext.BaseDirectory,
                "Resources",
                "EmptyTestMap.osu")).ToList());
    }
}
