using Mapping_Tools.Application.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;

/// <summary>Stores the persisted Geometry Dashboard project.</summary>
public sealed class GeometryDashboardProject : GeometryDashboardServiceOptions
{
    /// <summary>
    ///     Gets or sets whether the dashboard service remains running while its
    ///     view is not the shell's current feature.
    /// </summary>
    public bool KeepRunning { get; set; }
}
