namespace Mapping_Tools.Core.Tools.SliderCompletionator.Models;

/// <summary>
///     Identifies the beatmap objects supplied to Slider Completionator.
/// </summary>
public enum SliderCompletionatorImportMode
{
    /// <summary>Uses hit objects selected in the live editor.</summary>
    Selected,

    /// <summary>Uses objects covered by editor bookmarks.</summary>
    Bookmarked,

    /// <summary>Uses objects matched by <see cref="SliderCompletionatorOptions.TimeCode" />.</summary>
    Time,

    /// <summary>Uses every hit object in each input beatmap.</summary>
    Everything,
}

