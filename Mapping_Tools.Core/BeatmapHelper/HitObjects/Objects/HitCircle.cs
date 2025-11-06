using System.Collections.Generic;
using Mapping_Tools.Core.BeatmapHelper.Contexts;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff.TimelineObjects;
using Mapping_Tools.Core.BeatmapHelper.Types;

namespace Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;

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