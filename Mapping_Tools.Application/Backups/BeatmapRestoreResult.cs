using Mapping_Tools.Application.BeatmapEditing;

namespace Mapping_Tools.Application.Backups;

/// <summary>
///     Records a completed restore together with the safety snapshot that makes
///     the overwrite reversible.
/// </summary>
/// <param name="BackupPath">The snapshot copied into the destination.</param>
/// <param name="DestinationPath">The beatmap that was replaced.</param>
/// <param name="SafetyBackup">The destination state captured before replacement.</param>
public sealed record BeatmapRestoreResult(
    string BackupPath,
    string DestinationPath,
    BeatmapBackupArtifact SafetyBackup);

