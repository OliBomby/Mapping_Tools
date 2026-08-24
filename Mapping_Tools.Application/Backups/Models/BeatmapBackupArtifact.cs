namespace Mapping_Tools.Application.Backups.Models;

/// <summary>
///     Describes one physical backup and the source state from which it was created.
/// </summary>
/// <param name="Path">The complete path of the retained backup file.</param>
/// <param name="SourcePath">The beatmap or storyboard path protected by the snapshot.</param>
/// <param name="Reason">The operation that caused this snapshot to be written.</param>
/// <param name="ContainsUnsavedState">
///     Whether the contents came from an editing session rather than a direct disk copy.
/// </param>
/// <param name="CreatedAt">The timestamp used in the filename and retention ordering.</param>
public sealed record BeatmapBackupArtifact(
    string Path,
    string SourcePath,
    BeatmapBackupReason Reason,
    bool ContainsUnsavedState,
    DateTimeOffset CreatedAt);

