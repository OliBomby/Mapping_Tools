using Mapping_Tools.Application.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;

/// <summary>
///     Reads the selected osu! editor state needed by Geometry Dashboard.
/// </summary>
public interface IGeometryDashboardEditorReader
{
    /// <summary>
    ///     Captures a validated memory snapshot from the active osu! editor.
    /// </summary>
    /// <param name="process">The process snapshot selected for this read.</param>
    /// <param name="cancellationToken">Cancels before or during the memory read.</param>
    /// <returns>
    ///     A snapshot when osu! is running with an open editor, or <see langword="null" />
    ///     when the process/editor is unavailable.
    /// </returns>
    Task<GeometryDashboardEditorSnapshot?> ReadGeometryDashboardAsync(
        GeometryDashboardProcess process,
        CancellationToken cancellationToken = default);
}

