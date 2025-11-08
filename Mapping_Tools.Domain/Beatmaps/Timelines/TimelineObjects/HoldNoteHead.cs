using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Timelines.TimelineObjects;

public class HoldNoteHead(double time, HitSampleInfo hitsounds) : TimelineObject(time, hitsounds) {
    public override bool HasHitsound => true;
    public override bool CanCustoms => false;

    public override void HitsoundsToOrigin(HitSampleInfo hitsounds, bool copyCustoms = false) {
        if (Origin is not HoldNote)
            throw new InvalidOperationException(
                $"Invalid origin. Can not assign hold note head hitsounds to a {Origin?.GetType()}: {Origin}.");

        hitsounds.CopyTo(Origin.Hitsounds);
    }
}