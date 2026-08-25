using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.Sliderator.Models;

namespace Mapping_Tools.Application.Tools.Sliderator.Models;

/// <summary>Stores Sliderator's persisted generation settings.</summary>
public class SlideratorProject : SlideratorOptions
{
    /// <summary>Gets or sets the imported object selection mode.</summary>
    public HitObjectSelectionMode ImportModeSetting { get; set; } = HitObjectSelectionMode.Selected;

    /// <summary>Gets or sets the time-code expression used by time selection.</summary>
    public string TimeCode { get; set; } = string.Empty;

}
