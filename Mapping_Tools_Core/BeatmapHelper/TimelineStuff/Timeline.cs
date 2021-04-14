using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.Contexts;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.BeatmapHelper.Types;

namespace Mapping_Tools_Core.BeatmapHelper.TimelineStuff {
    /// <summary>
    /// A timeline with timeline objects.
    /// </summary>
    public class Timeline {
        /// <summary>
        /// All timeline objects of the timeline.
        /// </summary>
        [NotNull]
        public List<TimelineObject> TimelineObjects { get; set; }

        public Timeline(IEnumerable<HitObject> hitObjects) {
            var timelineObjects = hitObjects.Where(ho => ho is IHasTimelineObjects)
                .SelectMany(ho => ((IHasTimelineObjects) ho).GetTimelineObjects());

            // Sort the TimeLineObjects by time
            TimelineObjects = timelineObjects.OrderBy(o => o.Time).ToList();
        }

        public Timeline([NotNull]List<TimelineObject> timeLineObjects) {
            TimelineObjects = timeLineObjects;
        }

        /// <summary>
        /// Gets all the timeline objects in the time range inclusive.
        /// </summary>
        /// <param name="start">The start time in milliseconds.</param>
        /// <param name="end">The end time in milliseconds.</param>
        /// <returns></returns>
        public List<TimelineObject> GetTimeLineObjectsInRange(double start, double end) {
            return TimelineObjects.FindAll(o => o.Time >= start && o.Time <= end);
        }

        /// <summary>
        /// Adds new <see cref="TimingContext"/> to each timeline object.
        /// </summary>
        /// <param name="timing">The timing to get timing from.</param>
        public void GiveTimingContext(Timing timing) {
            foreach (TimelineObject tlo in TimelineObjects) {
                tlo.SetContext(new TimingContext(timing.GlobalSliderMultiplier,
                    timing.GetSvAtTime(tlo.Time),
                    timing.GetTimingPointAtTime(tlo.Time),
                    timing.GetTimingPointAtTime(tlo.Time + 5),
                    timing.GetRedlineAtTime(tlo.Time),
                    Array.Empty<TimingPoint>()));
            }
        }

        /// <summary>
        /// Finds the timeline objects that is closest to the given time and matches the predicate.
        /// </summary>
        /// <param name="time">The target time.</param>
        /// <param name="predicate">The predicate to filter timeline objects.</param>
        /// <returns></returns>
        public TimelineObject GetNearestTlo(double time, Func<TimelineObject, bool> predicate) {
            if (TimelineObjects.Count == 0) {
                return null;
            }

            TimelineObject closest = null;
            double closestDist = double.PositiveInfinity;
            foreach (TimelineObject tlo in TimelineObjects) {
                double dist = Math.Abs(tlo.Time - time);

                if (dist <= closestDist) {
                    if (!predicate(tlo))
                        continue;

                    closest = tlo;
                    closestDist = dist;
                }
            }

            return closest;
        }
    }
}
