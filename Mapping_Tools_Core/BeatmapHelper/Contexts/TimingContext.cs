using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;

namespace Mapping_Tools_Core.BeatmapHelper.Contexts {
    /// <summary>
    /// Special info for <see cref="HitObject"/> combined with <see cref="Timing"/>.
    /// </summary>
    public class TimingContext : IContext {
        /// <summary>
        /// The global slider velocity of the beatmap.
        /// Measured in 100 pixels per beat.
        /// </summary>
        public double GlobalSliderVelocity { get; set; }

        /// <summary>
        /// The greenline slider velocity multiplier at the start time of this hit object.
        /// </summary>
        public double SliderVelocity { get; set; }

        /// <summary>
        /// The timing point active at the start time of this hit object.
        /// Usefull for determining slider velocity.
        /// </summary>
        [NotNull]
        public TimingPoint TimingPoint { get; set; }

        /// <summary>
        /// The timing point active 5 milliseconds after the start time of this hit object.
        /// This is the timing point determining the hitsounds for this hit object at the start time.
        /// </summary>
        [NotNull]
        public TimingPoint HitsoundTimingPoint { get; set; }

        /// <summary>
        /// The uninherited timing point active at the start time of this hit object.
        /// Determines the BPM at the start time of this hit object.
        /// </summary>
        [NotNull]
        public TimingPoint UninheritedTimingPoint { get; set; }

        /// <summary>
        /// Other timing points that occur during the duration of the hit object.
        /// </summary>
        [NotNull]
        public List<TimingPoint> BodyHitsounds { get; set; }

        /// <summary>
        /// Creates a new timing context. Automatically copies the timing points.
        /// </summary>
        public TimingContext(double globalSliderVelocity, double sliderVelocity, TimingPoint timingPoint,
            TimingPoint hitsoundTimingPoint, TimingPoint unInheritedTimingPoint, IEnumerable<TimingPoint> bodyHitsounds) {
            GlobalSliderVelocity = globalSliderVelocity;
            SliderVelocity = sliderVelocity;
            TimingPoint = timingPoint.Copy();
            HitsoundTimingPoint = hitsoundTimingPoint;
            UninheritedTimingPoint = unInheritedTimingPoint.Copy();
            BodyHitsounds = bodyHitsounds.Select(o => o.Copy()).ToList();
        }

        public IContext Copy() {
            return new TimingContext(GlobalSliderVelocity, SliderVelocity, TimingPoint, HitsoundTimingPoint, UninheritedTimingPoint, BodyHitsounds);
        }
    }
}