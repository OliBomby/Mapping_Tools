namespace Mapping_Tools.Core.Tools.PropertyTransformer;

/// <summary>
///     Stores the multipliers, offsets, clipping, and filters used by Property Transformer.
/// </summary>
public class PropertyTransformerEngineOptions
{
    /// <summary>Gets or sets the timing-point offset multiplier.</summary>
    public double TimingpointOffsetMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the timing-point offset addition in milliseconds.</summary>
    public double TimingpointOffsetOffset { get; set; }

    /// <summary>Gets or sets the uninherited timing-point BPM multiplier.</summary>
    public double TimingpointBpmMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the uninherited timing-point BPM addition.</summary>
    public double TimingpointBpmOffset { get; set; }

    /// <summary>Gets or sets the inherited timing-point slider-velocity multiplier.</summary>
    public double TimingpointSvMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the inherited timing-point slider-velocity addition.</summary>
    public double TimingpointSvOffset { get; set; }

    /// <summary>Gets or sets the timing-point custom-index multiplier.</summary>
    public double TimingpointIndexMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the timing-point custom-index addition.</summary>
    public double TimingpointIndexOffset { get; set; }

    /// <summary>Gets or sets the timing-point volume multiplier.</summary>
    public double TimingpointVolumeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the timing-point volume addition.</summary>
    public double TimingpointVolumeOffset { get; set; }

    /// <summary>Gets or sets the hit-object time multiplier.</summary>
    public double HitObjectTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the hit-object time addition in milliseconds.</summary>
    public double HitObjectTimeOffset { get; set; }

    /// <summary>Gets or sets the hit-object volume multiplier.</summary>
    public double HitObjectVolumeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the hit-object volume addition.</summary>
    public double HitObjectVolumeOffset { get; set; }

    /// <summary>Gets or sets the bookmark time multiplier.</summary>
    public double BookmarkTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the bookmark time addition in milliseconds.</summary>
    public double BookmarkTimeOffset { get; set; }

    /// <summary>Gets or sets the storyboard event time multiplier.</summary>
    public double SbEventTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the storyboard event time addition in milliseconds.</summary>
    public double SbEventTimeOffset { get; set; }

    /// <summary>Gets or sets the storyboard sample time multiplier.</summary>
    public double SbSampleTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the storyboard sample time addition in milliseconds.</summary>
    public double SbSampleTimeOffset { get; set; }

    /// <summary>Gets or sets the storyboard sample volume multiplier.</summary>
    public double SbSampleVolumeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the storyboard sample volume addition.</summary>
    public double SbSampleVolumeOffset { get; set; }

    /// <summary>Gets or sets the break time multiplier.</summary>
    public double BreakTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the break time addition in milliseconds.</summary>
    public double BreakTimeOffset { get; set; }

    /// <summary>Gets or sets the video start-time multiplier.</summary>
    public double VideoTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the video start-time addition in milliseconds.</summary>
    public double VideoTimeOffset { get; set; }

    /// <summary>Gets or sets the preview-point time multiplier.</summary>
    public double PreviewTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the preview-point time addition in milliseconds.</summary>
    public double PreviewTimeOffset { get; set; }

    /// <summary>Gets or sets whether transformed values are constrained to their legacy bounds.</summary>
    public bool ClipProperties { get; set; }

    /// <summary>Gets or sets whether match, mismatch, and time-range filters are active.</summary>
    public bool EnableFilters { get; set; }

    /// <summary>Gets or sets the values allowed by the match filter.</summary>
    public double[] MatchFilter { get; set; } = [];

    /// <summary>Gets or sets the values rejected by the mismatch filter.</summary>
    public double[] UnmatchFilter { get; set; } = [];

    /// <summary>Gets or sets the inclusive lower time filter, or <c>-1</c> for no lower bound.</summary>
    public double MinTimeFilter { get; set; } = -1;

    /// <summary>Gets or sets the inclusive upper time filter, or <c>-1</c> for no upper bound.</summary>
    public double MaxTimeFilter { get; set; } = -1;

}
