using System.Collections.Generic;

namespace Mapping_Tools_Core.BeatmapHelper.Objects {
    public class HitCircle : HitObject {
        public override List<double> GetAllTloTimes(Timing timing) {
            return new List<double>{StartTime};
        }

        protected override void DeepCloneAdd(HitObject clonedHitObject) { }
    }
}