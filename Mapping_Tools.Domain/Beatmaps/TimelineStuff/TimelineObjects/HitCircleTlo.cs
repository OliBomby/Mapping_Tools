using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.Beatmaps.HitObjects.Objects;

namespace Mapping_Tools.Domain.Beatmaps.TimelineStuff.TimelineObjects;

public class HitCircleTlo : TimelineObject {
    public override bool HasHitsound => true;
    public override bool CanCustoms => true;

    public HitCircleTlo(double time, [NotNull] HitSampleInfo hitsounds) : base(time, hitsounds) { }

    public override void HitsoundsToOrigin() {
        if (!(Origin is HitCircle))
            throw new InvalidOperationException(
                $"Invalid origin. Can not assign hitcircle hitsounds to a {Origin?.GetType()}: {Origin}.");

        Hitsounds.CopyTo(Origin.Hitsounds);
    }
}