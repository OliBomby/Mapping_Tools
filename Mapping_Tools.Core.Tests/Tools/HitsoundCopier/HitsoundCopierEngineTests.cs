using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;
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
        var act = () => HitsoundCopierEngine.Validate(options);

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
            redline);
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
            .And.OnlyContain(point => point.SampleSet == SampleSet.Drum && Precision.AlmostEquals(point.Volume, 5));
    }

    [TestMethod]
    public void Apply_CopyToSliderSlides_DoesNotAddFiveMillisecondRevertGreenline()
    {
        // Arrange
        TimingPoint sourceTiming = new(0, 1000, 4, SampleSet.Normal, 0, 100, true, false, false);
        HitObject sourceObject = new(500, 1, SampleSet.Normal, SampleSet.None)
        {
            CustomIndex = 1,
        };
        Beatmap source = new([sourceObject], [sourceTiming], sourceTiming);
        var targetTiming = sourceTiming.Copy();
        Beatmap target = new(
            [
                new HitObject("256,192,0,2,0,L|396:192,1,140,0|0:0,0:0:0:0:"),
            ],
            [targetTiming],
            targetTiming);
        HitsoundCopierEngineOptions options = new()
        {
            CopyMode = HitsoundCopierCopyMode.OverwriteOnlyDefined,
            CopyToSliderSlides = true,
            CopyBodyHitsounds = false,
        };
        SampleSchema assignmentSchema = new();
        assignmentSchema.Add("normal-sliderslide100", []);

        // Act
        var result = HitsoundCopierEngine.Apply(
            target,
            source,
            options,
            @"C:\maps",
            sourceMapDirectory: @"C:\source",
            assignSample: _ => new HitsoundSampleAssignment(100, SampleSet.Normal, assignmentSchema));

        // Assert
        result.GeneratedSampleCount.Should().Be(1);
        target.BeatmapTiming.Greenlines.Should().Contain(point => Precision.AlmostEquals(point.Offset, 500));
        target.BeatmapTiming.Greenlines.Should().NotContain(point => Precision.AlmostEquals(point.Offset, 505));
        target.BeatmapTiming.GetGreenlineAtTime(500).SampleIndex.Should().Be(100);
    }

    [TestMethod]
    public void Apply_WithHitObjectAtLeniencyBoundary_CopiesOnlyWhenWithinTolerance()
    {
        // Arrange
        Beatmap source = CreateCircleBeatmap(1000, 2);
        Beatmap targetOutside = CreateCircleBeatmap(1005, 8);
        Beatmap targetInside = CreateCircleBeatmap(1005, 8);
        HitsoundCopierEngineOptions options = new()
        {
            CopyMode = HitsoundCopierCopyMode.OverwriteOnlyDefined,
            CopyBodyHitsounds = false,
            TemporalLeniency = 4,
        };

        // Act
        var outside = HitsoundCopierEngine.Apply(targetOutside, source, options, @"C:\maps");
        options.TemporalLeniency = 5;
        var inside = HitsoundCopierEngine.Apply(targetInside, source, options, @"C:\maps");

        // Assert
        outside.MatchedHitsoundCount.Should().Be(0);
        targetOutside.HitObjects[0].Hitsounds.Should().Be(8);
        inside.MatchedHitsoundCount.Should().Be(1);
        targetInside.HitObjects[0].Hitsounds.Should().Be(2);
    }

    [TestMethod]
    public void Apply_WithStoryboardSampleAtHitObjectTime_SkipsSampleWhenConfigured()
    {
        // Arrange
        Beatmap source = LoadStoryboardFixture();
        source.StoryboardSoundSamples.Clear();
        source.StoryboardSoundSamples.Add(new StoryboardSoundSample(1000, StoryboardLayer.Foreground, "sample.wav", 80));
        Beatmap target = CreateCircleBeatmap(1000, 2);
        HitsoundCopierEngineOptions options = new()
        {
            CopyHitsounds = false,
            CopyBodyHitsounds = false,
            CopyStoryboardedSamples = true,
            IgnoreHitsoundSatisfiedSamples = false,
            IgnoreWheneverHitsound = true,
        };

        // Act
        HitsoundCopierEngine.Apply(target, source, options, @"C:\maps");

        // Assert
        target.StoryboardSoundSamples.Should().BeEmpty();
    }

    [TestMethod]
    public void Apply_WithRepeatedStoryboardSample_AddsOnlyOneCopy()
    {
        // Arrange
        Beatmap source = LoadStoryboardFixture();
        source.StoryboardSoundSamples.Clear();
        source.StoryboardSoundSamples.Add(new StoryboardSoundSample(100, StoryboardLayer.Foreground, "sample.wav", 80));
        source.StoryboardSoundSamples.Add(new StoryboardSoundSample(100, StoryboardLayer.Foreground, "sample.wav", 80));
        Beatmap target = LoadStoryboardFixture();
        target.StoryboardSoundSamples.Clear();
        HitsoundCopierEngineOptions options = new()
        {
            CopyHitsounds = false,
            CopyBodyHitsounds = false,
            CopyStoryboardedSamples = true,
            IgnoreHitsoundSatisfiedSamples = false,
        };

        // Act
        HitsoundCopierEngine.Apply(target, source, options, @"C:\maps");

        // Assert
        target.StoryboardSoundSamples.Should().ContainSingle()
            .Which.StartTime.Should().Be(100);
    }

    [DataTestMethod]
    [DataRow(-1d)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    public void Validate_WithInvalidTemporalLeniency_Throws(double leniency)
    {
        // Arrange
        HitsoundCopierEngineOptions options = new() { TemporalLeniency = leniency };

        // Act
        var act = () => HitsoundCopierEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Apply_OverwriteVolumes_PreservesFivePercentMuteOnlyWhenConfigured()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        TimingPoint mute = new(500, -100, 4, SampleSet.Normal, 0, 5, false, false, false);
        Beatmap source = CreateCircleBeatmap(1000, 2);
        Beatmap preserved = new([new HitObject("256,192,1000,1,0,0:0:0:0:")],
            [redline.Copy(), mute.Copy()], redline.Copy());
        Beatmap overwritten = new([new HitObject("256,192,1000,1,0,0:0:0:0:")],
            [redline.Copy(), mute.Copy()], redline.Copy());
        HitsoundCopierEngineOptions options = new()
        {
            CopyHitsounds = false,
            CopyBodyHitsounds = false,
            CopyVolumes = true,
            AlwaysPreserve5Volume = true,
        };

        // Act
        HitsoundCopierEngine.Apply(preserved, source, options, @"C:\maps");
        options.AlwaysPreserve5Volume = false;
        HitsoundCopierEngine.Apply(overwritten, source, options, @"C:\maps");

        // Assert
        preserved.BeatmapTiming.GetTimingPointAtTime(1000).Volume.Should().Be(5);
        overwritten.BeatmapTiming.GetTimingPointAtTime(1000).Volume.Should().Be(100);
    }

    private static Beatmap CreateCircleBeatmap(int time, int hitsound)
    {
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        return new Beatmap(
            [new HitObject($"256,192,{time},1,{hitsound},0:0:0:0:")],
            [redline],
            redline);
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
