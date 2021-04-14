using System;
using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.Objects;

namespace Mapping_Tools_Core.BeatmapHelper.TimelineStuff.TimelineObjects {
    public class SpinnerTail : TimelineObject {
        public override bool HasHitsound => true;
        public override bool CanCustoms => false;

        public SpinnerTail(double time, [NotNull] HitSampleInfo hitsounds) : base(time, hitsounds) { }

        public override void HitoundsToOrigin() {
            if (Origin is Spinner) {
                Hitsounds.CopyTo(Origin.Hitsounds);
            }
            throw new InvalidOperationException($"Invalid origin. Can not assign spinner tail hitsounds to a {Origin}.");
        }
    }
}