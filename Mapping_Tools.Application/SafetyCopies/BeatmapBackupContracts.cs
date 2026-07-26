using Mapping_Tools.Application.BeatmapEditing;

namespace Mapping_Tools.Application.SafetyCopies;

/// <summary>
/// Identifies why a snapshot exists so filenames and future retention policies
/// can distinguish tool safety copies from explicit user actions.
/// </summary>
public enum BeatmapBackupReason
{
    /// <summary>
    /// Protects the durable file immediately before a mapping operation overwrites it.
    /// </summary>
    Automatic,

    /// <summary>
    /// Records a snapshot explicitly requested by the user.
    /// </summary>
    User,

    /// <summary>
    /// Captures changed in-memory editor content during periodic monitoring.
    /// </summary>
    Periodic,

    /// <summary>
    /// Preserves the current destination immediately before a restore or QuickUndo.
    /// </summary>
    RestoreSafety
}

/// <summary>
/// Describes one physical backup and the source state from which it was created.
/// </summary>
/// <param name="Path">The complete path of the retained backup file.</param>
/// <param name="SourcePath">The beatmap or storyboard path protected by the snapshot.</param>
/// <param name="Reason">The operation that caused this snapshot to be written.</param>
/// <param name="ContainsUnsavedState">
/// Whether the contents came from an editing session rather than a direct disk copy.
/// </param>
/// <param name="CreatedAt">The timestamp used in the filename and retention ordering.</param>
public sealed record BeatmapBackupArtifact(
    string Path,
    string SourcePath,
    BeatmapBackupReason Reason,
    bool ContainsUnsavedState,
    DateTimeOffset CreatedAt);

/// <summary>
/// Collects every file produced for a request that may protect several maps
/// or both the durable and unsaved versions of one map.
/// </summary>
/// <param name="Artifacts">Backups in source order, with a live companion immediately after its disk copy.</param>
/// <param name="SkippedByPreference">
/// Whether automatic backups were disabled and the caller did not force the request.
/// </param>
public sealed record BeatmapBackupResult(
    IReadOnlyList<BeatmapBackupArtifact> Artifacts,
    bool SkippedByPreference);

/// <summary>
/// Records a completed restore together with the safety snapshot that makes
/// the overwrite reversible.
/// </summary>
/// <param name="BackupPath">The snapshot copied into the destination.</param>
/// <param name="DestinationPath">The beatmap that was replaced.</param>
/// <param name="SafetyBackup">The destination state captured before replacement.</param>
public sealed record BeatmapRestoreResult(
    string BackupPath,
    string DestinationPath,
    BeatmapBackupArtifact SafetyBackup);

/// <summary>
/// Supplies filesystem operations and creation timestamps needed for backup
/// retention without exposing <see cref="FileInfo"/> or <see cref="DirectoryInfo"/>
/// to application orchestration.
/// </summary>
public interface IBeatmapBackupStore
{
    /// <summary>
    /// Checks whether a source or restore destination currently names a physical file.
    /// </summary>
    /// <param name="path">The candidate file path.</param>
    /// <returns><see langword="true"/> only for an existing file.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Checks whether the configured backup destination is available before a destructive action.
    /// </summary>
    /// <param name="path">The candidate directory path.</param>
    /// <returns><see langword="true"/> only for an existing directory.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Extracts the final filename using the host platform's path rules.
    /// </summary>
    /// <param name="path">A path whose leaf component is required.</param>
    /// <returns>The filename including its extension.</returns>
    string GetFileName(string path);

    /// <summary>
    /// Joins a backup directory and generated filename using host path semantics.
    /// </summary>
    /// <param name="directory">The configured backup root.</param>
    /// <param name="fileName">A single generated backup filename.</param>
    /// <returns>The full destination path.</returns>
    string Combine(string directory, string fileName);

    /// <summary>
    /// Copies a durable source into a backup or restores a backup over its destination.
    /// </summary>
    /// <param name="sourcePath">The existing file to read.</param>
    /// <param name="destinationPath">The file to create or replace.</param>
    /// <param name="cancellationToken">Cancels before opening either file and during stream copying.</param>
    /// <returns>A task that completes only after the destination stream has been flushed.</returns>
    Task CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes an in-memory editor snapshot directly into the backup directory.
    /// </summary>
    /// <param name="destinationPath">The generated backup path.</param>
    /// <param name="lines">The complete osu! document in serialization order.</param>
    /// <param name="cancellationToken">Cancels before or during the write.</param>
    /// <returns>A task that completes only after all lines have been flushed.</returns>
    Task WriteLinesAsync(
        string destinationPath,
        IReadOnlyList<string> lines,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates retained files newest first according to filesystem creation time.
    /// </summary>
    /// <param name="directory">The configured backup root.</param>
    /// <param name="cancellationToken">Cancels before enumeration begins.</param>
    /// <returns>An ordered snapshot suitable for QuickUndo and retention pruning.</returns>
    Task<IReadOnlyList<StoredBeatmapBackup>> ListAsync(
        string directory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes one retention candidate after newer backups have been secured.
    /// </summary>
    /// <param name="path">The exact backup selected for pruning.</param>
    /// <param name="cancellationToken">Cancels before deletion.</param>
    /// <returns>A task that completes when the file no longer exists.</returns>
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Carries only the physical metadata required to order and prune retained backups.
/// </summary>
/// <param name="Path">The complete file path.</param>
/// <param name="CreatedAt">The filesystem creation timestamp used by legacy ordering.</param>
public sealed record StoredBeatmapBackup(string Path, DateTimeOffset CreatedAt);

/// <summary>
/// Creates, retains, restores, and locates beatmap safety copies independently
/// of dialogs, hotkeys, timers, and either desktop frontend.
/// </summary>
public interface IBeatmapBackupService
{
    /// <summary>
    /// Copies durable files into the backup directory and applies the configured retention limit.
    /// </summary>
    /// <param name="sourcePaths">Files protected in enumeration order.</param>
    /// <param name="reason">The filename code and policy category for the request.</param>
    /// <param name="force">
    /// When <see langword="true"/>, creates the backup even if automatic backups are disabled.
    /// </param>
    /// <param name="cancellationToken">Stops before the next copy or retention deletion.</param>
    /// <returns>The created artifacts, or a preference-skipped result with no artifacts.</returns>
    /// <exception cref="FileNotFoundException">A requested source does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">The configured backup directory is unavailable.</exception>
    /// <exception cref="OperationCanceledException">Cancellation occurs before all requested copies and pruning finish.</exception>
    Task<BeatmapBackupResult> CreateAsync(
        IEnumerable<string> sourcePaths,
        BeatmapBackupReason reason,
        bool force = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Protects both the on-disk baseline and, when present, the matching
    /// unsaved editor version of a beatmap.
    /// </summary>
    /// <param name="session">The editing session whose path and provenance identify the versions to retain.</param>
    /// <param name="reason">The filename code and policy category for the request.</param>
    /// <param name="force">Whether to ignore the automatic-backup preference.</param>
    /// <param name="cancellationToken">Stops before the next copy, snapshot write, or prune.</param>
    /// <returns>One disk artifact and, for a live session, one companion artifact.</returns>
    /// <exception cref="FileNotFoundException">The session's durable source no longer exists.</exception>
    /// <exception cref="DirectoryNotFoundException">The configured backup directory is unavailable.</exception>
    /// <exception cref="OperationCanceledException">Cancellation occurs before both versions and pruning finish.</exception>
    Task<BeatmapBackupResult> CreateAsync(
        BeatmapEditingSession session,
        BeatmapBackupReason reason,
        bool force = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes changed session contents once and suppresses later periodic
    /// snapshots until the serialized document changes again.
    /// </summary>
    /// <param name="session">The current disk or live editor state.</param>
    /// <param name="cancellationToken">Cancels hashing, writing, or pruning.</param>
    /// <returns>The new artifact, or <see langword="null"/> when disabled or unchanged.</returns>
    /// <exception cref="DirectoryNotFoundException">Periodic backup is enabled but its destination is unavailable.</exception>
    /// <exception cref="OperationCanceledException">Cancellation occurs before hashing, writing, or pruning finishes.</exception>
    Task<BeatmapBackupArtifact?> CreatePeriodicIfChangedAsync(
        BeatmapEditingSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a beatmap with a chosen backup only after validating metadata
    /// and preserving the current destination as a restore-safety backup.
    /// </summary>
    /// <param name="backupPath">The snapshot to restore.</param>
    /// <param name="destinationPath">The existing beatmap to replace.</param>
    /// <param name="allowDifferentFilename">
    /// Allows restore when artist, title, creator, or difficulty metadata derive a different filename.
    /// </param>
    /// <param name="reloadEditor">Requests an osu! reload after the restored file is durable.</param>
    /// <param name="cancellationToken">Cancels before validation, safety backup, replacement, or reload.</param>
    /// <returns>The completed restore and its safety snapshot.</returns>
    /// <exception cref="FileNotFoundException">The selected backup or restore destination does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">The safety backup directory is unavailable.</exception>
    /// <exception cref="BeatmapBackupIncompatibleException">
    /// Metadata differs and <paramref name="allowDifferentFilename"/> is <see langword="false"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">Cancellation occurs before replacement and any requested reload finish.</exception>
    Task<BeatmapRestoreResult> RestoreAsync(
        string backupPath,
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the newest retained file into the requested beatmap using the
    /// same compatibility and safety rules as an explicit restore.
    /// </summary>
    /// <param name="destinationPath">The current beatmap that QuickUndo should replace.</param>
    /// <param name="allowDifferentFilename">Allows the globally newest backup to belong to different metadata.</param>
    /// <param name="reloadEditor">Requests an osu! reload after replacement.</param>
    /// <param name="cancellationToken">Cancels before lookup, validation, replacement, or reload.</param>
    /// <returns>The restore result, or <see langword="null"/> when no backup exists.</returns>
    /// <exception cref="DirectoryNotFoundException">The configured backup directory is unavailable.</exception>
    /// <exception cref="BeatmapBackupIncompatibleException">
    /// The newest backup has different metadata and <paramref name="allowDifferentFilename"/> is <see langword="false"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">Cancellation occurs before lookup or restoration finishes.</exception>
    Task<BeatmapRestoreResult?> QuickUndoAsync(
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Prevents a restore from silently replacing a different difficulty or mapset.
/// </summary>
public sealed class BeatmapBackupIncompatibleException : Exception
{
    /// <summary>
    /// Creates an error that presents both metadata-derived filenames for an
    /// informed explicit-override decision.
    /// </summary>
    /// <param name="backupFileName">The canonical filename derived from backup metadata.</param>
    /// <param name="destinationFileName">The canonical filename derived from destination metadata.</param>
    public BeatmapBackupIncompatibleException(
        string backupFileName,
        string destinationFileName)
        : base(
            "The backup and destination contain different beatmap metadata." +
            Environment.NewLine +
            backupFileName +
            Environment.NewLine +
            destinationFileName)
    {
        BackupFileName = backupFileName;
        DestinationFileName = destinationFileName;
    }

    /// <summary>
    /// Exposes the filename implied by the backup's metadata, not its timestamped storage name.
    /// </summary>
    public string BackupFileName { get; }

    /// <summary>
    /// Exposes the filename implied by the destination's current metadata.
    /// </summary>
    public string DestinationFileName { get; }
}
