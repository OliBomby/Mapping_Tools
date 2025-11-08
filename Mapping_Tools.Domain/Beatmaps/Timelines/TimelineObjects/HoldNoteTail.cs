using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Timelines.TimelineObjects;

public class HoldNoteTail(double time, HitSampleInfo hitsounds) : TimelineObject(time, hitsounds) {
    public override bool HasHitsound => false;
    public override bool CanCustoms => false;

    public override void HitsoundsToOrigin(HitSampleInfo hitsounds, bool copyCustoms = false) { }
}