using Mapping_Tools.Domain.Beatmaps.Contexts;
using Mapping_Tools.Domain.Beatmaps.TimelineStuff;
using Mapping_Tools.Domain.Beatmaps.TimelineStuff.TimelineObjects;
using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.HitObjects.Objects;

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