using Mapping_Tools.Domain.Beatmaps.Contexts;
using Mapping_Tools.Domain.Beatmaps.Timelines;
using Mapping_Tools.Domain.Beatmaps.Timelines.TimelineObjects;
using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.HitObjects;

public class HoldNote : HitObject, IDuration, IHasTimelineObjects {
    private double _endTime;

    public override double Duration => EndTime - StartTime;

    public override double EndTime => _endTime;

    protected override void DeepCloneAdd(HitObject clonedHitObject) {
    }

    public void SetDuration(double duration) {
        SetEndTime(StartTime + duration);
    }

    public void SetEndTime(double newEndTime) {
        _endTime = newEndTime;
    }

    public override void MoveTime(double deltaTime) {
        _endTime += deltaTime;

        base.MoveTime(deltaTime);
    }

    public IEnumerable<TimelineObject> GetTimelineObjects() {
        var context = new TimelineContext();

        var tlo1 = new HoldNoteHead(StartTime, Hitsounds.Clone()) { Origin = this };
        context.TimelineObjects.Add(tlo1);
        yield return tlo1;

        var tlo2 = new HoldNoteTail(EndTime, new HitSampleInfo()) { Origin = this };
        context.TimelineObjects.Add(tlo2);
        yield return tlo2;

        SetContext(context);
    }
}