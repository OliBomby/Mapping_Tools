namespace Mapping_Tools.Core.Tools.SliderMerger;

/// <summary>
///     Identifies the beatmap objects supplied to Slider Merger.
/// </summary>
public enum SliderMergerImportMode
{
    /// <summary>Uses hit objects selected in the live editor.</summary>
    Selected,

    /// <summary>Uses objects covered by editor bookmarks.</summary>
    Bookmarked,

    /// <summary>Uses objects matched by <see cref="SliderMergerOptions.TimeCode" />.</summary>
    Time,

    /// <summary>Uses every hit object in each input beatmap.</summary>
    Everything,
}

/// <summary>
///     Describes how two converted slider paths are joined.
/// </summary>
public enum SliderMergerConnectionMode
{
    /// <summary>Moves the next path so its start meets the previous path's end.</summary>
    Move,

    /// <summary>Adds a straight Bézier-encoded gap between the two paths.</summary>
    Linear,

    /// <summary>Leaves the converted control polygons to form a Bézier bridge.</summary>
    Bezier,
}

/// <summary>
///     Stores Slider Merger's persisted settings and transformation inputs.
/// </summary>
public class SliderMergerOptions
{
    /// <summary>Gets or sets how hit objects are selected for merging.</summary>
    public SliderMergerImportMode ImportModeSetting { get; set; } = SliderMergerImportMode.Selected;

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
