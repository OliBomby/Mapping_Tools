using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Platform;

/// <summary>
///     Creates native target-bound overlay hosts for Geometry Dashboard.
/// </summary>
public sealed class WindowsGeometryDashboardOverlayHostFactory : IGeometryDashboardOverlayHostFactory
{
    private readonly Func<bool> isWindows;
    private readonly IGeometryDashboardWindowService windows;

    /// <summary>Creates a factory using the native window service and current platform guard.</summary>
    public WindowsGeometryDashboardOverlayHostFactory(
        IGeometryDashboardWindowService windows)
        : this(windows, OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardOverlayHostFactory(
        IGeometryDashboardWindowService windows,
        Func<bool> isWindows)
    {
        this.windows = windows ?? throw new ArgumentNullException(nameof(windows));
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc />
    public IGeometryDashboardOverlayHost Create()
    {
        return new WindowsGeometryDashboardOverlayHost(windows, isWindows);
    }
}

