using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class TimingTests {
    [TestMethod]
    public void TimingPoint_ParseAndSerialize_PreservesLine() {
        const string line = "1000,500,4,2,3,75,1,9";

        var timingPoint = new TimingPoint(line);

        Assert.AreEqual(line, timingPoint.GetLine());
        Assert.AreEqual(SampleSet.Soft, timingPoint.SampleSet);
        Assert.IsTrue(timingPoint.Uninherited);
        Assert.IsTrue(timingPoint.Kiai);
        Assert.IsTrue(timingPoint.OmitFirstBarLine);
    }

    [TestMethod]
    public void TimingPoint_InvalidMillisecondsPerBeat_ThrowsParsingException() {
        const string line = "1000,not-a-number,4,2,3,75,1,0";

        Assert.ThrowsException<BeatmapParsingException>(() => new TimingPoint(line));
    }

    [TestMethod]
    public void Timing_SortsAndResnapsAgainstActiveRedline() {
        var laterGreenline = new TimingPoint("2000,-100,4,1,0,50,0,0");
        var redline = new TimingPoint("1000,500,4,1,0,100,1,0");
        var timing = new Timing(new List<TimingPoint> { laterGreenline, redline }, 1.4);

        double snapped = timing.Resnap(1260, new IBeatDivisor[] { new RationalBeatDivisor(4) }, floor: false);

        Assert.AreSame(redline, timing[0]);
        Assert.AreEqual(1250, snapped, 0.0001);
    }

    [TestMethod]
    public void ResnapInRange_WhenSnapWouldCrossBoundary_KeepsOriginalTime() {
        var timing = new Timing(new[] { "1000,500,4,1,0,100,1,0" }, 1.4);

        double snapped = timing.ResnapInRange(
            1110,
            new IBeatDivisor[] { new RationalBeatDivisor(4) },
            rangeStart: 1124,
            rangeEnd: 2000,
            floor: false);

        Assert.AreEqual(1110, snapped, 0.0001);
    }
}
