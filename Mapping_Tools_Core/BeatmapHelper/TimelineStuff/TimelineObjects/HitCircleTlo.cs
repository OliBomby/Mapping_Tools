using System;
using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.Objects;

namespace Mapping_Tools_Core.BeatmapHelper.TimelineStuff.TimelineObjects {
    public class HitCircleTlo : TimelineObject {
        public override bool HasHitsound => true;
        public override bool CanCustoms => true;

        public HitCircleTlo(double time, [NotNull] HitSampleInfo hitsounds) : base(time, hitsounds) { }

        public override void HitoundsToOrigin() {
            if (Origin is HitCircle) {
                Hitsounds.CopyTo(Origin.Hitsounds);
            }
            throw new InvalidOperationException($"Invalid origin. Can not assign hitcircle hitsounds to a {Origin}.");
        }
    }
}