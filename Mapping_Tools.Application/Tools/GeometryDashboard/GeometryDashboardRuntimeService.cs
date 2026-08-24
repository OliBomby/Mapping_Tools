namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>
///     Implements the Geometry Dashboard external-state read sequence without
///     depending on a view, timer, dispatcher, or native handle type.
/// </summary>
public sealed class GeometryDashboardRuntimeService : IGeometryDashboardRuntime
{
    private readonly IGeometryDashboardEditorReader editor;
    private readonly IGeometryDashboardProcessDiscovery processes;
    private readonly IGeometryDashboardScreenService screens;
    private readonly IGeometryDashboardWindowService windows;

    /// <summary>
    ///     Creates a runtime service from the independent platform ports.
    /// </summary>
    /// <param name="processes">Discovers the stable osu! process.</param>
    /// <param name="editor">Reads validated editor memory.</param>
    /// <param name="windows">Selects and snapshots the process main window.</param>
    /// <param name="screens">Supplies the legacy primary monitor selection.</param>
    public GeometryDashboardRuntimeService(
        IGeometryDashboardProcessDiscovery processes,
        IGeometryDashboardEditorReader editor,
        IGeometryDashboardWindowService windows,
        IGeometryDashboardScreenService screens)
    {
        this.processes = processes ?? throw new ArgumentNullException(nameof(processes));
        this.editor = editor ?? throw new ArgumentNullException(nameof(editor));
        this.windows = windows ?? throw new ArgumentNullException(nameof(windows));
        this.screens = screens ?? throw new ArgumentNullException(nameof(screens));
    }

    /// <inheritdoc />
    public async Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var process = await processes
            .FindAsync(cancellationToken)
            .ConfigureAwait(false);
        if (process is null) return null;

        var window = windows.GetMainWindow(process);
        if (window is null) return null;

        if (!window.Title.EndsWith(".osu", StringComparison.Ordinal)) return null;

        var editor = await this.editor
            .ReadGeometryDashboardAsync(process, cancellationToken)
            .ConfigureAwait(false);
        if (editor is null) return null;

        return new GeometryDashboardRuntimeSnapshot(
            process,
            window,
            editor,
            screens.GetPrimaryScreen());
    }
}
