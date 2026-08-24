namespace Mapping_Tools.Application.Backups.Models;

/// <summary>
///     Reports a QuickUndo attempt without requiring a hotkey callback to show a
///     dialog or inspect backup storage.
/// </summary>
/// <param name="Status">Whether a map was restored or why no replacement occurred.</param>
/// <param name="Restore">The completed restore metadata when a backup was applied.</param>
/// <param name="Exception">The captured lookup or restore failure retained for diagnostics.</param>
public sealed record QuickUndoCommandResult(
    QuickUndoCommandStatus Status,
    BeatmapRestoreResult? Restore = null,
    Exception? Exception = null);

