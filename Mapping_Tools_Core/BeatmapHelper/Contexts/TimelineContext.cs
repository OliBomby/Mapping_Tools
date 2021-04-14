using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools_Core.BeatmapHelper.Types;

namespace Mapping_Tools_Core.BeatmapHelper.Contexts {
    public class TimelineContext : IContext {
        /// <summary>
        /// The timeline objects associated with this hit object.
        /// </summary>
        [NotNull]
        public List<TimelineObject> TimelineObjects { get; set; }

        public TimelineContext() {
            TimelineObjects = new List<TimelineObject>();
        }

        public TimelineContext(IEnumerable<TimelineObject> timelineObjects) {
            TimelineObjects = timelineObjects.ToList();
        }

        /// <summary>
        /// Update the associated timeline object with new time information.
        /// </summary>
        /// <param name="hitObject">The hit object to align the timeline objects with.</param>
        public void UpdateTimelineObjectTimes<T>(T hitObject) where T : IHasRepeats, IHasStartTime {
            if (hitObject is IHasRepeats hasRepeats && hitObject is IHasStartTime startTime) {
                for (int i = 0; i < TimelineObjects.Count; i++) {
                    double time = Math.Floor(startTime.StartTime + hasRepeats.SpanDuration * i);
                    TimelineObjects[i].Time = time;
                }
            }
        }

        public IContext Copy() {
            return new TimelineContext(TimelineObjects.Select(o => o.Copy()));
        }
    }
}