using System.Collections.Generic;

namespace Mapping_Tools_Core.BeatmapHelper.Objects {
    public class HitCircle : HitObject {
        public override int TloCount => 1;
        public override List<double> GetAllTloTimes(Timing timing) {
            return new List<double>{StartTime};
        }
    }
}