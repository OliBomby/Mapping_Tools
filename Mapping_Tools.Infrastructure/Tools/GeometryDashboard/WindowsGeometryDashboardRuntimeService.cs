using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

/// <summary>
///     Combines Windows process, window, and editor-reader state into the
///     semantic runtime snapshot consumed by the application.
/// </summary>
public sealed class WindowsGeometryDashboardRuntimeService : IGeometryDashboardRuntime
{
    private readonly ILiveBeatmapReader liveReader;
    private readonly IGeometryDashboardProcessDiscovery processes;
    private readonly IGeometryDashboardWindowService windows;

    /// <summary>Creates a runtime service from the Windows platform adapters.</summary>
    /// <param name="processes">Discovers the stable osu! process.</param>
    /// <param name="liveReader">Reads semantic editor state without exposing platform identity.</param>
    /// <param name="windows">Reads the process main window and activation state.</param>
    public WindowsGeometryDashboardRuntimeService(
        IGeometryDashboardProcessDiscovery processes,
        ILiveBeatmapReader liveReader,
        IGeometryDashboardWindowService windows)
    {
        this.processes = processes ?? throw new ArgumentNullException(nameof(processes));
        this.liveReader = liveReader ?? throw new ArgumentNullException(nameof(liveReader));
        this.windows = windows ?? throw new ArgumentNullException(nameof(windows));
    }

    /// <inheritdoc />
    public async Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var process = await processes.FindAsync(cancellationToken).ConfigureAwait(false);
        if (process is null) return null;

        var window = windows.GetMainWindow(process);
        if (window is null || !window.Title.EndsWith(".osu", StringComparison.Ordinal)) return null;

        var editor = await liveReader
            .ReadAsync(cancellationToken)
            .ConfigureAwait(false);
        return editor is null
            ? null
            : new GeometryDashboardRuntimeSnapshot(editor, window.IsActivated);
    }
}
