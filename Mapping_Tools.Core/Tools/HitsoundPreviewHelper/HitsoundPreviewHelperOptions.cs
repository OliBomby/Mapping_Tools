using Mapping_Tools.Core.Classes.HitsoundStuff;

namespace Mapping_Tools.Core.Tools.HitsoundPreviewHelper;

/// <summary>
/// Identifies which beatmap objects supply hitsound-preview events.
/// </summary>
public enum HitsoundPreviewHelperImportMode
{
    /// <summary>Uses hit objects selected in the live editor.</summary>
    Selected,

    /// <summary>Uses objects covered by editor bookmarks.</summary>
    Bookmarked,

    /// <summary>Uses objects matched by <see cref="HitsoundPreviewHelperOptions.TimeCode"/>.</summary>
    Time,

    /// <summary>Uses every hit object in each input beatmap.</summary>
    Everything
}

/// <summary>
/// Stores the framework-independent settings for positional hitsound preview.
/// </summary>
public class HitsoundPreviewHelperOptions
{
    /// <summary>Gets or sets how hit objects are selected for preview.</summary>
    public HitsoundPreviewHelperImportMode ImportModeSetting { get; set; } =
        HitsoundPreviewHelperImportMode.Everything;

    /// <summary>
    /// Gets or sets the legacy osu! time-code query used by Time mode.
    /// </summary>
    public string TimeCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the positional rules applied to each selected timeline event.
    /// </summary>
    public List<HitsoundZone> Items { get; set; } = [];
}
