using Mapping_Tools.Application.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;

/// <summary>
///     Tracks top-level windows without leaking native window handles.
/// </summary>
public interface IGeometryDashboardWindowService
{
    /// <summary>Gets whether native window inspection is available.</summary>
    bool IsSupported { get; }

    /// <summary>Gets a current snapshot for a window identifier.</summary>
    /// <param name="window">The window identifier.</param>
    /// <returns>The current window, or <see langword="null" /> when it no longer exists.</returns>
    GeometryDashboardWindow? GetWindow(PlatformWindowId window);

    /// <summary>Gets the current main window for a discovered process.</summary>
    /// <param name="process">The process snapshot whose window should be tracked.</param>
    /// <returns>The current main window, or <see langword="null" /> when unavailable.</returns>
    GeometryDashboardWindow? GetMainWindow(GeometryDashboardProcess process);

    /// <summary>Enumerates current top-level windows in native enumeration order.</summary>
    /// <returns>Window snapshots that could be read successfully.</returns>
    IReadOnlyList<GeometryDashboardWindow> GetTopLevelWindows();
}

