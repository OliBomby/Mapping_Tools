namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

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

