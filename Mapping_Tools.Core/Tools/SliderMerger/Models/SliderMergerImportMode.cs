namespace Mapping_Tools.Core.Tools.SliderMerger.Models;

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

