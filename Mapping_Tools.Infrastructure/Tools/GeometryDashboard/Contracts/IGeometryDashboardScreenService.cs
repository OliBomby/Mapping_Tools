using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;

/// <summary>Enumerates monitors used by the Windows Geometry Dashboard adapters.</summary>
public interface IGeometryDashboardScreenService
{
    /// <summary>Gets whether native monitor inspection is available.</summary>
    bool IsSupported { get; }

    /// <summary>Enumerates readable monitors.</summary>
    /// <returns>Monitor snapshots in native enumeration order.</returns>
    IReadOnlyList<GeometryDashboardScreen> GetScreens();

    /// <summary>Reads the primary monitor.</summary>
    /// <returns>The primary monitor, or <see langword="null" /> when unavailable.</returns>
    GeometryDashboardScreen? GetPrimaryScreen();

    /// <summary>Reads the monitor nearest to a window.</summary>
    /// <param name="window">The opaque native window identifier.</param>
    /// <returns>The nearest monitor, or <see langword="null" /> when unavailable.</returns>
    GeometryDashboardScreen? GetScreenForWindow(PlatformWindowId window);
}