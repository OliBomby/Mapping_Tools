using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.TimingHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.TimingHelper;

[TestClass]
public sealed class TimingHelperEngineTests
{
    [TestMethod]
    public void Apply_WithUnevenObjectMarkers_AddsRedlineAtPreviousMarker()
    {
        // Arrange
        var beatmap = CreateBeatmap(500, 500, 1300);
        TimingHelperEngineOptions options = new()
        {
            Bookmarks = false,
            Greenlines = false,
            BeatDivisors = [new RationalBeatDivisor(1, 4)],
        };

        // Act
        int redlinesAdded = TimingHelperEngine.Apply(beatmap, options);

        // Assert
        redlinesAdded.Should().Be(1);
        beatmap.BeatmapTiming.Redlines.Select(point => point.Offset)
            .Should().Equal(0, 500);
        beatmap.HitObjects.Select(hitObject => hitObject.Time)
            .Should().Equal(500, 1300);
    }

    [TestMethod]
    public void Apply_WithRedlinesDisabled_RemovesAllRedlinesExceptTheFirst()
    {
        // Arrange
        var beatmap = CreateBeatmap(500, 500);
        beatmap.BeatmapTiming.Add(CreateTimingPoint(1000, 500));
        TimingHelperEngineOptions options = new()
        {
            Objects = false,
            Bookmarks = false,
            Greenlines = false,
            Redlines = false,
            BeatDivisors = [new RationalBeatDivisor(1, 4)],
        };

        // Act
        TimingHelperEngine.Apply(beatmap, options);

        // Assert
        beatmap.BeatmapTiming.Redlines.Should().ContainSingle();
        beatmap.BeatmapTiming.Redlines[0].Offset.Should().Be(0);
    }

    [TestMethod]
    public void Apply_WithNegativeLeniency_ThrowsBeforeMutatingTiming()
    {
        // Arrange
        var beatmap = CreateBeatmap(500, 500);
        TimingHelperEngineOptions options = new()
        {
            Leniency = -1,
            BeatDivisors = [new RationalBeatDivisor(1, 4)],
        };

        // Act
        Action act = () => TimingHelperEngine.Apply(beatmap, options);

        // Assert
        act.Should().Throw<ArgumentException>();
        beatmap.BeatmapTiming.Redlines.Should().ContainSingle();
        beatmap.BeatmapTiming.Redlines[0].MpB.Should().Be(500);
    }

    private static Beatmap CreateBeatmap(double millisecondsPerBeat, params double[] objectTimes)
    {
        var redline = CreateTimingPoint(0, millisecondsPerBeat);
        return new Beatmap(
            objectTimes
                .Select(time => new HitObject(time, 0, SampleSet.None, SampleSet.None))
                .ToList(),
            [redline],
            redline);
    }

    private static TimingPoint CreateTimingPoint(double offset, double millisecondsPerBeat)
    {
        return new TimingPoint(
            offset,
            millisecondsPerBeat,
            4,
            SampleSet.Normal,
            0,
            100,
            true,
            false,
            false);
    }
}
