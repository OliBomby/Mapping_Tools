using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.SliderCompletionator.Models;

namespace Mapping_Tools.Application.Tools.SliderCompletionator;

/// <summary>
///     Persists the complete Slider Completionator form while retaining the legacy
///     project property names used by WPF JSON files.
/// </summary>
public sealed class SliderCompletionatorProject : SliderCompletionatorOptions
{
    /// <summary>Gets or sets how hit objects are selected for completion.</summary>
    public HitObjectSelectionMode ImportModeSetting { get; set; } = HitObjectSelectionMode.Selected;

    /// <summary>Gets or sets the legacy time-code query used by Time mode.</summary>
    public string TimeCode { get; set; } = string.Empty;

}
