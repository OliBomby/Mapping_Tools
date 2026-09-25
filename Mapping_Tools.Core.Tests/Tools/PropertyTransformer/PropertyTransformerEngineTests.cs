using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.Tools.PropertyTransformer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.PropertyTransformer;

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

    [TestMethod]
    public void Validate_WithNonFiniteMultiplier_ThrowsValidationException()
    {
        // Arrange
        PropertyTransformerEngineOptions options = new()
        {
            HitObjectTimeMultiplier = double.NaN,
        };

        // Act
        var act = () => PropertyTransformerEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*finite*");
    }

    [TestMethod]
    public void Apply_WithHitObjectTimeAndVolume_TransformsSpinnerEndsAndClipsVolume()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        HitObject circle = new("256,192,1000,1,0,0:0:0:30:");
        HitObject spinner = new("256,192,2000,8,0,3000,0:0:0:90:");
        Beatmap beatmap = new([circle, spinner], [redline], redline);
        PropertyTransformerEngineOptions options = new()
        {
            HitObjectTimeMultiplier = 2,
            HitObjectTimeOffset = 100,
            HitObjectVolumeOffset = 30,
            ClipProperties = true,
        };

        // Act
        PropertyTransformerEngine.Apply(beatmap, options);

        // Assert
        circle.Time.Should().Be(2100);
        spinner.Time.Should().Be(4100);
        spinner.EndTime.Should().Be(6100);
        circle.SampleVolume.Should().Be(60);
        spinner.SampleVolume.Should().Be(100);
    }

    [TestMethod]
    public void Apply_WithTimingPointIndexAndVolume_ClipsEachToItsOwnBounds()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 2, 80, true, false, false);
        Beatmap beatmap = new([], [redline], redline);
        PropertyTransformerEngineOptions options = new()
        {
            TimingpointIndexOffset = -10,
            TimingpointVolumeOffset = -100,
            ClipProperties = true,
        };

        // Act
        PropertyTransformerEngine.Apply(beatmap, options);

        // Assert
        redline.SampleIndex.Should().Be(0);
        redline.Volume.Should().Be(5);
        redline.GetBpm().Should().Be(120);
    }

    [TestMethod]
    public void Apply_WithSampleAndPreviewOffsets_TransformsTheirIndependentTimes()
    {
        // Arrange
        Beatmap beatmap = Load("standard-feature-rich.osu");
        beatmap.StoryboardSoundSamples.Add(new StoryboardSoundSample(1000, StoryboardLayer.Foreground, "sample.wav", 40));
        var sample = beatmap.StoryboardSoundSamples.First();
        double sampleTime = sample.StartTime;
        double sampleVolume = sample.Volume;
        double previewTime = beatmap.General["PreviewTime"].DoubleValue;
        PropertyTransformerEngineOptions options = new()
        {
            SbSampleTimeOffset = 100,
            SbSampleVolumeOffset = 10,
            PreviewTimeOffset = 250,
        };

        // Act
        PropertyTransformerEngine.Apply(beatmap, options);

        // Assert
        sample.StartTime.Should().Be(sampleTime + 100);
        sample.Volume.Should().Be(sampleVolume + 10);
        beatmap.General["PreviewTime"].DoubleValue.Should().Be(previewTime + 250);
    }

    [TestMethod]
    public void Apply_WithBreakAndVideoOffsets_TransformsBothBreakEndpointsAndVideoStart()
    {
        // Arrange
        Beatmap beatmap = Load("standard-feature-rich.osu");
        Break breakPeriod = new("2,1000,2000");
        Video video = new() { EventType = "Video", Filename = "background.mp4", StartTime = 500 };
        beatmap.BreakPeriods.Add(breakPeriod);
        beatmap.BackgroundAndVideoEvents.Add(video);
        PropertyTransformerEngineOptions options = new()
        {
            BreakTimeMultiplier = 2,
            BreakTimeOffset = 100,
            VideoTimeOffset = -200,
        };

        // Act
        PropertyTransformerEngine.Apply(beatmap, options);

        // Assert
        breakPeriod.StartTime.Should().Be(2100);
        breakPeriod.EndTime.Should().Be(4100);
        video.StartTime.Should().Be(300);
    }

    [TestMethod]
    public void Apply_WithBpmAndSliderVelocityMultipliers_UpdatesRedlineAndGreenline()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        TimingPoint greenline = new(1000, -100, 4, SampleSet.Normal, 0, 100, false, false, false);
        Beatmap beatmap = new([], [redline, greenline], redline);
        PropertyTransformerEngineOptions options = new()
        {
            TimingpointBpmMultiplier = 2,
            TimingpointSvMultiplier = 2,
        };

        // Act
        PropertyTransformerEngine.Apply(beatmap, options);

        // Assert
        redline.GetBpm().Should().Be(240);
        beatmap.BeatmapTiming.GetSvMultiplierAtTime(1000).Should().Be(2);
    }

    [TestMethod]
    public void Apply_StoryboardWithLoop_OffsetsAbsoluteCommandsButNotRelativeChildren()
    {
        // Arrange
        StoryBoard storyboard = new();
        Sprite sprite = new() { FilePath = "sprite.png", Layer = StoryboardLayer.Foreground };
        OtherCommand command = new() { StartTime = 100, EndTime = 200, ParentEvent = sprite, Params = [] };
        StandardLoop loop = new() { StartTime = 1000, LoopCount = 2, ParentEvent = sprite };
        OtherCommand loopChild = new() { StartTime = 20, EndTime = 40, ParentEvent = loop, Params = [] };
        sprite.ChildEvents.Add(command);
        sprite.ChildEvents.Add(loop);
        loop.ChildEvents.Add(loopChild);
        storyboard.StoryboardLayerForeground.Add(sprite);
        PropertyTransformerEngineOptions options = new()
        {
            SbEventTimeMultiplier = 2,
            SbEventTimeOffset = 100,
        };

        // Act
        PropertyTransformerEngine.Apply(storyboard, options);

        // Assert
        command.StartTime.Should().Be(300);
        command.EndTime.Should().Be(500);
        loop.StartTime.Should().Be(2100);
        loopChild.StartTime.Should().Be(40);
        loopChild.EndTime.Should().Be(80);
    }

    [TestMethod]
    public void Apply_StoryboardSamplesAndVideo_TransformsTheirTimesAndVolume()
    {
        // Arrange
        StoryBoard storyboard = new();
        StoryboardSoundSample sample = new(1000, StoryboardLayer.Foreground, "sample.wav", 40);
        Video video = new() { EventType = "Video", Filename = "background.mp4", StartTime = 500 };
        storyboard.StoryboardSoundSamples.Add(sample);
        storyboard.BackgroundAndVideoEvents.Add(video);
        PropertyTransformerEngineOptions options = new()
        {
            SbSampleTimeOffset = 100,
            SbSampleVolumeMultiplier = 2,
            VideoTimeOffset = -200,
        };

        // Act
        PropertyTransformerEngine.Apply(storyboard, options);

        // Assert
        sample.StartTime.Should().Be(1100);
        sample.Volume.Should().Be(80);
        video.StartTime.Should().Be(300);
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
