using Mapping_Tools_Core.BeatmapHelper.Types;

namespace Mapping_Tools_Core.BeatmapHelper.Objects {
    public class Spinner : HitObject, IHasDuration {
        public double Duration {
            get => EndTime - StartTime;
            set => EndTime = StartTime + value;
        }

        public double EndTime { get; set; }

        // Spinners ignore combo skip
        public override int ComboSkip => 0;

        public Spinner() {

        }

        protected override void DeepCloneAdd(HitObject clonedHitObject) { }
    }
}