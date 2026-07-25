using System;
using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.BeatmapHelper.SliderPathStuff {
    [TestClass]
    public class SliderPathTests {
        [TestMethod]
        public void GetSliderPath_RepeatedControlPoints_CreatesExpectedSegments() {
            // Arrange
            var slider =
                new HitObject("42,179,300,2,0,B|135:234|219:171|219:171|194:100|194:100|266:53|345:48|405:117,1,499.999952316284");

            // Act
            var sliderPath = slider.GetSliderPath();

            // Assert
            sliderPath.SegmentStarts.Count.Should().Be(3);

            int i = 0;
            foreach (var segmentStart in sliderPath.SegmentStarts) {
                Console.WriteLine(++i + " : " + segmentStart);
            }
        }
    }
}