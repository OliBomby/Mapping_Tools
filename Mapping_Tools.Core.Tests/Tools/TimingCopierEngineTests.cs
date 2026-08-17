using FluentAssertions;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.TimingCopier;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class TimingCopierEngineTests
{
    [TestMethod]
    public void Apply_PreserveBeatSpacing_MovesObjectsAndBookmarksBySourceTempo()
    {
        // Arrange
        Beatmap source = CreateBeatmap(500);
        Beatmap target = CreateBeatmap(1000, 1000);
        target.SetBookmarks([1500]);
        TimingCopierOptions options = new()
        {
            ResnapMode = TimingCopierResnapModes.PreserveBeatSpacing,
            BeatDivisors = [new RationalBeatDivisor(1, 4)]
        };

        // Act
        TimingCopierEngine.Apply(target, source, options);

        // Assert
        target.HitObjects.Select(hitObject => hitObject.Time).Should().Equal(500);
        target.GetBookmarks().Should().Equal(750);
        target.BeatmapTiming.Redlines.Select(point => point.MpB).Should().Equal(500);
    }

    [TestMethod]
    public void Apply_ResnapMode_ResnapsHitObjectsWithoutMovingBookmarks()
    {
        // Arrange
        Beatmap source = CreateBeatmap(500);
        Beatmap target = CreateBeatmap(1000, 700);
        target.SetBookmarks([700]);
        TimingCopierOptions options = new()
        {
            ResnapMode = TimingCopierResnapModes.Resnap,
            BeatDivisors = [new RationalBeatDivisor(1, 4)]
        };

        // Act
        TimingCopierEngine.Apply(target, source, options);

        // Assert
        target.HitObjects.Select(hitObject => hitObject.Time).Should().Equal(750);
        target.GetBookmarks().Should().Equal(700);
    }

    [TestMethod]
    public void Apply_KeepObjectsFixed_ReplacesTimingWithoutMovingMapContent()
    {
        // Arrange
        Beatmap source = CreateBeatmap(500);
        Beatmap target = CreateBeatmap(1000, 700);
        target.SetBookmarks([900]);
        TimingCopierOptions options = new()
        {
            ResnapMode = TimingCopierResnapModes.KeepObjectsFixed,
            BeatDivisors = [new RationalBeatDivisor(1, 4)]
        };

        // Act
        TimingCopierEngine.Apply(target, source, options);

        // Assert
        target.HitObjects.Select(hitObject => hitObject.Time).Should().Equal(700);
        target.GetBookmarks().Should().Equal(900);
        target.BeatmapTiming.Redlines.Select(point => point.MpB).Should().Equal(500);
    }

    [TestMethod]
    public void Apply_KeepObjectsFixed_PreservesTargetGreenlinesWhileReplacingRedlines()
    {
        // Arrange
        Beatmap source = CreateBeatmap(500);
        Beatmap target = CreateBeatmap(1000);
        TimingPoint targetGreenline = new(
            500,
            -100,
            4,
            SampleSet.Normal,
            0,
            100,
            uninherited: false,
            kiai: false,
            omitFirstBarLine: false);
        target.BeatmapTiming.Add(targetGreenline);
        TimingCopierOptions options = new()
        {
            ResnapMode = TimingCopierResnapModes.KeepObjectsFixed,
            BeatDivisors = [new RationalBeatDivisor(1, 4)]
        };

        // Act
        TimingCopierEngine.Apply(target, source, options);

        // Assert
        target.BeatmapTiming.Redlines.Select(point => point.MpB).Should().Equal(500);
        target.BeatmapTiming.Greenlines.Select(point => point.Offset).Should().Contain(500);
    }

    [TestMethod]
    public void Apply_WhenCancelledBeforeMutation_LeavesTargetUnchanged()
    {
        // Arrange
        Beatmap source = CreateBeatmap(500);
        Beatmap target = CreateBeatmap(1000, 1000);
        CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        TimingCopierOptions options = new()
        {
            ResnapMode = TimingCopierResnapModes.KeepObjectsFixed,
            BeatDivisors = [new RationalBeatDivisor(1, 4)]
        };

        // Act
        Action act = () => TimingCopierEngine.Apply(target, source, options, cancellation.Token);

        // Assert
        act.Should().Throw<OperationCanceledException>();
        target.HitObjects.Select(hitObject => hitObject.Time).Should().Equal(1000);
        target.BeatmapTiming.Redlines.Select(point => point.MpB).Should().Equal(1000);
    }

    private static Beatmap CreateBeatmap(double millisecondsPerBeat, params double[] objectTimes)
    {
        TimingPoint redline = new(
            0,
            millisecondsPerBeat,
            4,
            SampleSet.Normal,
            0,
            100,
            uninherited: true,
            kiai: false,
            omitFirstBarLine: false);
        List<HitObject> hitObjects = objectTimes
            .Select(time => new HitObject(time, 0, SampleSet.None, SampleSet.None))
            .ToList();
        return new Beatmap(hitObjects, [redline], redline);
    }
}
