using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.TimelineStuff.TimelineObjects;

public class SpinnerHead : TimelineObject {
    public override bool HasHitsound => false;
    public override bool CanCustoms => false;

    public SpinnerHead(double time, [NotNull] HitSampleInfo hitsounds) : base(time, hitsounds) { }

    public override void HitsoundsToOrigin() { }
}