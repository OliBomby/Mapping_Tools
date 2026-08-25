using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.Sliderator.Models;

namespace Mapping_Tools.Application.Tools.Sliderator.Models;

/// <summary>Stores Sliderator's persisted generation settings.</summary>
public sealed class SlideratorProject : SlideratorOptions
{
    /// <summary>Gets or sets the imported object selection mode.</summary>
    public HitObjectSelectionMode ImportModeSetting { get; set; } = HitObjectSelectionMode.Selected;

    /// <summary>Gets or sets the time-code expression used by time selection.</summary>
    public string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets whether red slider-path anchors are shown in the preview.</summary>
    public bool ShowRedAnchors { get; set; }

    /// <summary>Gets or sets whether graph anchors are shown in the preview.</summary>
    public bool ShowGraphAnchors { get; set; }

    /// <summary>Gets or sets whether the manually entered velocity is used.</summary>
    public bool ManualVelocity { get; set; }

    /// <summary>Gets or sets the distance travelled by the current graph.</summary>
    public double DistanceTraveled { get; set; }

}
