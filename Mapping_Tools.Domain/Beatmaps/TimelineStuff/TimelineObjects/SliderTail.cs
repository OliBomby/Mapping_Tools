using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.TimelineStuff.TimelineObjects;

public class SliderTail : SliderNode {
    public SliderTail(double time, [NotNull] HitSampleInfo hitsounds, int nodeIndex) : base(time, hitsounds, nodeIndex) { }
}