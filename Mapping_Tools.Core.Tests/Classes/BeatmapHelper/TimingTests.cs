using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class TimingTests
{
    [TestMethod]
    public void TimingPoint_ParseAndSerialize_PreservesLine()
    {
        // Arrange
        const string line = "1000,500,4,2,3,75,1,9";

        // Act
        var timingPoint = new TimingPoint(line);

        // Assert
        timingPoint.GetLine().Should().Be(line);
        timingPoint.SampleSet.Should().Be(SampleSet.Soft);
        timingPoint.Uninherited.Should().BeTrue();
        timingPoint.Kiai.Should().BeTrue();
        timingPoint.OmitFirstBarLine.Should().BeTrue();
    }

    [TestMethod]
    public void TimingPoint_InvalidMillisecondsPerBeat_ThrowsParsingException()
    {
        // Arrange
        const string line = "1000,not-a-number,4,2,3,75,1,0";

        // Act
        Action act1 = () => new TimingPoint(line);

        // Assert
        act1.Should().Throw<BeatmapParsingException>();
    }

    [TestMethod]
    public void Timing_SortsAndResnapsAgainstActiveRedline()
    {
        // Arrange
        var laterGreenline = new TimingPoint("2000,-100,4,1,0,50,0,0");
        var redline = new TimingPoint("1000,500,4,1,0,100,1,0");
        var timing = new Timing(new List<TimingPoint> { laterGreenline, redline }, 1.4);

        // Act
        double snapped = timing.Resnap(1260, new IBeatDivisor[] { new RationalBeatDivisor(4) }, false);

        // Assert
        timing[0].Should().BeSameAs(redline);
        snapped.Should().BeApproximately(1250, 0.0001);
    }

    [TestMethod]
    public void ResnapInRange_WhenSnapWouldCrossBoundary_KeepsOriginalTime()
    {
        // Arrange
        var timing = new Timing(new[] { "1000,500,4,1,0,100,1,0" }, 1.4);

        // Act
        double snapped = timing.ResnapInRange(
            1110,
            new IBeatDivisor[] { new RationalBeatDivisor(4) },
            1124,
            2000,
            false);

        // Assert
        snapped.Should().BeApproximately(1110, 0.0001);
    }
}
