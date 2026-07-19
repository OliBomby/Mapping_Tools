using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class HitObjectTests {
    [TestMethod]
    public void CircleLine_ParsesAndRoundTrips() {
        const string line = "256,192,1000,5,2,2:3:4:75:custom.wav";
        var hitObject = new HitObject(line);

        Assert.IsTrue(hitObject.IsCircle);
        Assert.IsTrue(hitObject.NewCombo);
        Assert.IsTrue(hitObject.Whistle);
        Assert.AreEqual(SampleSet.Soft, hitObject.SampleSet);
        Assert.AreEqual(SampleSet.Drum, hitObject.AdditionSet);
        Assert.AreEqual(4, hitObject.CustomIndex);
        Assert.AreEqual(75d, hitObject.SampleVolume);
        Assert.AreEqual("custom.wav", hitObject.Filename);
        Assert.AreEqual(line, hitObject.GetLine());
    }

    [TestMethod]
    public void SliderLine_ParsesPathEdgesAndRoundTrips() {
        const string line = "64,96,1200,6,2,B|128:96|192:128,2,240,2|8|0,1:2|2:3|3:1,1:2:3:60:";
        var hitObject = new HitObject(line);

        Assert.IsTrue(hitObject.IsSlider);
        Assert.AreEqual(PathType.Bezier, hitObject.SliderType);
        Assert.AreEqual(2, hitObject.Repeat);
        CollectionAssert.AreEqual(new[] { 2, 8, 0 }, hitObject.EdgeHitsounds);
        CollectionAssert.AreEqual(new[] { SampleSet.Normal, SampleSet.Soft, SampleSet.Drum }, hitObject.EdgeSampleSets);
        CollectionAssert.AreEqual(new[] { SampleSet.Soft, SampleSet.Drum, SampleSet.Normal }, hitObject.EdgeAdditionSets);
        Assert.AreEqual(line, hitObject.GetLine());
    }

    [TestMethod]
    public void HoldNoteLine_UsesEndTimeFromObjectParams() {
        const string line = "128,192,2000,128,0,2500:1:2:3:40:hold.wav";
        var hitObject = new HitObject(line);

        Assert.IsTrue(hitObject.IsHoldNote);
        Assert.AreEqual(2500d, hitObject.EndTime);
        Assert.AreEqual(line, hitObject.GetLine());
    }

    [TestMethod]
    public void Comparer_CanIgnorePositionAndTime() {
        var first = new HitObject("64,96,1000,1,0,0:0:0:0:");
        var second = new HitObject("128,192,2000,1,0,0:0:0:0:");

        Assert.IsTrue(new HitObjectComparer(checkPosition: false, checkTime: false).Equals(first, second));
        Assert.IsFalse(new HitObjectComparer().Equals(first, second));
    }
}
