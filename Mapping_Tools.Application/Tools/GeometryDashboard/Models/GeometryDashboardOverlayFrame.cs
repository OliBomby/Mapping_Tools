namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

/// <summary>Describes the neutral geometry payload rendered by the native overlay.</summary>
public sealed record GeometryDashboardOverlayFrame(
    IReadOnlyList<GeometryDashboardOverlayShape> Shapes)
{
    /// <summary>Gets an empty frame that clears all previous geometry.</summary>
    public static GeometryDashboardOverlayFrame Empty { get; } = new([]);
}

