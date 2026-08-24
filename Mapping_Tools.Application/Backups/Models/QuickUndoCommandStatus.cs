namespace Mapping_Tools.Application.Backups.Models;

/// <summary>
///     Distinguishes a completed one-key restore from missing editor state,
///     exhausted backup history, and a captured restore failure.
/// </summary>
public enum QuickUndoCommandStatus
{
    /// <summary>
    ///     The newest compatible backup replaced the current beatmap.
    /// </summary>
    Restored,

    /// <summary>
    ///     osu! did not expose a current beatmap path to restore.
    /// </summary>
    NoCurrentBeatmap,

    /// <summary>
    ///     The backup store contained no snapshot eligible for QuickUndo.
    /// </summary>
    NoBackup,

    /// <summary>
    ///     Current-map discovery or restore failed and its exception was reported.
    /// </summary>
    Failed,
}

