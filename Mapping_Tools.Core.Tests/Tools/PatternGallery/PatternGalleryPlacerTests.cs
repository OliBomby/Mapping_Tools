using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.PatternGallery;

[TestClass]
public sealed class PatternGalleryPlacerTests
{
    [TestMethod]
    public void Validate_WithNonFinitePadding_ThrowsValidationException()
    {
        // Arrange
        PatternGalleryEngineOptions options = new()
        {
            Padding = double.PositiveInfinity,
        };

        // Act
        var act = () => PatternGalleryPlacer.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*numeric*");
    }

    [TestMethod]
    public void Validate_WithDefaultOptions_DoesNotThrow()
    {
        // Arrange
        PatternGalleryEngineOptions options = new();

        // Act
        var act = () => PatternGalleryPlacer.Validate(options);

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void PlaceOsuPatternAtTime_WithNoOverwrite_PreservesSourceAndPlacesAtRequestedTime()
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(64, 1000, 1500);
        Beatmap target = CreateBeatmap(256, 2000);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.NoOverwrite);

        // Act
        placer.PlaceOsuPatternAtTime(pattern, target, 5000);

        // Assert
        pattern.HitObjects.Select(item => item.Time).Should().Equal(1000, 1500);
        target.HitObjects.Select(item => item.Time).Should().Equal(2000, 5000, 5500);
        target.HitObjects.Where(item => item.Time >= 5000)
            .Select(item => item.Pos.X).Should().Equal(64, 64);
    }

    [TestMethod]
    public void PlaceOsuPattern_WithPartitionedOverwrite_PreservesTargetObjectInPatternGap()
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(64, 1000, 5000);
        Beatmap target = CreateBeatmap(256, 1000, 3000, 5000, 7000);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.PartitionedOverwrite);

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        target.HitObjects.Select(item => item.Time).Should().Equal(1000, 3000, 5000, 7000);
        target.HitObjects.Single(item => Precision.AlmostEquals(item.Time, 3000)).Pos.X.Should().Be(256);
        target.HitObjects.Single(item => Precision.AlmostEquals(item.Time, 1000)).Pos.X.Should().Be(64);
    }

    [TestMethod]
    public void PlaceOsuPattern_WithCompleteOverwrite_RemovesTargetObjectInPatternGap()
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(64, 1000, 5000);
        Beatmap target = CreateBeatmap(256, 1000, 3000, 5000, 7000);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.CompleteOverwrite);

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        target.HitObjects.Select(item => item.Time).Should().Equal(1000, 5000, 7000);
        target.HitObjects.Single(item => Precision.AlmostEquals(item.Time, 5000)).Pos.X.Should().Be(64);
    }

    [TestMethod]
    public void PlaceOsuPattern_WithExplicitOverwriteBounds_RemovesOnlyTargetEntriesInTheSpecifiedRange()
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(64, 995, 1500, 2005);
        Beatmap target = CreateBeatmap(256, 1000, 1005, 1800, 1995, 2000);
        target.BeatmapTiming.Add(new TimingPoint(1900, 375, 4, SampleSet.Normal, 0, 100, true, false, false));
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.PartitionedOverwrite);

        placer.Padding = 0;
        placer.TimingOverwriteMode = TimingOverwriteMode.PatternTimingOnly;

        // Act
        placer.PlaceOsuPattern(pattern, target, overwriteStartTime: 1005, overwriteEndTime: 1995);

        // Assert
        target.HitObjects.Where(item => Precision.AlmostEquals(item.Pos.X, 256))
            .Select(item => item.Time).Should().Equal(1000, 2000);
        target.HitObjects.Where(item => Precision.AlmostEquals(item.Pos.X, 64))
            .Select(item => item.Time).Should().Equal(995, 1500, 2005);
        target.BeatmapTiming.Redlines.Should().NotContain(point => Precision.AlmostEquals(point.Offset, 1900));
        target.BeatmapTiming.GetMpBAtTime(2010).Should().Be(375);
    }

    [TestMethod]
    public void PlaceOsuPatternAtTime_WithExplicitOverwriteBounds_ShiftsBothBoundsWithThePattern()
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(64, 1000, 1500);
        Beatmap target = CreateBeatmap(256, 4800, 4900, 5200, 5600, 5700);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.CompleteOverwrite);

        // Act
        placer.PlaceOsuPatternAtTime(pattern, target, overwriteStartTime: 900, overwriteEndTime: 1600, time: 5000);

        // Assert
        target.HitObjects.Select(item => item.Time).Should().Equal(4800, 5000, 5500, 5700);
    }

    [TestMethod]
    public void PlaceOsuPattern_WithTargetTwiceAsFast_ConvertsPatternBeatSpacing()
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(64, 1000, 1500, 2000);
        Beatmap target = CreateBeatmap(256, 250d, 3000);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.NoOverwrite);
        placer.ScaleToNewTiming = true;

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        target.HitObjects.Select(item => item.Time).Should().Equal(1000, 1250, 1500, 3000);
        pattern.HitObjects.Select(item => item.Time).Should().Equal(1000, 1500, 2000);
    }

    [TestMethod]
    public void PlaceOsuPattern_WithCustomScale_ScalesSliderPositionAndPixelLength()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        Beatmap pattern = new(
            [BeatmapTestData.DecodeHitObject("300,192,1000,2,0,L|320:192,1,20,0|0,0:0|0:0,0:0:0:0:")],
            [redline],
            redline);
        Beatmap target = CreateBeatmap(256, 3000);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.NoOverwrite);
        placer.CustomScale = 2;

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        HitObject placed = target.HitObjects.Single(item => item.IsSlider);
        placed.Pos.X.Should().Be(344);
        placed.GetAbsoluteControlPointPositions()[^1].X.Should().Be(384);
        placed.PixelLength.Should().Be(40);
        pattern.HitObjects[0].Pos.X.Should().Be(300);
        pattern.HitObjects[0].PixelLength.Should().Be(20);
    }

    [TestMethod]
    public void PlaceOsuPattern_WithRotation_RotatesAroundPlayfieldCentre()
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(356, 1000);
        Beatmap target = CreateBeatmap(256, 3000);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.NoOverwrite);
        placer.CustomRotate = Math.PI / 2;

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        HitObject placed = target.HitObjects.Single(item => Precision.AlmostEquals(item.Time, 1000));
        placed.Pos.X.Should().BeApproximately(256, 0.001);
        placed.Pos.Y.Should().BeApproximately(292, 0.001);
    }

    [TestMethod]
    public void PlaceOsuPattern_WithWholeBeatSnapping_MovesOffGridObjectsToNearestBeats()
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(64, 1020, 1490);
        Beatmap target = CreateBeatmap(256, 3000);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.NoOverwrite);
        placer.SnapToNewTiming = true;
        placer.BeatDivisors = [new RationalBeatDivisor(1)];

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        target.HitObjects.Select(item => item.Time).Should().Equal(1000, 1500, 3000);
        pattern.HitObjects.Select(item => item.Time).Should().Equal(1020, 1490);
    }

    [TestMethod]
    public void PlaceOsuPattern_WithScaleBeyondLeftEdge_ShiftsEntirePatternIntoPlayfield()
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(0, 1000);
        pattern.HitObjects.Add(BeatmapTestData.DecodeHitObject("100,192,1500,1,0,0:0:0:0:"));
        Beatmap target = CreateBeatmap(256, 3000);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.NoOverwrite);
        placer.CustomScale = 2;

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        target.HitObjects.Take(2).Select(item => item.Pos.X).Should().Equal(0, 200);
        pattern.HitObjects.Select(item => item.Pos.X).Should().Equal(0, 100);
    }

    [DataTestMethod]
    [DataRow(TimingOverwriteMode.PatternTimingOnly, 500d, 250d)]
    [DataRow(TimingOverwriteMode.InPatternAbsoluteTiming, 400d, 250d)]
    [DataRow(TimingOverwriteMode.InPatternRelativeTiming, 400d, 200d)]
    public void PlaceOsuPattern_WithTimingOverwriteMode_AppliesPatternChangeAndRestoresTargetTiming(
        TimingOverwriteMode mode, double expectedStartBeatLength, double expectedChangedBeatLength)
    {
        // Arrange
        TimingPoint patternStart = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        TimingPoint patternChange = new(1250, 250, 4, SampleSet.Normal, 0, 100, true, false, false);
        Beatmap pattern = new(
            [
                BeatmapTestData.DecodeHitObject("64,192,1000,1,0,0:0:0:0:"),
                BeatmapTestData.DecodeHitObject("64,192,1350,1,0,0:0:0:0:"),
                BeatmapTestData.DecodeHitObject("64,192,1500,1,0,0:0:0:0:"),
            ],
            [patternStart, patternChange],
            patternStart);
        Beatmap target = CreateBeatmap(256, 400d, 3000);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.NoOverwrite);
        placer.TimingOverwriteMode = mode;

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        target.BeatmapTiming.GetMpBAtTime(1000).Should().Be(expectedStartBeatLength);
        target.BeatmapTiming.GetMpBAtTime(1350).Should().Be(expectedChangedBeatLength);
        target.BeatmapTiming.GetMpBAtTime(1501).Should().Be(400);
    }

    [TestMethod]
    [DataRow(PatternOverwriteMode.PartitionedOverwrite, false)]
    [DataRow(PatternOverwriteMode.CompleteOverwrite, true)]
    public void PlaceOsuPattern_WithZeroDurationPart_PreservesTargetBpm(
        PatternOverwriteMode overwriteMode, bool targetHasRedlineAtPlacement)
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(64, 1000);
        pattern.BeatmapTiming.Add(new TimingPoint(1000, 400, 4, SampleSet.Normal, 0, 100, true, false, false));
        Beatmap target = CreateBeatmap(256, 2000);
        if (targetHasRedlineAtPlacement)
            target.BeatmapTiming.Add(new TimingPoint(1000, 600, 4, SampleSet.Normal, 0, 100, true, false, false));
        double expectedBeatLength = targetHasRedlineAtPlacement ? 600 : 500;
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(overwriteMode);
        placer.TimingOverwriteMode = TimingOverwriteMode.PatternTimingOnly;
        placer.Padding = 0;

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        target.HitObjects.Select(item => item.Time).Should().Equal(1000, 2000);
        target.BeatmapTiming.GetMpBAtTime(1000).Should().Be(expectedBeatLength);
        target.BeatmapTiming.GetMpBAtTime(2000).Should().Be(expectedBeatLength);
    }

    [DataTestMethod]
    [DataRow(PatternOverwriteMode.PartitionedOverwrite, 14, 1001d, 1001d)]
    [DataRow(PatternOverwriteMode.CompleteOverwrite, 14, 1001d, 1001d)]
    [DataRow(PatternOverwriteMode.PartitionedOverwrite, 128, 1000.4d, 1000.75d)]
    [DataRow(PatternOverwriteMode.CompleteOverwrite, 128, 1000.4d, 1000.75d)]
    public void PlaceOsuPattern_WithSubMillisecondSlider_RestoresTargetBpmBeforeNextSlider(
        PatternOverwriteMode overwriteMode, int version, double restoreTime, double nextSliderStart)
    {
        // Arrange
        TimingPoint patternRedline = new(0, 0.4, 4, SampleSet.Normal, 0, 100, true, false, false);
        Beatmap pattern = new(
            [BeatmapTestData.DecodeHitObject("64,192,1000,2,0,L|204:192,1,140")], [patternRedline]);
        TimingPoint targetRedline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        string nextSlider = FormattableString.Invariant($"256,192,{nextSliderStart},2,0,L|396:192,1,140");
        Beatmap target = new([BeatmapTestData.DecodeHitObject(nextSlider, version)], [targetRedline]) { Version = version };
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(overwriteMode);
        placer.TimingOverwriteMode = TimingOverwriteMode.PatternTimingOnly;
        placer.Padding = 0;

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        Beatmap result = new BeatmapDecoder().Decode(new BeatmapEncoder().Encode(target));
        result.HitObjects.Select(item => item.Time).Should().Equal(1000, nextSliderStart);
        result.HitObjects[0].TemporalLength.Should().BeApproximately(0.4, 1e-8);
        result.HitObjects[1].TemporalLength.Should().BeApproximately(500, 1e-8);
        result.BeatmapTiming.GetRedlineAtTime(nextSliderStart).Offset.Should().Be(restoreTime);
    }

    [DataTestMethod]
    [DataRow(false, 1)]
    [DataRow(true, 2)]
    public void PlaceOsuPattern_WithHitsoundOption_KeepsOrRemovesPatternWhistle(bool includeHitsounds, int expectedHitsound)
    {
        // Arrange
        Beatmap pattern = CreateBeatmap(64, 1000);
        pattern.HitObjects[0].Hitsounds = 2;
        Beatmap target = CreateBeatmap(256, 3000);
        PatternGalleryPlacer placer = CreateUnsnappedPlacer(PatternOverwriteMode.NoOverwrite);
        placer.IncludeHitsounds = includeHitsounds;

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        target.HitObjects[0].Hitsounds.Should().Be(expectedHitsound);
        pattern.HitObjects[0].Hitsounds.Should().Be(2);
    }

    [TestMethod]
    public void PlaceOsuPattern_WithAdjacentLegacySubMillisecondParts_RestoresTargetBpmAfterBothSliders()
    {
        // Arrange
        TimingPoint patternRedline = new(0, 0.1, 4, SampleSet.Normal, 0, 100, true, false, false);
        Beatmap pattern = new(
            [
                BeatmapTestData.DecodeHitObject("64,192,1000,2,0,L|204:192,1,140"),
                BeatmapTestData.DecodeHitObject("96,192,1001,2,0,L|236:192,1,140"),
            ],
            [patternRedline]);
        TimingPoint targetRedline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        Beatmap target = new(
            [BeatmapTestData.DecodeHitObject("256,192,1002,2,0,L|396:192,1,140")], [targetRedline]) { Version = 14 };
        var placer = CreateUnsnappedPlacer(PatternOverwriteMode.PartitionedOverwrite);
        placer.TimingOverwriteMode = TimingOverwriteMode.PatternTimingOnly;
        placer.Padding = 0;

        // Act
        placer.PlaceOsuPattern(pattern, target);

        // Assert
        Beatmap result = new BeatmapDecoder().Decode(new BeatmapEncoder().Encode(target));
        result.HitObjects.Select(item => item.Time).Should().Equal(1000, 1001, 1002);
        result.HitObjects[0].TemporalLength.Should().BeApproximately(0.1, 1e-8);
        result.HitObjects[1].TemporalLength.Should().BeApproximately(0.1, 1e-8);
        result.HitObjects[2].TemporalLength.Should().BeApproximately(500, 1e-8);
    }

    private static Beatmap CreateBeatmap(int x, params int[] times)
    {
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        List<HitObject> objects = times
            .Select(time => BeatmapTestData.DecodeHitObject($"{x},192,{time},1,0,0:0:0:0:"))
            .ToList();
        return new Beatmap(objects, [redline], redline);
    }

    private static Beatmap CreateBeatmap(int x, double beatLength, params int[] times)
    {
        TimingPoint redline = new(0, beatLength, 4, SampleSet.Normal, 0, 100, true, false, false);
        List<HitObject> objects = times
            .Select(time => BeatmapTestData.DecodeHitObject($"{x},192,{time},1,0,0:0:0:0:"))
            .ToList();
        return new Beatmap(objects, [redline], redline);
    }

    private static PatternGalleryPlacer CreateUnsnappedPlacer(PatternOverwriteMode overwriteMode)
    {
        return new PatternGalleryPlacer
        {
            PatternOverwriteMode = overwriteMode,
            ScaleToNewTiming = false,
            SnapToNewTiming = false,
            FixColourHax = false,
        };
    }
}
