using JetBrains.Annotations;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;

namespace Mapping_Tools.Core.BeatmapHelper.TimelineStuff.TimelineObjects;

public class SliderHead : SliderNode {
    public SliderHead(double time, [NotNull] HitSampleInfo hitsounds, int nodeIndex) : base(time, hitsounds, nodeIndex) { }
}