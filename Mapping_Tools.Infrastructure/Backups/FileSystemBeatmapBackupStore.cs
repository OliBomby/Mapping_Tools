using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Infrastructure.Files;

namespace Mapping_Tools.Infrastructure.Backups;

/// <summary>
///     Persists backup copies with sibling temporary files so cancellation or an
///     interrupted write cannot leave a partially replaced beatmap.
/// </summary>
public sealed class FileSystemBeatmapBackupStore : IBeatmapBackupStore
{
    /// <inheritdoc />
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    /// <inheritdoc />
    public string GetFileName(string path)
    {
        return Path.GetFileName(path);
    }

    /// <inheritdoc />
    public string Combine(string directory, string fileName)
    {
        return Path.Combine(directory, fileName);
    }

    /// <inheritdoc />
    public async Task CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        await PhysicalAtomicFileWriter
            .CopyAsync(sourcePath, destinationPath, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task WriteLinesAsync(
        string destinationPath,
        IReadOnlyList<string> lines,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentNullException.ThrowIfNull(lines);
        await PhysicalAtomicFileWriter
            .WriteLinesAsync(
                destinationPath,
                lines,
                PhysicalAtomicFileWriter.Utf8WithoutBom,
                "\r\n",
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<StoredBeatmapBackup>> ListAsync(
        string directory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<StoredBeatmapBackup> backups = new DirectoryInfo(directory)
            .GetFiles()
            .Where(file => !file.Name.Contains(".mapping-tools-", StringComparison.Ordinal))
            .OrderByDescending(file => file.CreationTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.Ordinal)
            .Select(file => new StoredBeatmapBackup(
                file.FullName,
                new DateTimeOffset(file.CreationTimeUtc, TimeSpan.Zero)))
            .ToArray();
        return Task.FromResult(backups);
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        File.Delete(path);
        return Task.CompletedTask;
    }

}
