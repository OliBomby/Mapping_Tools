using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Timelines.TimelineObjects;

public class HitCircleTlo(double time, HitSampleInfo hitsounds) : TimelineObject(time, hitsounds) {
    public override bool HasHitsound => true;
    public override bool CanCustoms => true;

    public override void HitsoundsToOrigin() {
        if (!(Origin is HitCircle))
            throw new InvalidOperationException(
                $"Invalid origin. Can not assign hitcircle hitsounds to a {Origin?.GetType()}: {Origin}.");

        Hitsounds.CopyTo(Origin.Hitsounds);
    }
}