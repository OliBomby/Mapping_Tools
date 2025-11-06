using System;
using JetBrains.Annotations;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;

namespace Mapping_Tools.Core.BeatmapHelper.TimelineStuff.TimelineObjects;

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