using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

/// <summary>
///     Maintains the latest immutable coordinate transform for the current osu!
///     editor and atomically replaces it when desktop state changes.
/// </summary>
public sealed class WindowsGeometryDashboardCoordinateContext
{
    private readonly WindowsGeometryDashboardOsuConfigProvider configuration;
    private readonly IGeometryDashboardProcessDiscovery processes;
    private readonly IGeometryDashboardScreenService screens;
    private readonly IGeometryDashboardWindowService windows;
    private readonly Func<bool> isWindows;
    private readonly object refreshGate = new();
    private Box2 editorBoxOffset = new(0, 1, 0, 1);
    private WindowsGeometryDashboardCoordinateSnapshot? current;

    /// <summary>
    ///     Creates a live coordinate context from the platform services that own
    ///     process, window, monitor, DPI, and configuration discovery.
    /// </summary>
    /// <param name="settings">Application settings containing the osu! config path.</param>
    /// <param name="files">The text-file abstraction used to read osu! configuration.</param>
    /// <param name="processes">Discovers the current stable osu! process.</param>
    /// <param name="windows">Reads the process window bounds, activation, and DPI.</param>
    /// <param name="screens">Reads monitor bounds used by osu!'s fullscreen layout.</param>
    public WindowsGeometryDashboardCoordinateContext(
        ApplicationSettings settings,
        ITextFileStore files,
        IGeometryDashboardProcessDiscovery processes,
        IGeometryDashboardWindowService windows,
        IGeometryDashboardScreenService screens)
        : this(
            settings,
            files,
            processes,
            windows,
            screens,
            OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardCoordinateContext(
        ApplicationSettings settings,
        ITextFileStore files,
        IGeometryDashboardProcessDiscovery processes,
        IGeometryDashboardWindowService windows,
        IGeometryDashboardScreenService screens,
        Func<bool> isWindows)
    {
        configuration = new WindowsGeometryDashboardOsuConfigProvider(settings, files);
        this.processes = processes ?? throw new ArgumentNullException(nameof(processes));
        this.windows = windows ?? throw new ArgumentNullException(nameof(windows));
        this.screens = screens ?? throw new ArgumentNullException(nameof(screens));
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <summary>Gets whether this context can query the current desktop.</summary>
    public bool IsSupported => isWindows() && processes.IsSupported && windows.IsSupported && screens.IsSupported;

    /// <summary>Gets the latest configuration status observed while refreshing the context.</summary>
    public string? ConfigurationStatus => configuration.Status;

    /// <summary>
    ///     Refreshes the transform from current desktop state and replaces the
    ///     context's current immutable snapshot when osu! is available.
    /// </summary>
    /// <param name="editorBoxOffset">The osu! editor-space offset requested by the feature.</param>
    /// <param name="snapshot">Receives the refreshed coordinate snapshot.</param>
    /// <returns><see langword="true" /> when a usable osu! window was found.</returns>
    internal bool TryRefresh(
        Box2 editorBoxOffset,
        out WindowsGeometryDashboardCoordinateSnapshot snapshot)
    {
        lock (refreshGate)
        {
            this.editorBoxOffset = editorBoxOffset;
            return TryRefreshCore(editorBoxOffset, out snapshot);
        }
    }

    /// <summary>
    ///     Refreshes the transform using the most recently supplied overlay
    ///     offset, which lets input follow window and configuration changes too.
    /// </summary>
    /// <param name="snapshot">Receives the refreshed coordinate snapshot.</param>
    /// <returns><see langword="true" /> when a usable osu! window was found.</returns>
    internal bool TryRefresh(out WindowsGeometryDashboardCoordinateSnapshot snapshot)
    {
        lock (refreshGate) return TryRefreshCore(editorBoxOffset, out snapshot);
    }

    private bool TryRefreshCore(
        Box2 editorBoxOffset,
        out WindowsGeometryDashboardCoordinateSnapshot snapshot)
    {
        snapshot = default!;
        if (!IsSupported) return false;

        var process = processes.FindAsync().GetAwaiter().GetResult();
        var window = process is null ? null : windows.GetMainWindow(process);
        if (window is null || !window.Title.EndsWith(".osu", StringComparison.Ordinal)) return false;

        var screen = screens.GetScreenForWindow(window.Id) ?? screens.GetPrimaryScreen();
        var transform = new WindowsGeometryDashboardCoordinateTransform(
            window,
            screen,
            configuration.Read(),
            editorBoxOffset);
        snapshot = new WindowsGeometryDashboardCoordinateSnapshot(
            window,
            transform,
            ConfigurationStatus);
        Interlocked.Exchange(ref current, snapshot);
        return true;
    }

    /// <summary>Gets the latest usable transform without performing a desktop query.</summary>
    /// <param name="snapshot">Receives the most recently refreshed snapshot.</param>
    /// <returns><see langword="true" /> when a snapshot has been established.</returns>
    internal bool TryGetCurrent(out WindowsGeometryDashboardCoordinateSnapshot snapshot)
    {
        snapshot = Volatile.Read(ref current)!;
        return snapshot is not null;
    }
}

internal sealed record WindowsGeometryDashboardCoordinateSnapshot(
    GeometryDashboardWindow Window,
    WindowsGeometryDashboardCoordinateTransform Transform,
    string? ConfigurationStatus);
