using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;

/// <summary>Finds the stable osu! process used by the Windows Geometry Dashboard adapters.</summary>
public interface IGeometryDashboardProcessDiscovery
{
    /// <summary>Finds the first matching stable osu! process.</summary>
    /// <param name="cancellationToken">Cancels before enumeration begins.</param>
    /// <returns>The process snapshot, or <see langword="null" /> when unavailable.</returns>
    Task<GeometryDashboardProcess?> FindAsync(CancellationToken cancellationToken = default);
}
