using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.Tools.SliderMerger.Models;

/// <summary>
///     Stores Slider Merger's persisted settings and transformation inputs.
/// </summary>
public class SliderMergerOptions
{
    /// <summary>Gets or sets how hit objects are selected for merging.</summary>
    public HitObjectSelectionMode ImportModeSetting { get; set; } = HitObjectSelectionMode.Selected;

    /// <summary>Gets or sets the legacy time-code query used by Time mode.</summary>
    public string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets how adjacent slider paths are connected.</summary>
    public SliderMergerConnectionMode ConnectionModeSetting { get; set; } = SliderMergerConnectionMode.Move;

    /// <summary>Gets or sets the maximum allowed distance between adjacent objects in osu! pixels.</summary>
    public double Leniency { get; set; } = 256;

    /// <summary>Gets or sets whether a fully linear result uses the linear path type without red anchors.</summary>
    public bool LinearOnLinear { get; set; }

    /// <summary>Gets or sets whether a slider's playable end, rather than its final anchor, is used for matching.</summary>
    public bool MergeOnSliderEnd { get; set; } = true;
}
