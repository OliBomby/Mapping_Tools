using System.Text;
using Mapping_Tools.Application.Backups;

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
        string temporaryPath = CreateTemporarySibling(destinationPath);
        try
        {
            await using (FileStream source = new(
                             sourcePath,
                             FileMode.Open,
                             FileAccess.Read,
                             FileShare.Read,
                             81920,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (FileStream destination = new(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await source.CopyToAsync(destination, cancellationToken)
                    .ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, destinationPath, true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    /// <inheritdoc />
    public async Task WriteLinesAsync(
        string destinationPath,
        IReadOnlyList<string> lines,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentNullException.ThrowIfNull(lines);
        string temporaryPath = CreateTemporarySibling(destinationPath);
        try
        {
            await using (FileStream stream = new(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            await using (StreamWriter writer = new(
                             stream,
                             new UTF8Encoding(false)))
            {
                writer.NewLine = "\r\n";

                foreach (string line in lines)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.WriteLineAsync(
                            line.AsMemory(),
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, destinationPath, true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
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

    private static string CreateTemporarySibling(string destinationPath)
    {
        string directory = Path.GetDirectoryName(destinationPath)
                           ?? throw new DirectoryNotFoundException(
                               $"Path '{destinationPath}' does not have a parent directory.");
        string fileName = Path.GetFileName(destinationPath);
        return Path.Combine(
            directory,
            $".mapping-tools-{fileName}-{Guid.NewGuid():N}.tmp");
    }
}
