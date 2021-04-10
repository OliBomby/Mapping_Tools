using Mapping_Tools_Core.BeatmapHelper.Types;

namespace Mapping_Tools_Core.BeatmapHelper.Objects {
    public class Spinner : HitObject, IHasDuration {
        public double Duration {
            get => EndTime - StartTime;
            set => EndTime = StartTime + value;
        }

        public double EndTime { get; set; }

        public Spinner() {

        }

        protected override void DeepCloneAdd(HitObject clonedHitObject) { }
    }
}