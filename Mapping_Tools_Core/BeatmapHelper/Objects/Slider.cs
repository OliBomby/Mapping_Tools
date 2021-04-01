using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Types;
using Mapping_Tools_Core.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.SliderPathStuff;

namespace Mapping_Tools_Core.BeatmapHelper.Objects {
    public class Slider : HitObject, IHasRepeats, IHasEndTime {
        /// <summary>
        /// Position of slider end. By default is equal to the start position.
        /// </summary>
        public Vector2 EndPos { get; set; }

        /// <summary>
        /// Stacked slider end position of hit object. Must be computed by beatmap.
        /// </summary>
        public Vector2 StackedEndPos { get; set; }

        public PathType SliderType { get; set; }
        [NotNull]
        public List<Vector2> CurvePoints { get; set; }

        public int RepeatCount { get; set; }
        public double PixelLength { get; set; }

        [NotNull]
        public List<HitSampleInfo> EdgeHitsounds { get; set; }

        // Special combined with greenline
        [NotNull]
        public List<TimingPoint> BodyHitsounds { get; set; }
        
        public double Duration { get; set; }

        public double EndTime {
            get => GetEndTime();
            set => SetEndTime(value);
        }

        public double GetEndTime(bool floor = true) {
            var endTime = StartTime + Duration;
            return floor ? Math.Floor(endTime + Precision.DOUBLE_EPSILON) : endTime;
        }

        private void SetEndTime(double value) {
            Duration = value - StartTime;
        }

        public Slider() {
            CurvePoints = new List<Vector2>();
            EdgeHitsounds = new List<HitSampleInfo>();
            BodyHitsounds = new List<TimingPoint>();
        }

        public bool GetSliderExtras() {
            return EdgeHitsounds.Any(o => !o.Equals(Hitsounds)) ||
                   !Hitsounds.Equals(new HitSampleInfo());
        }

        public override List<double> GetAllTloTimes(Timing timing) {
            var times = new List<double>();

            // Adding time for every repeat of the slider
            var sliderTemporalLength = timing.CalculateSliderTemporalLength(StartTime, PixelLength);

            for (var i = 0; i < ((IHasRepeats)this).SpanCount + 1; i++) {
                var time = Math.Floor(StartTime + sliderTemporalLength * i);
                times.Add(time);
            }

            return times;
        }

        public List<string> GetPlayingBodyFilenames(double sliderTickRate, bool includeDefaults = true) {
            if (TimingPoint == null || UnInheritedTimingPoint == null) {
                throw new InvalidOperationException("Slider is not initialized with timing. Can not get the playing body filenames.");
            }

            var samples = new List<string>();
            
            // Get sliderslide hitsounds for every timingpoint in the slider
            if (includeDefaults || TimingPoint.SampleIndex != 0) {
                var firstSampleSet = Hitsounds.SampleSet == SampleSet.Auto ? TimingPoint.SampleSet : Hitsounds.SampleSet;
                samples.Add(GetSliderFilename(firstSampleSet, "slide", TimingPoint.SampleIndex));
                if (Hitsounds.Whistle)
                    samples.Add(GetSliderFilename(firstSampleSet, "whistle", TimingPoint.SampleIndex));
            }

            foreach (var bodyTp in BodyHitsounds)
                if (includeDefaults || bodyTp.SampleIndex != 0) {
                    var sampleSet = Hitsounds.SampleSet == SampleSet.Auto ? bodyTp.SampleSet : Hitsounds.SampleSet;
                    samples.Add(GetSliderFilename(sampleSet, "slide", bodyTp.SampleIndex));
                    if (Hitsounds.Whistle)
                        samples.Add(GetSliderFilename(sampleSet, "whistle", bodyTp.SampleIndex));
                }

            // Add tick samples
            // 10 ms over tick time is tick
            var t = StartTime + UnInheritedTimingPoint.MpB / sliderTickRate;
            while (t + 10 < EndTime) {
                var bodyTp = Timing.GetTimingPointAtTime(t, BodyHitsounds, TimingPoint);
                if (includeDefaults || bodyTp.SampleIndex != 0) {
                    var sampleSet = Hitsounds.SampleSet == SampleSet.Auto ? bodyTp.SampleSet : Hitsounds.SampleSet;
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


        /// <summary>
        /// Retrieves the hitsounds at a particular edge of the repeating slider.
        /// </summary>
        /// <param name="nodeIndex">The edge to attempt to retrieve the hitsounds at.</param>
        /// <returns>The samples at the given edge index, or the slider's default hitsounds if the given edge doesn't exist.</returns>
        public HitSampleInfo GetNodeSamples(int nodeIndex) 
            => nodeIndex < EdgeHitsounds.Count ? EdgeHitsounds[nodeIndex] : Hitsounds;

        public override void ResetHitsounds() {
            for (int i = 0; i < EdgeHitsounds.Count; i++) {
                EdgeHitsounds[i] = new HitSampleInfo();
            }

            BodyHitsounds.Clear();

            base.ResetHitsounds();
        }

        public override void MoveTime(double deltaTime) {
            base.MoveTime(deltaTime);

            BodyHitsounds.RemoveAll(s => s.Offset >= EndTime || s.Offset <= StartTime);
        }

        /// <summary>
        /// Changes the actual end time of the slider by changing the <see cref="PixelLength"/>.
        /// </summary>
        /// <param name="timing">The timing to recalculate the new pixel length with.</param>
        /// <param name="deltaTime">The time in milliseconds to offset the end time by.</param>
        public void MoveEndTime(Timing timing, double deltaTime) {
            ChangeDuration(timing, deltaTime / ((IHasRepeats)this).SpanCount);
        }

        /// <summary>
        /// Changes the actual duration of the slider by changing the <see cref="PixelLength"/>.
        /// </summary>
        /// <param name="timing">The timing to recalculate the new pixel length with.</param>
        /// <param name="deltaDuration">The time in milliseconds to offset the duration by.</param>
        public void ChangeDuration(Timing timing, double deltaDuration) {
            TimingPoint redline = UnInheritedTimingPoint;
            double sv = SliderVelocity;
            if (redline == null) {
                // Slider is probably not initialized with a timing
                redline = timing.GetRedlineAtTime(StartTime);
                sv = timing.GetSvAtTime(StartTime);
            }

            var deltaLength = -10000 * timing.SliderMultiplier * deltaDuration /
                              (redline.MpB *
                               (double.IsNaN(sv) ? -100 : sv));

            // Change
            PixelLength += deltaLength; // Change the pixel length to match the new time
            Duration += deltaDuration;

            // Move body objects
            UpdateTimelineObjectTimes();

            BodyHitsounds.RemoveAll(s => s.Offset >= EndTime);
        }

        /// <summary>
        /// Calculates the <see cref="Duration"/> accurate to the timing and pixellength.
        /// </summary>
        /// <param name="timing">The timing to calculate the duration with.</param>
        /// <param name="useOwnSv">Whether to use the SV of a greenline in the timing or use the SliderVelocity of this object.</param>
        public void CalculateSliderDuration(Timing timing, bool useOwnSv) {
            if (double.IsNaN(PixelLength) || PixelLength < 0 || CurvePoints.All(o => o == Pos)) {
                Duration = 0;
            } else {
                Duration = useOwnSv
                    ? timing.CalculateSliderTemporalLength(StartTime, PixelLength, SliderVelocity)
                    : timing.CalculateSliderTemporalLength(StartTime, PixelLength);
            }
        }

        /// <summary>
        /// Calculates the <see cref="EndPos"/> for sliders.
        /// </summary>
        public void CalculateEndPosition() {
            EndPos = GetSliderPath().PositionAt(1);
        }

        public override void Move(Vector2 delta) {
            base.Move(delta);
            for (var i = 0; i < CurvePoints.Count; i++) CurvePoints[i] = CurvePoints[i] + delta;
        }

        public override void Transform(Matrix2 mat) {
            base.Transform(mat);
            for (var i = 0; i < CurvePoints.Count; i++) CurvePoints[i] = Matrix2.Mult(mat, CurvePoints[i]);
        }

        public SliderPath GetSliderPath(bool fullLength = false) {
            return fullLength
                ? new SliderPath(SliderType, GetAllCurvePoints().ToArray())
                : new SliderPath(SliderType, GetAllCurvePoints().ToArray(), PixelLength);
        }

        public void SetSliderPath(SliderPath sliderPath) {
            var controlPoints = sliderPath.ControlPoints;
            SetAllCurvePoints(controlPoints);
            SliderType = sliderPath.Type;
            PixelLength = sliderPath.Distance;
        }

        public List<Vector2> GetAllCurvePoints() {
            var controlPoints = new List<Vector2> { Pos };
            controlPoints.AddRange(CurvePoints);
            return controlPoints;
        }

        public void SetAllCurvePoints(List<Vector2> controlPoints) {
            Pos = controlPoints.First();
            CurvePoints = controlPoints.GetRange(1, controlPoints.Count - 1);
        }

        /// <summary>
        /// Detects a failure in the slider path algorithm causing a slider to become invisible.
        /// </summary>
        /// <returns></returns>
        public bool IsInvisible() {
            return PixelLength != 0 && PixelLength <= 0.0001 ||
                   double.IsNaN(PixelLength) ||
                   CurvePoints.All(o => o == Pos);
        }

        protected override void DeepCloneAdd(HitObject clonedHitObject) {
            var slider = (Slider) clonedHitObject;

            slider.CurvePoints.AddRange(CurvePoints);
            slider.EdgeHitsounds.AddRange(EdgeHitsounds.Select(o => o.Clone()));
            slider.BodyHitsounds.AddRange(BodyHitsounds.Select(o => o.Copy()));
        }
    }
}