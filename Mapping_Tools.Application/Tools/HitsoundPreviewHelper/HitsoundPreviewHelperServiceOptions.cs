using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper.Models;

namespace Mapping_Tools.Application.Tools.HitsoundPreviewHelper;

/// <summary>
///     Persists the complete hitsound-preview form while retaining the legacy
///     <c>Items</c> zone property used by WPF project files.
/// </summary>
public class HitsoundPreviewHelperServiceOptions : HitsoundPreviewHelperEngineOptions
{
    /// <summary>Gets or sets how hit objects are selected for preview.</summary>
    public HitObjectSelectionMode ImportModeSetting { get; set; } = HitObjectSelectionMode.Everything;

    /// <summary>Gets or sets the legacy osu! time-code query used by Time mode.</summary>
    public string TimeCode { get; set; } = string.Empty;

}
