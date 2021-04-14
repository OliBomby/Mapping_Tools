using System.Collections.Generic;
using Mapping_Tools_Core.BeatmapHelper.Contexts;
using Mapping_Tools_Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools_Core.BeatmapHelper.TimelineStuff.TimelineObjects;
using Mapping_Tools_Core.BeatmapHelper.Types;

namespace Mapping_Tools_Core.BeatmapHelper.Objects {
    public class Spinner : HitObject, IDuration, IHasTimelineObjects {
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

        public void SetEndTime(double newEndTime) {
            endTime = newEndTime;
        }

        public IEnumerable<TimelineObject> GetTimelineObjects() {
            var context = new TimelineContext();

            var tlo1 = new SpinnerHead(StartTime, new HitSampleInfo()) { Origin = this };
            context.TimelineObjects.Add(tlo1);
            yield return tlo1;

            var tlo2 = new SpinnerTail(EndTime, Hitsounds.Clone()) { Origin = this };
            context.TimelineObjects.Add(tlo2);
            yield return tlo2;

            SetContext(context);
        }
    }
}