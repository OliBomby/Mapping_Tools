namespace Mapping_Tools.Application.GeometryDashboard;

/// <summary>
///     Combines the Windows support ports into one dashboard-ready runtime snapshot.
/// </summary>
public sealed class GeometryDashboardRuntimeSnapshot
{
    /// <summary>
    ///     Creates a snapshot after process, window, and editor availability have
    ///     been established by <see cref="GeometryDashboardRuntimeService" />.
    /// </summary>
    /// <param name="process">The discovered stable osu! process.</param>
    /// <param name="window">The process main window at read time.</param>
    /// <param name="editor">The validated live editor state.</param>
    /// <param name="primaryScreen">The legacy primary monitor, when monitor enumeration succeeded.</param>
    public GeometryDashboardRuntimeSnapshot(
        GeometryDashboardProcess process,
        GeometryDashboardWindow window,
        GeometryDashboardEditorSnapshot editor,
        GeometryDashboardScreen? primaryScreen)
    {
        Process = process ?? throw new ArgumentNullException(nameof(process));
        Window = window ?? throw new ArgumentNullException(nameof(window));
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
        PrimaryScreen = primaryScreen;
    }

    /// <summary>Gets the discovered stable osu! process.</summary>
    public GeometryDashboardProcess Process { get; }

    /// <summary>Gets the process main window snapshot.</summary>
    public GeometryDashboardWindow Window { get; }

    /// <summary>Gets the validated live editor-memory snapshot.</summary>
    public GeometryDashboardEditorSnapshot Editor { get; }

    /// <summary>Gets the primary monitor used by the legacy coordinate converter, when available.</summary>
    public GeometryDashboardScreen? PrimaryScreen { get; }
}

/// <summary>
///     Reads the external state required before the Geometry Dashboard engine can
///     update geometry or start an overlay.
/// </summary>
public interface IGeometryDashboardRuntime
{
    /// <summary>
    ///     Attempts to read a complete runtime snapshot in legacy dependency order.
    /// </summary>
    /// <param name="cancellationToken">Cancels process discovery or editor memory access.</param>
    /// <returns>
    ///     A complete snapshot, or <see langword="null" /> when osu!, its main window,
    ///     or its editor is unavailable. Reader validation exceptions are preserved.
    /// </returns>
    Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default);
}

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
