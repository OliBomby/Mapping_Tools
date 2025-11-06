using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.IO.Editor;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.MapCleanerStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using Mapping_Tools.Core.Audio.DuplicateDetection;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tests.Tools.MapCleanerStuff;

[TestClass]
public class MapCleanerTests {
    [TestMethod]
    [Timeout(10000)]
    public void TestMapCleaner() {
        var path = Path.Combine("Resources", "TestMapset1",
            "Ne Obliviscaris - Forget Not (Dereban) [Dreamscape] dirty.osu");
        var beatmap = new BeatmapEditor(path).ReadFile();
        var mapset = new BeatmapSetEditor(Path.GetDirectoryName(path)).ReadFile();
        beatmap.BeatmapSet = mapset;
        var mapDir = Path.GetDirectoryName(beatmap.GetBeatmapSetRelativePath()) ?? string.Empty;

        var sampleMap = new MonolithicDuplicateSampleDetector().AnalyzeSamples(mapset.SoundFiles, out var exception);

        Assert.IsNull(exception);

        var timelineBefore = beatmap.GetTimeline();

        double progress = 0;
        var result = MapCleaner.CleanMap(beatmap,
            new MapCleanerArgs(true, true, true, true, true, false, false, RationalBeatDivisor.GetDefaultBeatDivisors()),
            p => progress = p);

        var timelineAfter = beatmap.GetTimeline();

        Console.WriteLine($"Greenlines removed: {result.TimingPointsRemoved}, Objects resnapped: {result.ObjectsResnapped}");

        Assert.AreEqual(100, progress, Precision.DOUBLE_EPSILON);

        Assert.AreEqual(timelineBefore.TimelineObjects.Count, timelineAfter.TimelineObjects.Count);

        // Check a specific resnap
        var tloTest = timelineAfter.GetNearestTlo(266076);
        Assert.AreEqual(266081, tloTest.Time);

        for (int i = 0; i < timelineBefore.TimelineObjects.Count; i++) {
            var tloBefore = timelineBefore.TimelineObjects[i];
            var tloAfter = timelineAfter.TimelineObjects[i];

            Assert.AreEqual(tloBefore.Hitsounds.Whistle, tloAfter.Hitsounds.Whistle);
            Assert.AreEqual(tloBefore.Hitsounds.Clap, tloAfter.Hitsounds.Clap);
            Assert.AreEqual(tloBefore.Hitsounds.Finish, tloAfter.Hitsounds.Finish);
            Assert.AreEqual(tloBefore.Hitsounds.Filename, tloAfter.Hitsounds.Filename);
            Assert.AreEqual(tloBefore.FenoSampleSet, tloAfter.FenoSampleSet);
            Assert.AreEqual(tloBefore.FenoAdditionSet, tloAfter.FenoAdditionSet);
            Assert.AreEqual(tloBefore.FenoSampleVolume, tloAfter.FenoSampleVolume);

            var filenamesBefore = new HashSet<string>(tloBefore.GetFirstPlayingFilenames(beatmap.General.Mode, mapDir, sampleMap));
            var filenamesAfter = new HashSet<string>(tloAfter.GetFirstPlayingFilenames(beatmap.General.Mode, mapDir, sampleMap));

            Assert.AreEqual(filenamesBefore.Count, filenamesAfter.Count);

            foreach (var filename in filenamesBefore) {
                Assert.IsTrue(filenamesAfter.Contains(filename), $"Could not find filename ({filename}) in timeline object at {tloBefore.Time}.");
            }
        }
    }
}