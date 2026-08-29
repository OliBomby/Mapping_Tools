using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

/// <summary>
///     Resolves live desktop state and renders neutral Geometry Dashboard scenes
///     through one reusable Windows overlay.
/// </summary>
public sealed class WindowsGeometryDashboardOverlayService : IGeometryDashboardOverlayService
{
    private readonly WindowsGeometryDashboardCoordinateContext coordinates;
    private readonly WindowsGeometryDashboardOverlayHost host;
    private readonly Func<bool> isWindows;
    private readonly object gate = new();
    private bool disposed;

    /// <summary>Creates the overlay service from the shared coordinate context.</summary>
    /// <param name="coordinates">Refreshes immutable transforms from live platform state.</param>
    /// <param name="windows">Reads the target window followed by the native overlay.</param>
    public WindowsGeometryDashboardOverlayService(
        WindowsGeometryDashboardCoordinateContext coordinates,
        IGeometryDashboardWindowService windows)
        : this(coordinates, windows, OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardOverlayService(
        WindowsGeometryDashboardCoordinateContext coordinates,
        IGeometryDashboardWindowService windows,
        Func<bool> isWindows)
    {
        this.coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
        host = new WindowsGeometryDashboardOverlayHost(
            windows ?? throw new ArgumentNullException(nameof(windows)),
            isWindows);
    }

    /// <inheritdoc />
    public bool IsSupported => !disposed && isWindows() && coordinates.IsSupported;

    /// <inheritdoc />
    public bool IsVisible => host.IsVisible;

    /// <inheritdoc />
    public string? ConfigurationStatus => coordinates.ConfigurationStatus;

    /// <inheritdoc />
    public void Update(
        GeometryDashboardOverlayScene scene,
        GeometryDashboardOverlayOptions options)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(options);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            if (!IsSupported || !coordinates.TryRefresh(options.EditorBoxOffset, out var snapshot))
            {
                host.Disable();
                return;
            }

            if (host.TargetWindow != snapshot.Window.Id)
                host.Initialize(snapshot.Window.Id);

            host.SetScene(scene, snapshot.Transform);
            host.SetBorder(options.ShowDebugBorder);
            host.Enable();
            host.Update(
                snapshot.Transform.EditorBox,
                snapshot.Transform.GetDpiMultiplier(),
                snapshot.Transform.DpiSourceAvailable);
        }
    }

    /// <inheritdoc />
    public void Hide()
    {
        lock (gate)
        {
            if (!disposed) host.Disable();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;

            disposed = true;
            host.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
