using Mapping_Tools.Application.Backups.Models;

namespace Mapping_Tools.Application.Backups.Contracts;

/// <summary>
///     Supplies filesystem operations and creation timestamps needed for backup
///     retention without exposing <see cref="FileInfo" /> or <see cref="DirectoryInfo" />
///     to application orchestration.
/// </summary>
public interface IBeatmapBackupStore
{
    /// <summary>
    ///     Checks whether a source or restore destination currently names a physical file.
    /// </summary>
    /// <param name="path">The candidate file path.</param>
    /// <returns><see langword="true" /> only for an existing file.</returns>
    bool FileExists(string path);

    /// <summary>
    ///     Checks whether the configured backup destination is available before a destructive action.
    /// </summary>
    /// <param name="path">The candidate directory path.</param>
    /// <returns><see langword="true" /> only for an existing directory.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    ///     Extracts the final filename using the host platform's path rules.
    /// </summary>
    /// <param name="path">A path whose leaf component is required.</param>
    /// <returns>The filename including its extension.</returns>
    string GetFileName(string path);

    /// <summary>
    ///     Joins a backup directory and generated filename using host path semantics.
    /// </summary>
    /// <param name="directory">The configured backup root.</param>
    /// <param name="fileName">A single generated backup filename.</param>
    /// <returns>The full destination path.</returns>
    string Combine(string directory, string fileName);

    /// <summary>
    ///     Copies a durable source into a backup or restores a backup over its destination.
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
    ///     Serializes an in-memory editor snapshot directly into the backup directory.
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
    ///     Enumerates retained files newest first according to filesystem creation time.
    /// </summary>
    /// <param name="directory">The configured backup root.</param>
    /// <param name="cancellationToken">Cancels before enumeration begins.</param>
    /// <returns>An ordered snapshot suitable for QuickUndo and retention pruning.</returns>
    Task<IReadOnlyList<StoredBeatmapBackup>> ListAsync(
        string directory,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes one retention candidate after newer backups have been secured.
    /// </summary>
    /// <param name="path">The exact backup selected for pruning.</param>
    /// <param name="cancellationToken">Cancels before deletion.</param>
    /// <returns>A task that completes when the file no longer exists.</returns>
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}

