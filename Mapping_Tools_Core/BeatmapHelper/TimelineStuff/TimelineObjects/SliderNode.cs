using System;
using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.Objects;

namespace Mapping_Tools_Core.BeatmapHelper.TimelineStuff.TimelineObjects {
    /// <summary>
    /// One edge of a slider. Can be slider head, slider repeat, or slider end.
    /// </summary>
    public class SliderNode : TimelineObject {
        public override bool HasHitsound => true;
        public override bool CanCustoms => false;

        /// <summary>
        /// The index of the edge this node represents.
        /// </summary>
        public int NodeIndex { get; set; }

        public SliderNode(double time, [NotNull] HitSampleInfo hitsounds, int nodeIndex) : base(time, hitsounds) {
            NodeIndex = nodeIndex;
        }

        public override void HitoundsToOrigin() {
            if (Origin is Slider slider) {
                Hitsounds.CopyTo(slider.GetNodeSamples(NodeIndex));
            }
            throw new InvalidOperationException($"Invalid origin. Can not assign slider node hitsounds to a {Origin}.");
        }
    }
}