namespace Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;

/// <summary>Creates target-bound Geometry Dashboard overlay hosts.</summary>
public interface IGeometryDashboardOverlayHostFactory
{
    /// <summary>Creates a disposable overlay host, including an unavailable-platform no-op host.</summary>
    /// <returns>A host whose <see cref="IGeometryDashboardOverlayHost.IsSupported" /> reports platform availability.</returns>
    IGeometryDashboardOverlayHost Create();
}
