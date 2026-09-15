using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.Tools.HitsoundCopier;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.HitsoundCopier;

[TestClass]
public sealed class HitsoundCopierEngineTests
{
    [TestMethod]
    public void Apply_OverwriteMode_CopiesAllSourceObjectHitsounds()
    {
        // Arrange
        var source = LoadFixture();
        Beatmap target = new(source.GetLines());
        HitsoundCopierEngineOptions options = new()
        {
            CopyMode = HitsoundCopierCopyMode.OverwriteEverything,
        };

        // Act
        var result = HitsoundCopierEngine.Apply(
            target,
            source,
            options,
            @"C:\maps");

        // Assert
        result.MatchedHitsoundCount.Should().BeGreaterThan(0);
        target.HitObjects.Select(item => item.Hitsounds)
            .Should().BeEquivalentTo(source.HitObjects.Select(item => item.Hitsounds));
    }

    [TestMethod]
    public void Validate_WithUndefinedCopyMode_ThrowsArgumentException()
    {
        // Arrange
        HitsoundCopierEngineOptions options = new()
        {
            CopyMode = (HitsoundCopierCopyMode)2,
        };

        // Act
        Action act = () => HitsoundCopierEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Apply_StoryboardSamples_PreservesSourceTiming()
    {
        // Arrange
        var source = LoadStoryboardFixture();
        source.StoryboardSoundSamples.Clear();
        source.StoryboardSoundSamples.Add(new StoryboardSoundSample(100, StoryboardLayer.Foreground, "sample.wav", 80));
        Beatmap target = new(source.GetLines());
        HitsoundCopierEngineOptions options = new()
        {
            CopyHitsounds = false,
            CopyStoryboardedSamples = true,
            IgnoreHitsoundSatisfiedSamples = false,
        };

        // Act
        HitsoundCopierEngine.Apply(
            target,
            source,
            options,
            @"C:\maps");

        // Assert
        target.StoryboardSoundSamples.Should().ContainSingle();
        target.StoryboardSoundSamples[0].StartTime.Should().Be(100);
    }

    [TestMethod]
    public void Apply_MutingWithConfiguredSampleSet_SetsAllEligibleSliderEndsToConfiguredSampleSet()
    {
        // Arrange
        TimingPoint redline = new(0, 1000, 4, SampleSet.Normal, 0, 100, true, false, false);
        Beatmap target = new(
        [
            new HitObject("256,192,100,2,0,L|396:192,1,140,0|1,0:0|2:0,0:0:0:0:"),
            new HitObject("256,192,2100,2,0,L|396:192,1,140,0|1,0:0|3:0,0:0:0:0:"),
        ],
        [redline],
        redline,
        globalSv: 1.4);
        Beatmap source = new();
        HitsoundCopierEngineOptions options = new()
        {
            CopyHitsounds = false,
            CopyBodyHitsounds = false,
            MuteSliderends = true,
            BeatDivisors = [new RationalBeatDivisor(1)],
            MutedDivisors = [new RationalBeatDivisor(1)],
            MinLength = 0,
            MutedSampleSet = SampleSet.Drum,
        };

        // Act
        var result = HitsoundCopierEngine.Apply(target, source, options, @"C:\maps");

        // Assert
        result.MutedEdgeCount.Should().Be(2);
        target.HitObjects.Select(item => item.EdgeSampleSets[^1])
            .Should().Equal(SampleSet.Drum, SampleSet.Drum);
        target.HitObjects.Select(item => item.EdgeHitsounds[^1])
            .Should().Equal(0, 0);
        target.BeatmapTiming.TimingPoints
            .Where(point => point.Offset is 1100 or 3100)
            .Should().HaveCount(2)
            .And.OnlyContain(point => point.SampleSet == SampleSet.Drum && point.Volume == 5);
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
