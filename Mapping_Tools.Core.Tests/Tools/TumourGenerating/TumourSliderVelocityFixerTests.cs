using FluentAssertions;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.TumourGenerating;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.TumourGenerating;

[TestClass]
public sealed class TumourSliderVelocityFixerTests
{
    [TestMethod]
    public void Fix_WithGreenlineVelocity_LeavesObjectTimesAndAddsSelectedVelocityChanges()
    {
        // Arrange
        (Beatmap beatmap, HitObject selected, HitObject unselected) = CreateBeatmap();
        double selectedTime = selected.Time;
        double unselectedTime = unselected.Time;
        selected.SliderVelocity = -200;
        double selectedVelocity = selected.SliderVelocity;

        // Act
        TumourSliderVelocityFixer.Fix(beatmap, [selected], delegateToBpm: false, removeSliderTicks: false);

        // Assert
        selected.Time.Should().Be(selectedTime);
        unselected.Time.Should().Be(unselectedTime);
        beatmap.BeatmapTiming.Greenlines.Should().Contain(point =>
            point.Offset == selectedTime && point.MpB == selectedVelocity);
    }

    [TestMethod]
    public void Fix_WithBpmDelegation_MovesSelectedSliderAndCreatesBeforeAndAfterRedlines()
    {
        // Arrange
        (Beatmap beatmap, HitObject selected, _) = CreateBeatmap();
        double selectedTime = selected.Time;
        selected.SliderVelocity = -200;

        // Act
        TumourSliderVelocityFixer.Fix(beatmap, [selected], delegateToBpm: true, removeSliderTicks: true);

        // Assert
        selected.Time.Should().Be(selectedTime - 1);
        double.IsNaN(selected.SliderVelocity).Should().BeTrue();
        beatmap.BeatmapTiming.Redlines.Should().Contain(point => point.Offset == selectedTime - 1);
        beatmap.BeatmapTiming.Redlines.Should().Contain(point => point.Offset == selectedTime);
    }

    [TestMethod]
    public void Fix_WhenCancellationIsRequestedBeforeIteration_LeavesBeatmapUnchanged()
    {
        // Arrange
        (Beatmap beatmap, HitObject selected, _) = CreateBeatmap();
        int timingPointCount = beatmap.BeatmapTiming.TimingPoints.Count;
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Action act = () => TumourSliderVelocityFixer.Fix(
            beatmap,
            [selected],
            delegateToBpm: true,
            removeSliderTicks: true,
            cancellationToken: cancellation.Token);

        // Assert
        act.Should().Throw<OperationCanceledException>();
        selected.Time.Should().Be(100);
        beatmap.BeatmapTiming.TimingPoints.Should().HaveCount(timingPointCount);
    }

    private static (Beatmap Beatmap, HitObject Selected, HitObject Unselected) CreateBeatmap()
    {
        TimingPoint redline = new(
            0,
            500,
            4,
            SampleSet.Normal,
            0,
            100,
            uninherited: true,
            kiai: false,
            omitFirstBarLine: false);
        HitObject selected = new("64,64,100,2,0,L|164:64,1,100");
        HitObject unselected = new("64,64,300,2,0,L|164:64,1,100");
        return (new Beatmap([selected, unselected], [redline], redline), selected, unselected);
    }
}
