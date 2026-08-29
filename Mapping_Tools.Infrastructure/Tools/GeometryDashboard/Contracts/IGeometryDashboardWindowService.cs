using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;

/// <summary>Tracks top-level windows used by the Windows Geometry Dashboard adapters.</summary>
public interface IGeometryDashboardWindowService
{
    /// <summary>Gets whether native window inspection is available.</summary>
    bool IsSupported { get; }

    /// <summary>Reads a current window snapshot.</summary>
    /// <param name="window">The opaque native window identifier.</param>
    /// <returns>The current window, or <see langword="null" /> when unavailable.</returns>
    GeometryDashboardWindow? GetWindow(PlatformWindowId window);

    /// <summary>Reads the current main window for a process.</summary>
    /// <param name="process">The process whose main window is requested.</param>
    /// <returns>The current window, or <see langword="null" /> when unavailable.</returns>
    GeometryDashboardWindow? GetMainWindow(GeometryDashboardProcess process);

    /// <summary>Enumerates readable top-level windows.</summary>
    /// <returns>Window snapshots in native enumeration order.</returns>
    IReadOnlyList<GeometryDashboardWindow> GetTopLevelWindows();
}