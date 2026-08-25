using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.SliderMerger.Models;

namespace Mapping_Tools.Application.Tools.SliderMerger;

/// <summary>Persists the complete Slider Merger form using the legacy property names.</summary>
public class SliderMergerServiceOptions : SliderMergerEngineOptions
{
    /// <summary>Gets or sets how hit objects are selected for merging.</summary>
    public HitObjectSelectionMode ImportModeSetting { get; set; } = HitObjectSelectionMode.Selected;

    /// <summary>Gets or sets the legacy time-code query used by Time mode.</summary>
    public string TimeCode { get; set; } = string.Empty;

}
