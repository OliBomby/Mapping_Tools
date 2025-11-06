using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.IO.Editor;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

[TestClass]
public class BeatmapExtensionTests {
    [TestMethod]
    public void QueryTimeCodeTest() {
        var path = Path.Join("Resources", "ComplicatedTestMap.osu");
        var beatmap = new BeatmapEditor(path).ReadFile();

        var hos = beatmap.QueryTimeCode("00:56:823 (1,2,1,2) - ").ToArray();
        Assert.AreEqual(0, hos.Length);


        hos = beatmap.QueryTimeCode("00:00:015 (1,2,3,4,5,1) - ").ToArray();
        Assert.AreEqual(6, hos.Length);
    }

    [TestMethod]
    public void FixBreakPeriodsTest() {
        var path = Path.Join("Resources", "ComplicatedTestMap.osu");
        var beatmap = new BeatmapEditor(path).ReadFile();

        beatmap.Storyboard.BreakPeriods.Add(new Break(-10000, -5000));
        beatmap.Storyboard.BreakPeriods.Add(new Break(0, 2000));
        beatmap.Storyboard.BreakPeriods.Add(new Break(0, 20000));

        Assert.AreEqual(3, beatmap.Storyboard.BreakPeriods.Count);

        beatmap.FixBreakPeriods();

        Assert.AreEqual(0, beatmap.Storyboard.BreakPeriods.Count);

        beatmap.HitObjects.RemoveAll(ho => ho.StartTime > 15 && ho.StartTime < 2000);
        beatmap.Storyboard.BreakPeriods.Add(new Break(-10000, -5000));
        beatmap.Storyboard.BreakPeriods.Add(new Break(0, 2000));
        beatmap.Storyboard.BreakPeriods.Add(new Break(0, 20000));

        Assert.AreEqual(2, beatmap.HitObjects.Count);
        Assert.AreEqual(3, beatmap.Storyboard.BreakPeriods.Count);

        beatmap.FixBreakPeriods();
        var approachTime = beatmap.Difficulty.ApproachTime;
        var prevEndTime = beatmap.HitObjects[0].EndTime;

        Assert.AreEqual(1, beatmap.Storyboard.BreakPeriods.Count);
        Assert.AreEqual(prevEndTime + 200, beatmap.Storyboard.BreakPeriods[0].StartTime, Precision.DOUBLE_EPSILON);
        Assert.AreEqual(2109 - approachTime, beatmap.Storyboard.BreakPeriods[0].EndTime, Precision.DOUBLE_EPSILON);
    }
}