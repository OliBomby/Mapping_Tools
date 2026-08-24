namespace Mapping_Tools.Application.BeatmapEditing.Models;

/// <summary>
///     Reports a BetterSave attempt without requiring hotkey or watcher callbacks to present UI.
/// </summary>
/// <param name="Status">Whether the document was saved or why it was not.</param>
/// <param name="Path">The current beatmap path when lookup succeeded.</param>
/// <param name="Exception">The captured failure retained for diagnostics.</param>
public sealed record BetterSaveResult(
    BetterSaveStatus Status,
    string? Path = null,
    Exception? Exception = null);

