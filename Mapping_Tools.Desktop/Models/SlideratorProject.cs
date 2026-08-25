using ApplicationSlideratorProject = Mapping_Tools.Application.Tools.Sliderator.Models.SlideratorProject;

namespace Mapping_Tools.Desktop.Models;

/// <summary>Stores Sliderator presentation state alongside the Application run inputs.</summary>
public sealed class SlideratorProject : ApplicationSlideratorProject
{
    /// <summary>Gets or sets whether red slider-path anchors are shown in the preview.</summary>
    public bool ShowRedAnchors { get; set; }

    /// <summary>Gets or sets whether graph anchors are shown in the preview.</summary>
    public bool ShowGraphAnchors { get; set; }

    /// <summary>Gets or sets whether the manually entered velocity is used.</summary>
    public bool ManualVelocity { get; set; }

    /// <summary>Gets or sets the distance travelled by the current graph.</summary>
    public double DistanceTraveled { get; set; }
}
