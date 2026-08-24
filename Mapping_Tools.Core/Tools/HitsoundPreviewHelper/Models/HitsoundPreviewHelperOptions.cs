using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Core.Tools.HitsoundPreviewHelper.Models;

/// <summary>
///     Stores the framework-independent settings for positional hitsound preview.
/// </summary>
public class HitsoundPreviewHelperOptions
{
    /// <summary>Gets or sets how hit objects are selected for preview.</summary>
    public HitsoundPreviewHelperImportMode ImportModeSetting { get; set; } =
        HitsoundPreviewHelperImportMode.Everything;

    /// <summary>
    ///     Gets or sets the legacy osu! time-code query used by Time mode.
    /// </summary>
    public string TimeCode { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the positional rules applied to each selected timeline event.
    /// </summary>
    public List<HitsoundZone> Items { get; set; } = [];
}
