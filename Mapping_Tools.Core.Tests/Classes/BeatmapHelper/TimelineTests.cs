using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class TimelineTests {
    [TestMethod]
    public void CircleAndSpinner_CreateChronologicalTimelineObjects() {
        var spinner = new HitObject("256,192,500,8,4,1500,0:0:0:0:");
        var circle = new HitObject("256,192,1000,1,2,0:0:0:0:");

        var timeline = new Timeline(new List<HitObject> { circle, spinner }, new Timing(1.4));

        CollectionAssert.AreEqual(new[] { 500d, 1000d, 1500d }, timeline.TimelineObjects.Select(x => x.Time).ToArray());
        Assert.IsFalse(timeline.TimelineObjects[0].HasHitsound);
        Assert.IsTrue(timeline.TimelineObjects[1].Whistle);
        Assert.IsTrue(timeline.TimelineObjects[2].Finish);
    }

    [TestMethod]
    public void FileName_UsesModeSetHitsoundAndIndex() {
        string filename = TimelineObject.GetFileName(SampleSet.Drum, Hitsound.Clap, 3, GameMode.Taiko);

        Assert.AreEqual("taiko-drum-hitclap3", filename);
    }
}
