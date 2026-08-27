namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>Provides the discoverable metadata for Geometry Dashboard.</summary>
public static class GeometryDashboardToolDefinition
{
    /// <summary>Gets the stable Geometry Dashboard metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "geometry-dashboard",
        "Geometry Dashboard",
        "Generate, display, snap to, and save useful geometry around osu! hit objects.",
        ["geometry", "snapping", "virtual objects", "overlay", "generators"]);
}
