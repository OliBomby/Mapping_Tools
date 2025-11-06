using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.Beatmaps.HitObjects.Objects;

namespace Mapping_Tools.Domain.Beatmaps.TimelineStuff.TimelineObjects;

public class HoldNoteHead : TimelineObject {
    public override bool HasHitsound => true;
    public override bool CanCustoms => false;

    public HoldNoteHead(double time, [NotNull] HitSampleInfo hitsounds) : base(time, hitsounds) { }

    public override void HitsoundsToOrigin() {
        if (!(Origin is HoldNote))
            throw new InvalidOperationException(
                $"Invalid origin. Can not assign hold note head hitsounds to a {Origin?.GetType()}: {Origin}.");

        Hitsounds.CopyTo(Origin.Hitsounds);
    }
}