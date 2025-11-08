using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Timelines.TimelineObjects;

/// <summary>
/// One edge of a slider. Can be slider head, slider repeat, or slider end.
/// </summary>
public class SliderNode(double time, HitSampleInfo hitsounds, int nodeIndex) : TimelineObject(time, hitsounds) {
    public override bool HasHitsound => true;
    public override bool CanCustoms => false;

    /// <summary>
    /// The index of the edge this node represents.
    /// </summary>
    public int NodeIndex { get; set; } = nodeIndex;

    public override void HitsoundsToOrigin() {
        if (!(Origin is Slider slider))
            throw new InvalidOperationException(
                $"Invalid origin. Can not assign slider node hitsounds to a {Origin?.GetType()}: {Origin}.");

        Hitsounds.CopyTo(slider.GetNodeSamples(NodeIndex));
    }
}