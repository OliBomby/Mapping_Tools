using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Application.Tools.GeometryDashboard;

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

