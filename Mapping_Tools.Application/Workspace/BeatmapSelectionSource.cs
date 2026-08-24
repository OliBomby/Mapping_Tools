namespace Mapping_Tools.Application.Workspace;

/// <summary>
///     Identifies why the workspace selection changed so consumers can distinguish
///     startup, file-dialog, drag/drop, recent-list, and live-editor updates.
/// </summary>
public enum BeatmapSelectionSource
{
    /// <summary>
    ///     The caller changed selection without a more specific user interaction.
    /// </summary>
    Programmatic,

    /// <summary>
    ///     Startup restored the newest persisted recent entry.
    /// </summary>
    Startup,

    /// <summary>
    ///     A native beatmap file picker returned paths.
    /// </summary>
    FilePicker,

    /// <summary>
    ///     The user activated entries in recent-map history.
    /// </summary>
    RecentHistory,

    /// <summary>
    ///     Files were dropped onto the desktop shell.
    /// </summary>
    DragAndDrop,

    /// <summary>
    ///     The osu! integration reported its currently open beatmap.
    /// </summary>
    CurrentEditor,
}

