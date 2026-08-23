using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

[TestClass]
public class HitObjectTests
{
    [TestMethod]
    public void CircleLine_ParsesAndRoundTrips()
    {
        // Arrange
        // Act
        const string line = "256,192,1000,5,2,2:3:4:75:custom.wav";
        var hitObject = new HitObject(line);

        // Assert
        hitObject.IsCircle.Should().BeTrue();
        hitObject.NewCombo.Should().BeTrue();
        hitObject.Whistle.Should().BeTrue();
        hitObject.SampleSet.Should().Be(SampleSet.Soft);
        hitObject.AdditionSet.Should().Be(SampleSet.Drum);
        hitObject.CustomIndex.Should().Be(4);
        hitObject.SampleVolume.Should().Be(75d);
        hitObject.Filename.Should().Be("custom.wav");
        hitObject.GetLine().Should().Be(line);
    }

    [TestMethod]
    public void SliderLine_ParsesPathEdgesAndRoundTrips()
    {
        // Arrange
        // Act
        const string line = "64,96,1200,6,2,B|128:96|192:128,2,240,2|8|0,1:2|2:3|3:1,1:2:3:60:";
        var hitObject = new HitObject(line);

        // Assert
        hitObject.IsSlider.Should().BeTrue();
        hitObject.SliderType.Should().Be(PathType.Bezier);
        hitObject.Repeat.Should().Be(2);
        hitObject.EdgeHitsounds.Should().Equal(2, 8, 0);
        hitObject.EdgeSampleSets.Should().Equal(SampleSet.Normal, SampleSet.Soft, SampleSet.Drum);
        hitObject.EdgeAdditionSets.Should().Equal(SampleSet.Soft, SampleSet.Drum, SampleSet.Normal);
        hitObject.GetLine().Should().Be(line);
    }

    [TestMethod]
    public void HoldNoteLine_UsesEndTimeFromObjectParams()
    {
        // Arrange
        // Act
        const string line = "128,192,2000,128,0,2500:1:2:3:40:hold.wav";
        var hitObject = new HitObject(line);

        // Assert
        hitObject.IsHoldNote.Should().BeTrue();
        hitObject.EndTime.Should().Be(2500d);
        hitObject.GetLine().Should().Be(line);
    }

    [TestMethod]
    public void Comparer_CanIgnorePositionAndTime()
    {
        // Arrange
        // Act
        var first = new HitObject("64,96,1000,1,0,0:0:0:0:");
        var second = new HitObject("128,192,2000,1,0,0:0:0:0:");

        // Assert
        new HitObjectComparer(false, false).Equals(first, second).Should().BeTrue();
        new HitObjectComparer().Equals(first, second).Should().BeFalse();
    }
}
