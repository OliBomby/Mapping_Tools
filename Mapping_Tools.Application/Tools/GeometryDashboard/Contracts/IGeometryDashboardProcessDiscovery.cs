using Mapping_Tools.Application.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;

/// <summary>
///     Finds the stable osu! process used by Geometry Dashboard.
/// </summary>
public interface IGeometryDashboardProcessDiscovery
{
    /// <summary>
    ///     Gets whether the adapter can inspect native processes on this platform.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    ///     Finds the first process whose executable and product identity match osu! stable.
    /// </summary>
    /// <param name="cancellationToken">Cancels before process enumeration begins.</param>
    /// <returns>The matching process snapshot, or <see langword="null" /> when unavailable.</returns>
    Task<GeometryDashboardProcess?> FindAsync(CancellationToken cancellationToken = default);
}

