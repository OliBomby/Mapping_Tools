namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>
///     Reads the external state required before the Geometry Dashboard engine can
///     update geometry or start an overlay.
/// </summary>
public interface IGeometryDashboardRuntime
{
    /// <summary>
    ///     Attempts to read a complete runtime snapshot in legacy dependency order.
    /// </summary>
    /// <param name="cancellationToken">Cancels process discovery or editor memory access.</param>
    /// <returns>
    ///     A complete snapshot, or <see langword="null" /> when osu!, its main window,
    ///     or its editor is unavailable. Reader validation exceptions are preserved.
    /// </returns>
    Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default);
}

