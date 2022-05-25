using System;
using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.BeatmapHelper.SliderPathStuff {
    [TestClass]
    public class SliderPathTests {
        [TestMethod]
        public void SliderPathSegmentsTest() {
            var slider =
                new HitObject("42,179,300,2,0,B|135:234|219:171|219:171|194:100|194:100|266:53|345:48|405:117,1,499.999952316284");

            var sliderPath = slider.GetSliderPath();

            Assert.AreEqual(3, sliderPath.SegmentStarts.Count);

            int i = 0;
            foreach (var segmentStart in sliderPath.SegmentStarts) {
                Console.WriteLine(++i + " : " + segmentStart);
            }
        }
    }
}