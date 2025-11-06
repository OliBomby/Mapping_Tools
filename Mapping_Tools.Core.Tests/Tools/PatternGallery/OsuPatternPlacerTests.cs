using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Contexts;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools.Core.BeatmapHelper.IO.Decoding;
using Mapping_Tools.Core.BeatmapHelper.IO.Decoding.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.IO.Editor;
using Mapping_Tools.Core.BeatmapHelper.TimingStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.PatternGallery;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.Tests.Tools.PatternGallery;

[TestClass]
public class OsuPatternPlacerTests {
    private static IBeatmap GetPattern1() {
        var maker = new OsuPatternMaker();

        var decoder = new HitObjectDecoder();
        var hitObjects = new List<HitObject> {
            decoder.Decode("245,44,0,6,0,B|249:111|215:269|370:-5|-72:203|658:203|216:-5|371:269|337:111|341:44,1,199.999980926514"),
            decoder.Decode("105,142,600,1,0,0:0:0:0:"),
            decoder.Decode("256,192,900,12,2,1500,1:0:0:0:"),
            decoder.Decode("471,55,1800,6,6,P|462:191|385:92,1,299.99997138977,0|0,1:0|1:0,0:0:0:0:")
        };

        var tpDecoder = new TimingPointDecoder();
        var timingPoints = new List<TimingPoint> {
            tpDecoder.Decode("0,600,4,2,100,97,1,0"),
            tpDecoder.Decode("0,-25,4,2,100,97,0,0"),
            tpDecoder.Decode("1800,-100,4,2,100,97,0,0"),
            tpDecoder.Decode("1950,-25,4,1,100,97,0,0")
        };

        maker.FromObjects(hitObjects, timingPoints, out IBeatmap patternBeatmap, "test", globalSv: 1);
        return patternBeatmap;
    }

    [TestMethod]
    public void ExportPatternTimingTest() {
        var path = Path.Join("Resources", "SAMString - Forget The Promise (DeviousPanda) [Elysium].osu");
        var beatmap = new BeatmapEditor(path).ReadFile();

        var placer = new OsuPatternPlacer {
            BeatDivisors = new IBeatDivisor[] {new RationalBeatDivisor(4)},
            FixBpmSv = true,
            FixColourHax = true,
            FixGlobalSv = true,
            IncludeHitsounds = true,
            IncludeKiai = true,
            PatternOverwriteMode = PatternOverwriteMode.CompleteOverwrite,
            TimingOverwriteMode = TimingOverwriteMode.PatternTimingOnly,
            ScaleToNewCircleSize = false,
            ScaleToNewTiming = false,
            SnapToNewTiming = true
        };

        placer.PlaceOsuPatternAtTime(GetPattern1(), beatmap, 101120);

        var patternHitObjects = beatmap.GetHitObjectsWithRangeInRange(101120, 101120 + 3600);

        PrintHitObjects(patternHitObjects);

        Assert.AreEqual(4, patternHitObjects.Count);

        Assert.IsInstanceOfType(patternHitObjects[0], typeof(Slider));
        Assert.IsInstanceOfType(patternHitObjects[1], typeof(HitCircle));
        Assert.IsInstanceOfType(patternHitObjects[2], typeof(Spinner));
        Assert.IsInstanceOfType(patternHitObjects[3], typeof(Slider));

        var testTp = beatmap.BeatmapTiming.GetTimingPointAtTime(106173);

        Assert.AreEqual(0.3, testTp.GetSliderVelocity(), Precision.DOUBLE_EPSILON);
        Assert.AreEqual(6, testTp.SampleIndex);

        Assert.AreEqual(190, beatmap.BeatmapTiming.GetBpmAtTime(106173), Precision.DOUBLE_EPSILON);
        Assert.AreEqual(100, beatmap.BeatmapTiming.GetBpmAtTime(101120), Precision.DOUBLE_EPSILON);
    }

    [TestMethod]
    public void ExportOriginalTimingTest() {
        var path = Path.Join("Resources", "SAMString - Forget The Promise (DeviousPanda) [Elysium].osu");
        var beatmap = new BeatmapEditor(path).ReadFile();

        var placer = new OsuPatternPlacer {
            BeatDivisors = new IBeatDivisor[] { new RationalBeatDivisor(4) },
            FixBpmSv = false,
            FixColourHax = true,
            FixGlobalSv = true,
            IncludeHitsounds = true,
            IncludeKiai = true,
            PatternOverwriteMode = PatternOverwriteMode.CompleteOverwrite,
            TimingOverwriteMode = TimingOverwriteMode.DestinationTimingOnly,
            ScaleToNewCircleSize = false,
            ScaleToNewTiming = true,
            SnapToNewTiming = true
        };

        placer.PlaceOsuPatternAtTime(GetPattern1(), beatmap, 101120);

        var patternHitObjects = beatmap.GetHitObjectsWithRangeInRange(101120, 101120 + 3600/1.9);

        PrintHitObjects(patternHitObjects);

        Assert.AreEqual(4, patternHitObjects.Count);

        Assert.IsInstanceOfType(patternHitObjects[0], typeof(Slider));
        Assert.IsInstanceOfType(patternHitObjects[1], typeof(HitCircle));
        Assert.IsInstanceOfType(patternHitObjects[2], typeof(Spinner));
        Assert.IsInstanceOfType(patternHitObjects[3], typeof(Slider));

        var testTp = beatmap.BeatmapTiming.GetTimingPointAtTime(106173);

        Assert.AreEqual(0.3, testTp.GetSliderVelocity(), Precision.DOUBLE_EPSILON);
        Assert.AreEqual(6, testTp.SampleIndex);

        Assert.AreEqual(190, beatmap.BeatmapTiming.GetBpmAtTime(106173), Precision.DOUBLE_EPSILON);
        Assert.AreEqual(190, beatmap.BeatmapTiming.GetBpmAtTime(101120), Precision.DOUBLE_EPSILON);

        var msBeatDelta = 1 / beatmap.BeatmapTiming.GetMpBAtTime(101120);
        Assert.AreEqual(0.5, beatmap.BeatmapTiming.GetBeatLength(patternHitObjects[0].EndTime, patternHitObjects[1].StartTime), msBeatDelta);
        Assert.AreEqual(0.5, beatmap.BeatmapTiming.GetBeatLength(patternHitObjects[1].EndTime, patternHitObjects[2].StartTime), msBeatDelta);
        Assert.AreEqual(0.5, beatmap.BeatmapTiming.GetBeatLength(patternHitObjects[2].EndTime, patternHitObjects[3].StartTime), msBeatDelta);
        Assert.AreEqual(3, beatmap.BeatmapTiming.GetBeatLength(patternHitObjects[3].StartTime, patternHitObjects[3].EndTime), msBeatDelta);
    }


    [TestMethod]
    public void ExportCustomOverwriteWindowTest() {
        var path = Path.Join("Resources", "SAMString - Forget The Promise (DeviousPanda) [Elysium].osu");
        var beatmap = new BeatmapEditor(path).ReadFile();

        var placer = new OsuPatternPlacer {
            BeatDivisors = new IBeatDivisor[] { new RationalBeatDivisor(4) },
            FixBpmSv = false,
            FixColourHax = true,
            FixGlobalSv = true,
            IncludeHitsounds = true,
            IncludeKiai = true,
            PatternOverwriteMode = PatternOverwriteMode.PartitionedOverwrite,
            TimingOverwriteMode = TimingOverwriteMode.PatternTimingOnly,
            ScaleToNewCircleSize = false,
            ScaleToNewTiming = true,
            SnapToNewTiming = true,
            Padding = 0,
        };

        var pattern = GetPattern1();
        pattern.BeatmapTiming.Add(new TimingPoint(3600, 60000d / 190, 4, SampleSet.Soft, 1, 40, true, false, false));
        beatmap.BeatmapTiming.Add(new TimingPoint(121331, 60000d / 190, 4, SampleSet.Soft, 1, 40, true, false, false));

        placer.PlaceOsuPattern(
            pattern,
            beatmap,
            101120,
            overwriteStartTime: 99857 - 101120,
            overwriteEndTime: 121331 - 101120  // 121331 is the estimated end time in the target beatmap if the time scaling ends up being 1:1 which is the case with PatternTimingOnly
        );

        var patternHitObjects = beatmap.GetHitObjectsWithRangeInRange(99857, 121331);

        PrintHitObjects(patternHitObjects);

        Assert.AreEqual(4, patternHitObjects.Count);

        Assert.IsInstanceOfType(patternHitObjects[0], typeof(Slider));
        Assert.IsInstanceOfType(patternHitObjects[1], typeof(HitCircle));
        Assert.IsInstanceOfType(patternHitObjects[2], typeof(Spinner));
        Assert.IsInstanceOfType(patternHitObjects[3], typeof(Slider));

        Assert.AreEqual(190, beatmap.BeatmapTiming.GetBpmAtTime(99856), Precision.DOUBLE_EPSILON);
        Assert.AreEqual(600, beatmap.BeatmapTiming.GetMpBAtTime(99857), Precision.DOUBLE_EPSILON);

        Assert.AreEqual(600, beatmap.BeatmapTiming.GetMpBAtTime(104719), Precision.DOUBLE_EPSILON);
        Assert.AreEqual(190, beatmap.BeatmapTiming.GetBpmAtTime(104720), Precision.DOUBLE_EPSILON);

        Assert.AreEqual(190, beatmap.BeatmapTiming.GetBpmAtTime(121330), Precision.DOUBLE_EPSILON);
        Assert.AreEqual(190, beatmap.BeatmapTiming.GetBpmAtTime(121331), Precision.DOUBLE_EPSILON);

        var redline = beatmap.BeatmapTiming.GetRedlineAtTime(121331);

        Assert.IsNotNull(redline);
        Assert.AreEqual(60000d / 190, redline.MpB, Precision.DOUBLE_EPSILON);
        Assert.AreEqual(121331, redline.Offset, Precision.DOUBLE_EPSILON);
    }

    [TestMethod]
    public void PartitionedOverwriteZeroLengthSliderBpmTest() {
        var maker = new OsuPatternMaker();

        var decoder = new HitObjectDecoder();
        var hitObjects = new List<HitObject> {
            decoder.Decode("100,142,100,1,0,0:0:0:0:"),
            decoder.Decode("199,44,199,6,0,B|249:111|215:269|370:-5|-72:203|658:203|216:-5|371:269|337:111|341:44,1,20000"),
            decoder.Decode("200,142,200,1,0,0:0:0:0:"),
            decoder.Decode("400,142,400,1,0,0:0:0:0:"),
        };

        var tpDecoder = new TimingPointDecoder();
        var timingPoints = new List<TimingPoint> {
            tpDecoder.Decode("100,200,4,2,100,97,1,0"),
            tpDecoder.Decode("199,1E-2900,4,2,100,97,1,0"),
            tpDecoder.Decode("199,NaN,4,2,100,97,0,0"),
            tpDecoder.Decode("200,200,4,2,100,97,1,0"),
        };

        maker.FromObjects(hitObjects, timingPoints, out IBeatmap patternBeatmap2, "test", globalSv: 1);
        patternBeatmap2.GiveObjectsTimingContext();

        Assert.AreEqual(199, patternBeatmap2.HitObjects[1].GetEndTime(true), Precision.DOUBLE_EPSILON);

        var path = Path.Join("Resources", "EmptyTestMap.osu");
        var beatmap = new BeatmapEditor(path).ReadFile();

        var placer = new OsuPatternPlacer {
            BeatDivisors = new IBeatDivisor[] { new RationalBeatDivisor(4) },
            FixBpmSv = false,
            FixColourHax = true,
            FixGlobalSv = true,
            IncludeHitsounds = true,
            IncludeKiai = true,
            PatternOverwriteMode = PatternOverwriteMode.PartitionedOverwrite,
            TimingOverwriteMode = TimingOverwriteMode.PatternTimingOnly,
            ScaleToNewCircleSize = false,
            ScaleToNewTiming = false,
            SnapToNewTiming = false
        };

        placer.PlaceOsuPattern(patternBeatmap2, beatmap);

        var patternHitObjects = beatmap.HitObjects;

        PrintHitObjects(patternHitObjects);
        PrintTimingPoints(beatmap.BeatmapTiming.TimingPoints);

        Assert.AreEqual(4, patternHitObjects.Count);

        Assert.IsInstanceOfType(patternHitObjects[0], typeof(HitCircle));
        Assert.IsInstanceOfType(patternHitObjects[1], typeof(Slider));
        Assert.IsInstanceOfType(patternHitObjects[2], typeof(HitCircle));
        Assert.IsInstanceOfType(patternHitObjects[3], typeof(HitCircle));

        Assert.AreEqual(199, patternHitObjects[1].GetEndTime(true), Precision.DOUBLE_EPSILON);
        Assert.AreEqual(20000, ((Slider)patternHitObjects[1]).PixelLength, Precision.DOUBLE_EPSILON);

        var testTp = beatmap.BeatmapTiming.GetGreenlineAtTime(199);

        Assert.IsTrue(double.IsNaN(testTp.GetSliderVelocity()));

        Assert.AreEqual(double.PositiveInfinity, beatmap.BeatmapTiming.GetBpmAtTime(199));
        Assert.AreEqual(60000 / 200d, beatmap.BeatmapTiming.GetBpmAtTime(200), Precision.DOUBLE_EPSILON);
    }

    private static void PrintHitObjects(IEnumerable<HitObject> hitobjects) {
        foreach (var patternHitObject in hitobjects) {
            Console.WriteLine("Start time: " + patternHitObject.StartTime);
            Console.WriteLine("End time: " + patternHitObject.EndTime);
            Console.WriteLine(patternHitObject);
            Console.WriteLine(patternHitObject.GetContext<TimingContext>());
        }
    }

    private static void PrintTimingPoints(IEnumerable<TimingPoint> timingPoints) {
        foreach (var tp in timingPoints) {
            Console.WriteLine(tp);
        }
    }
}