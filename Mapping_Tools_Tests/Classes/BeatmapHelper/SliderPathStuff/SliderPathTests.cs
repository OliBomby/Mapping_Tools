using System;
using Mapping_Tools.Classes.BeatmapHelper;
using NUnit.Framework;

namespace Mapping_Tools_Tests.Classes.BeatmapHelper.SliderPathStuff {
    [TestFixture]
    public class SliderPathTests {
        [Test]
        public void SliderPathSegmentsTest() {
            var slider =
                new HitObject("42,179,300,2,0,B|135:234|219:171|219:171|194:100|194:100|266:53|345:48|405:117,1,499.999952316284");

            var sliderPath = slider.GetSliderPath();

            Assert.That(sliderPath.SegmentStarts.Count, Is.EqualTo(3));

            int i = 0;
            foreach (var segmentStart in sliderPath.SegmentStarts) {
                Console.WriteLine(++i + " : " + segmentStart);
            }
        }
    }
}