using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class TimelineTests
{
    [TestMethod]
    public void CircleAndSpinner_CreateChronologicalTimelineObjects()
    {
        // Arrange
        var spinner = new HitObject("256,192,500,8,4,1500,0:0:0:0:");
        var circle = new HitObject("256,192,1000,1,2,0:0:0:0:");

        // Act
        var timeline = new Timeline(new List<HitObject> { circle, spinner }, new Timing(1.4));

        // Assert
        timeline.TimelineObjects.Select(x => x.Time).ToArray().Should().Equal(500d, 1000d, 1500d);
        timeline.TimelineObjects[0].HasHitsound.Should().BeFalse();
        timeline.TimelineObjects[1].Whistle.Should().BeTrue();
        timeline.TimelineObjects[2].Finish.Should().BeTrue();
    }

    [TestMethod]
    public void FileName_UsesModeSetHitsoundAndIndex()
    {
        // Arrange
        // Act
        string filename = TimelineObject.GetFileName(SampleSet.Drum, Hitsound.Clap, 3, GameMode.Taiko);

        // Assert
        filename.Should().Be("taiko-drum-hitclap3");
    }
}
