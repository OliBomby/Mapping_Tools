using Mapping_Tools.Application.Projects;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Application.GeometryDashboard;

/// <summary>Supplies the shared persistence definition used by Geometry Dashboard UI workflows.</summary>
public static class GeometryDashboardProjectDefinition
{
    /// <summary>
    /// Gets a project definition using the legacy autosave name and the current
    /// application project-picker conventions.
    /// </summary>
    public static ProjectDefinition<SnappingToolsProject> Definition { get; } = new(
        "geometrydashboardproject.json",
        "Geometry Dashboard Projects",
        () => new SnappingToolsProject(),
        "geometry-dashboard-project.json");
}
