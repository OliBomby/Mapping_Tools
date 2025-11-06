using System;
using JetBrains.Annotations;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;

namespace Mapping_Tools.Core.BeatmapHelper.TimelineStuff.TimelineObjects;

public class SpinnerTail : TimelineObject {
    public override bool HasHitsound => true;
    public override bool CanCustoms => false;

    public SpinnerTail(double time, [NotNull] HitSampleInfo hitsounds) : base(time, hitsounds) { }

    public override void HitsoundsToOrigin() {
        if (!(Origin is Spinner))
            throw new InvalidOperationException(
                $"Invalid origin. Can not assign spinner tail hitsounds to a {Origin?.GetType()}: {Origin}.");

        Hitsounds.CopyTo(Origin.Hitsounds);
    }
}