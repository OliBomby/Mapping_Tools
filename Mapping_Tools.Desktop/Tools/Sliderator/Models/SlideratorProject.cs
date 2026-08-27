using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Desktop.Tools.Sliderator.Models;

/// <summary>Stores Sliderator presentation state alongside the Application run inputs.</summary>
public sealed class SlideratorProject : SlideratorServiceOptions
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
