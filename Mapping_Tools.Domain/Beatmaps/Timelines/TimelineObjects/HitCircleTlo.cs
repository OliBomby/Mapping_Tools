using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Timelines.TimelineObjects;

public class HitCircleTlo(double time, HitSampleInfo hitsounds) : TimelineObject(time, hitsounds) {
    public override bool HasHitsound => true;
    public override bool CanCustoms => true;

    public override void HitsoundsToOrigin(HitSampleInfo hitsounds, bool copyCustoms = false) {
        if (Origin is not HitCircle)
            throw new InvalidOperationException(
                $"Invalid origin. Can not assign hit circle hitsounds to a {Origin?.GetType()}: {Origin}.");

        hitsounds.CopyTo(Origin.Hitsounds);
    }
}