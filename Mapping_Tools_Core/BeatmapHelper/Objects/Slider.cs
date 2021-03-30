using System;
using System.Collections.Generic;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools_Core.BeatmapHelper.Types;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.BeatmapHelper.Objects {
    public class Slider : HitObject, IHasDuration, IHasEndTime {
        /// <summary>
        /// Position of slider end. By default is equal to the start position.
        /// </summary>
        public Vector2 EndPos { get; set; }

        /// <summary>
        /// Stacked slider end position of hit object. Must be computed by beatmap.
        /// </summary>
        public Vector2 StackedEndPos { get; set; }

        public PathType SliderType { get; set; }
        public List<Vector2> CurvePoints { get; set; }

        public int Repeat { get; set; }
        public double PixelLength { get; set; }
        public List<int> EdgeHitsounds { get; set; }
        public List<SampleSet> EdgeSampleSets { get; set; }
        public List<SampleSet> EdgeAdditionSets { get; set; }

        public bool SliderExtras => GetSliderExtras();

        public override int TloCount => Repeat + 1;

        // Special combined with greenline
        public List<TimingPoint> BodyHitsounds = new List<TimingPoint>();


        public double RepeatDuration { get; set; }

        public double Duration {
            get => RepeatDuration * Repeat; 
            set => RepeatDuration = Repeat == 0 ? 0 : value / Repeat;
        }

        public double EndTime {
            get => GetEndTime();
            set => SetEndTime(value);
        }

        public double GetEndTime(bool floor = true) {
            var endTime = StartTime + Duration;
            return floor ? Math.Floor(endTime + Precision.DOUBLE_EPSILON) : endTime;
        }

        private void SetEndTime(double value) {
            RepeatDuration = Duration = value - StartTime;
        }

        public override List<double> GetAllTloTimes(Timing timing) {
            var times = new List<double>();

            // Adding time for every repeat of the slider
            var sliderTemporalLength = timing.CalculateSliderTemporalLength(Time, PixelLength);

            for (var i = 0; i < TloCount; i++) {
                var time = Math.Floor(StartTime + sliderTemporalLength * i);
                times.Add(time);
            }

            return times;
        }

        public List<string> GetPlayingBodyFilenames(double sliderTickRate, bool includeDefaults = true) {
            var samples = new List<string>();
            
            // Get sliderslide hitsounds for every timingpoint in the slider
            if (includeDefaults || TimingPoint.SampleIndex != 0) {
                var firstSampleSet = SampleSet == SampleSet.Auto ? TimingPoint.SampleSet : SampleSet;
                samples.Add(GetSliderFilename(firstSampleSet, "slide", TimingPoint.SampleIndex));
                if (Whistle)
                    samples.Add(GetSliderFilename(firstSampleSet, "whistle", TimingPoint.SampleIndex));
            }

            foreach (var bodyTp in BodyHitsounds)
                if (includeDefaults || bodyTp.SampleIndex != 0) {
                    var sampleSet = SampleSet == SampleSet.Auto ? bodyTp.SampleSet : SampleSet;
                    samples.Add(GetSliderFilename(sampleSet, "slide", bodyTp.SampleIndex));
                    if (Whistle)
                        samples.Add(GetSliderFilename(sampleSet, "whistle", bodyTp.SampleIndex));
                }

            // Add tick samples
            // 10 ms over tick time is tick
            var t = StartTime + UnInheritedTimingPoint.MpB / sliderTickRate;
            while (t + 10 < EndTime) {
                var bodyTp = Timing.GetTimingPointAtTime(t, BodyHitsounds, TimingPoint);
                if (includeDefaults || bodyTp.SampleIndex != 0) {
                    var sampleSet = SampleSet == SampleSet.Auto ? bodyTp.SampleSet : SampleSet;
                    samples.Add(GetSliderFilename(sampleSet, "tick", bodyTp.SampleIndex));
                }

                t += UnInheritedTimingPoint.MpB / sliderTickRate;
            }

            return samples;
        }

        private string GetSliderFilename(SampleSet sampleSet, string sampleName, int index) {
            if (index == 0) return $"{sampleSet.ToString().ToLower()}-slider{sampleName}-default.wav";
            if (index == 1) return $"{sampleSet.ToString().ToLower()}-slider{sampleName}.wav";
            return $"{sampleSet.ToString().ToLower()}-slider{sampleName}{index}.wav";
        }
    }
}