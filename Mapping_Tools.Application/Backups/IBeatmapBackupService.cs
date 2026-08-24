using Mapping_Tools.Application.BeatmapEditing;

namespace Mapping_Tools.Application.Backups;

/// <summary>
///     Creates, retains, restores, and locates beatmap safety copies independently
///     of dialogs, hotkeys, timers, and either desktop frontend.
/// </summary>
public interface IBeatmapBackupService
{
    /// <summary>
    ///     Copies durable files into the backup directory and applies the configured retention limit.
    /// </summary>
    /// <param name="sourcePaths">Files protected in enumeration order.</param>
    /// <param name="reason">The filename code and policy category for the request.</param>
    /// <param name="force">
    ///     When <see langword="true" />, creates the backup even if automatic backups are disabled.
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
    ///     Protects both the on-disk baseline and, when present, the matching
    ///     unsaved editor version of a beatmap.
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
    ///     Writes changed session contents once and suppresses later periodic
    ///     snapshots until the serialized document changes again.
    /// </summary>
    /// <param name="session">The current disk or live editor state.</param>
    /// <param name="cancellationToken">Cancels hashing, writing, or pruning.</param>
    /// <returns>The new artifact, or <see langword="null" /> when disabled or unchanged.</returns>
    /// <exception cref="DirectoryNotFoundException">Periodic backup is enabled but its destination is unavailable.</exception>
    /// <exception cref="OperationCanceledException">Cancellation occurs before hashing, writing, or pruning finishes.</exception>
    Task<BeatmapBackupArtifact?> CreatePeriodicIfChangedAsync(
        BeatmapEditingSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Replaces a beatmap with a chosen backup only after validating metadata
    ///     and preserving the current destination as a restore-safety backup.
    /// </summary>
    /// <param name="backupPath">The snapshot to restore.</param>
    /// <param name="destinationPath">The existing beatmap to replace.</param>
    /// <param name="allowDifferentFilename">
    ///     Allows restore when artist, title, creator, or difficulty metadata derive a different filename.
    /// </param>
    /// <param name="reloadEditor">Requests an osu! reload after the restored file is durable.</param>
    /// <param name="cancellationToken">Cancels before validation, safety backup, replacement, or reload.</param>
    /// <returns>The completed restore and its safety snapshot.</returns>
    /// <exception cref="FileNotFoundException">The selected backup or restore destination does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">The safety backup directory is unavailable.</exception>
    /// <exception cref="BeatmapBackupIncompatibleException">
    ///     Metadata differs and <paramref name="allowDifferentFilename" /> is <see langword="false" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">Cancellation occurs before replacement and any requested reload finish.</exception>
    Task<BeatmapRestoreResult> RestoreAsync(
        string backupPath,
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Restores the newest retained file into the requested beatmap using the
    ///     same compatibility and safety rules as an explicit restore.
    /// </summary>
    /// <param name="destinationPath">The current beatmap that QuickUndo should replace.</param>
    /// <param name="allowDifferentFilename">Allows the globally newest backup to belong to different metadata.</param>
    /// <param name="reloadEditor">Requests an osu! reload after replacement.</param>
    /// <param name="cancellationToken">Cancels before lookup, validation, replacement, or reload.</param>
    /// <returns>The restore result, or <see langword="null" /> when no backup exists.</returns>
    /// <exception cref="DirectoryNotFoundException">The configured backup directory is unavailable.</exception>
    /// <exception cref="BeatmapBackupIncompatibleException">
    ///     The newest backup has different metadata and <paramref name="allowDifferentFilename" /> is <see langword="false" />
    ///     .
    /// </exception>
    /// <exception cref="OperationCanceledException">Cancellation occurs before lookup or restoration finishes.</exception>
    Task<BeatmapRestoreResult?> QuickUndoAsync(
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default);
}

