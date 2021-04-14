using System;
using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.Objects;

namespace Mapping_Tools_Core.BeatmapHelper.TimelineStuff.TimelineObjects {
    public class HoldNoteHead : TimelineObject {
        public override bool HasHitsound => true;
        public override bool CanCustoms => false;

        public HoldNoteHead(double time, [NotNull] HitSampleInfo hitsounds) : base(time, hitsounds) { }

        public override void HitoundsToOrigin() {
            if (Origin is HoldNote) {
                Hitsounds.CopyTo(Origin.Hitsounds);
            }
            throw new InvalidOperationException($"Invalid origin. Can not assign hold note head hitsounds to a {Origin}.");
        }
    }
}