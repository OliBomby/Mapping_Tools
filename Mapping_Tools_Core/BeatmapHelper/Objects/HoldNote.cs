using Mapping_Tools_Core.BeatmapHelper.Types;

namespace Mapping_Tools_Core.BeatmapHelper.Objects {
    public class HoldNote : HitObject, IHasDuration {
        public double Duration {
            get => EndTime - StartTime;
            set => EndTime = StartTime + value;
        }

        public double EndTime { get; set; }

        public HoldNote() {

        }

        protected override void DeepCloneAdd(HitObject clonedHitObject) { }
    }
}