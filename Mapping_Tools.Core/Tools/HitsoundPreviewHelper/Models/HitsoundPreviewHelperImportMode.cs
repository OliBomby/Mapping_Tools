namespace Mapping_Tools.Core.Tools.HitsoundPreviewHelper.Models;

/// <summary>
///     Identifies which beatmap objects supply hitsound-preview events.
/// </summary>
public enum HitsoundPreviewHelperImportMode
{
    /// <summary>Uses hit objects selected in the live editor.</summary>
    Selected,

    /// <summary>Uses objects covered by editor bookmarks.</summary>
    Bookmarked,

    /// <summary>Uses objects matched by <see cref="HitsoundPreviewHelperOptions.TimeCode" />.</summary>
    Time,

    /// <summary>Uses every hit object in each input beatmap.</summary>
    Everything,
}

