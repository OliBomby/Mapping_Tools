using System.Collections.Generic;
using Mapping_Tools_Core.BeatmapHelper.Contexts;
using Mapping_Tools_Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools_Core.BeatmapHelper.TimelineStuff.TimelineObjects;
using Mapping_Tools_Core.BeatmapHelper.Types;

namespace Mapping_Tools_Core.BeatmapHelper.Objects {
    public class HitCircle : HitObject, IHasTimelineObjects {
        protected override void DeepCloneAdd(HitObject clonedHitObject) { }

        public IEnumerable<TimelineObject> GetTimelineObjects() {
            var context = new TimelineContext();

            var tlo = new HitCircleTlo(StartTime, Hitsounds.Clone()) { Origin = this };
            context.TimelineObjects.Add(tlo);
            yield return tlo;

            SetContext(context);
        }
    }
}