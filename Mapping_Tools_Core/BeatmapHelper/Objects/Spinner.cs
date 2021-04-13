using Mapping_Tools_Core.BeatmapHelper.Types;

namespace Mapping_Tools_Core.BeatmapHelper.Objects {
    public class Spinner : HitObject, IDuration {
        private double endTime;

        public override double Duration => EndTime - StartTime;

        public override double EndTime => endTime;

        // Spinners ignore combo skip
        public override int ComboSkip => 0;

        public override int ComboIncrement => 0;

        public Spinner() {

        }

        protected override void DeepCloneAdd(HitObject clonedHitObject) { }
        public void SetDuration(double duration) {
            SetEndTime(StartTime + duration);
        }

        public void SetEndTime(double endTime) {
            this.endTime = endTime;
        }
    }
}