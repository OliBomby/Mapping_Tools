using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

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
        Action act1 = () => _ = new TimingPoint(line);

        // Assert
        act1.Should().Throw<BeatmapParsingException>();
    }

    [TestMethod]
    public void Timing_SortsAndResnapsAgainstActiveRedline()
    {
        // Arrange
        var laterGreenline = new TimingPoint("2000,-100,4,1,0,50,0,0");
        var redline = new TimingPoint("1000,500,4,1,0,100,1,0");
        var timing = new Timing([laterGreenline, redline], 1.4);

        // Act
        double snapped = timing.Resnap(1260, [new RationalBeatDivisor(4)], false);

        // Assert
        timing[0].Should().BeSameAs(redline);
        snapped.Should().BeApproximately(1250, 0.0001);
    }

    [TestMethod]
    public void ResnapInRange_WhenSnapWouldCrossBoundary_KeepsOriginalTime()
    {
        // Arrange
        var timing = new Timing(["1000,500,4,1,0,100,1,0"], 1.4);

        // Act
        double snapped = timing.ResnapInRange(
            1110,
            [new RationalBeatDivisor(4)],
            1124,
            2000,
            false);

        // Assert
        snapped.Should().BeApproximately(1110, 0.0001);
    }

    [TestMethod]
    public void GetBeatLength_AcrossRedline_SumsEachTempoSegmentAndPreservesDirection()
    {
        // Arrange
        var timing = new Timing(
        [
            CreateRedline(0, 500),
            CreateRedline(1000, 250),
        ],
        1.4);

        // Act
        double forward = timing.GetBeatLength(500, 1500);
        double backward = timing.GetBeatLength(1500, 500);

        // Assert
        forward.Should().BeApproximately(3, 0.000001);
        backward.Should().BeApproximately(-3, 0.000001);
    }

    [TestMethod]
    public void GetMilliseconds_AcrossBeatDomainRedline_UsesTempoOnEachSide()
    {
        // Arrange
        var timing = new Timing(
        [
            CreateRedline(0, 500),
            CreateRedline(2, 250),
        ],
        1.4);

        // Act
        double milliseconds = timing.GetMilliseconds(4, originTime: 100);

        // Assert
        milliseconds.Should().BeApproximately(1600, 0.000001);
    }

    [TestMethod]
    public void WalkMillisecondsInBeatTime_CrossesRedlineInBothDirections()
    {
        // Arrange
        var timing = new Timing(
        [
            CreateRedline(0, 500),
            CreateRedline(2, 250),
        ],
        1.4);

        // Act
        double forward = timing.WalkMillisecondsInBeatTime(0, 1500);
        double backward = timing.WalkMillisecondsInBeatTime(4, -1500);

        // Assert
        forward.Should().BeApproximately(4, 0.000001);
        backward.Should().BeApproximately(0, 0.000001);
    }

    [TestMethod]
    public void WalkBeatsInMillisecondTime_CrossesRedlineInBothDirections()
    {
        // Arrange
        var timing = new Timing(
        [
            CreateRedline(0, 500),
            CreateRedline(1000, 250),
        ],
        1.4);

        // Act
        double forward = timing.WalkBeatsInMillisecondTime(3);
        double backward = timing.WalkBeatsInMillisecondTime(-2, 1250);

        // Assert
        forward.Should().BeApproximately(1250, 0.000001);
        backward.Should().BeApproximately(500, 0.000001);
    }

    [TestMethod]
    public void Resnap_WhenNearestTickCrossesNextRedline_UsesNextRedlineOffset()
    {
        // Arrange
        var timing = new Timing(
        [
            CreateRedline(1000, 500),
            CreateRedline(1490, 250),
        ],
        1.4);

        // Act
        double snapped = timing.Resnap(1489, [new RationalBeatDivisor(4)]);

        // Assert
        snapped.Should().Be(1490);
    }

    [TestMethod]
    public void ResnapBeatTime_WhenNearestTickCrossesNextRedline_UsesNextRedlineOffset()
    {
        // Arrange
        var timing = new Timing(
        [
            CreateRedline(0, 500),
            CreateRedline(0.7, 250),
        ],
        1.4);

        // Act
        double snapped = timing.ResnapBeatTime(0.69, [new RationalBeatDivisor(4)]);

        // Assert
        snapped.Should().BeApproximately(0.7, 0.000001);
    }

    [TestMethod]
    public void CalculateSliderTemporalLength_WithNaNOrOutOfRangeVelocity_UsesSafeVelocityBounds()
    {
        // Arrange
        var timing = new Timing([CreateRedline(0, 500)], 1.4);

        // Act
        double defaultLength = timing.CalculateSliderTemporalLength(0, 140, double.NaN);
        double tooFast = timing.CalculateSliderTemporalLength(0, 140, -5000);
        double tooSlow = timing.CalculateSliderTemporalLength(0, 140, -1);

        // Assert
        defaultLength.Should().BeApproximately(500, 0.000001);
        tooFast.Should().BeApproximately(5000, 0.000001);
        tooSlow.Should().BeApproximately(50, 0.000001);
    }

    private static TimingPoint CreateRedline(double offset, double millisecondsPerBeat)
    {
        return new TimingPoint(
            offset,
            millisecondsPerBeat,
            4,
            SampleSet.Normal,
            0,
            100,
            true,
            false,
            false);
    }
}
