using Mapping_Tools.Application.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;

/// <summary>
///     Enumerates monitors and supplies their physical bounds and effective DPI.
/// </summary>
public interface IGeometryDashboardScreenService
{
    /// <summary>Gets whether native monitor enumeration is available.</summary>
    bool IsSupported { get; }

    /// <summary>Gets all monitors in the current virtual desktop.</summary>
    IReadOnlyList<GeometryDashboardScreen> GetScreens();

    /// <summary>Gets the primary monitor, or <see langword="null" /> when unavailable.</summary>
    GeometryDashboardScreen? GetPrimaryScreen();

    /// <summary>
    ///     Gets the monitor containing a window's nearest monitor area.
    /// </summary>
    /// <param name="window">The window whose monitor should be selected.</param>
    /// <returns>The containing monitor, or <see langword="null" /> when unavailable.</returns>
    GeometryDashboardScreen? GetScreenForWindow(PlatformWindowId window);
}

