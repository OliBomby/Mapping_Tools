using Mapping_Tools.Domain.Beatmaps.TimelineStuff;
using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Contexts;

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
    public void UpdateTimelineObjectTimes<T>(T hitObject) where T : IHasStartTime {
        if (hitObject is IHasRepeats hasRepeats) {
            for (int i = 0; i < TimelineObjects.Count; i++) {
                double time = Math.Floor(hitObject.StartTime + hasRepeats.SpanDuration * i);
                TimelineObjects[i].Time = time;
            }
        }
        else if (hitObject is IHasDuration hasDuration) {
            for (int i = 0; i < TimelineObjects.Count; i++) {
                double spanDuration = TimelineObjects.Count > 1 ? hasDuration.Duration / (TimelineObjects.Count - 1) : 0;
                double time =
                    Math.Floor(hitObject.StartTime + spanDuration * i);
                TimelineObjects[i].Time = time;
            }
        }
        else {
            // Offset everything to match the start time
            var offset = hitObject.StartTime - TimelineObjects[0].Time;
            foreach (var tlo in TimelineObjects) {
                tlo.Time += offset;
            }
        }
    }

    public IContext Copy() {
        return new TimelineContext(TimelineObjects.Select(o => o.Copy()));
    }
}