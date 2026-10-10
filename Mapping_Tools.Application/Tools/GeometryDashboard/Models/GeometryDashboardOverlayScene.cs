namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

/// <summary>Describes the complete neutral scene rendered by the overlay.</summary>
public sealed record GeometryDashboardOverlayScene(
    IReadOnlyList<GeometryDashboardOverlayShape> Shapes)
{
    /// <summary>Gets an empty scene that clears all previous geometry.</summary>
    public static GeometryDashboardOverlayScene Empty { get; } = new([]);
}
