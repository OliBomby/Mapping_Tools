using Mapping_Tools.Application.MapsetMerger;

namespace Mapping_Tools.Infrastructure.MapsetMerger;

/// <summary>
/// Implements Mapset Merger's recursive source discovery and staged export on
/// the local filesystem.
/// </summary>
public sealed class PhysicalMapsetFileSystem : IMapsetFileSystem
{
    /// <inheritdoc/>
    public bool DirectoryExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Directory.Exists(path);
    }

    /// <inheritdoc/>
    public bool FileExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.Exists(path);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> EnumerateFiles(string directory, string searchPattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);
        return Directory
            .EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    /// <inheritdoc/>
    public IMapsetFileTransaction BeginTransaction(string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
        return new PhysicalMapsetFileTransaction(targetDirectory);
    }
}

internal sealed class PhysicalMapsetFileTransaction : IMapsetFileTransaction
{
    private readonly string _targetDirectory;
    private readonly string _rollbackDirectory;
    private readonly HashSet<string> _createdFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _createdDirectories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _backups = new(StringComparer.OrdinalIgnoreCase);
    private bool _committed;
    private bool _disposed;

    public PhysicalMapsetFileTransaction(string targetDirectory)
    {
        _targetDirectory = Path.GetFullPath(targetDirectory);
        string parent = Path.GetDirectoryName(_targetDirectory)
            ?? throw new ArgumentException("The export path has no parent directory.", nameof(targetDirectory));
        Directory.CreateDirectory(parent);
        StagingDirectory = Path.Combine(
            parent,
            $".{Path.GetFileName(_targetDirectory)}.mapset-merger-{Guid.NewGuid():N}");
        _rollbackDirectory = Path.Combine(StagingDirectory, ".rollback");
        Directory.CreateDirectory(StagingDirectory);
    }

    /// <inheritdoc/>
    public string StagingDirectory { get; }

    /// <inheritdoc/>
    public string GetStagedPath(string relativePath)
    {
        EnsureNotDisposed();
        string safePath = ResolveRelativePath(relativePath);
        string fullPath = Path.Combine(StagingDirectory, safePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        return fullPath;
    }

    /// <inheritdoc/>
    public void CopyToStaging(
        string sourcePath,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The referenced asset was not found.", sourcePath);
        }

        File.Copy(sourcePath, GetStagedPath(relativePath), overwrite: true);
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <inheritdoc/>
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        if (_committed)
        {
            return Task.CompletedTask;
        }

        try
        {
            foreach (string stagedPath in Directory.EnumerateFiles(
                         StagingDirectory,
                         "*",
                         SearchOption.AllDirectories)
                     .Where(path => !IsRollbackPath(path))
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(path => path, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = Path.GetRelativePath(StagingDirectory, stagedPath);
                string targetPath = Path.Combine(_targetDirectory, relativePath);
                EnsureOutputDirectory(Path.GetDirectoryName(targetPath)!);

                if (File.Exists(targetPath) &&
                    !_createdFiles.Contains(targetPath) &&
                    !_backups.ContainsKey(targetPath))
                {
                    string backupPath = Path.Combine(_rollbackDirectory, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                    File.Copy(targetPath, backupPath, overwrite: true);
                    _backups[targetPath] = backupPath;
                }
                else
                {
                    _createdFiles.Add(targetPath);
                }

                File.Copy(stagedPath, targetPath, overwrite: true);
            }

            DeleteStagingDirectory();
            _committed = true;
            return Task.CompletedTask;
        }
        catch
        {
            Rollback();
            throw;
        }
    }

    /// <inheritdoc/>
    public void Rollback()
    {
        if (_committed)
        {
            return;
        }

        foreach (string path in _createdFiles)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // Preserve the original failure when rollback itself cannot remove a file.
            }
            catch (UnauthorizedAccessException)
            {
                // Preserve the original failure when rollback itself cannot remove a file.
            }
        }

        foreach ((string targetPath, string backupPath) in _backups)
        {
            if (File.Exists(backupPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(backupPath, targetPath, overwrite: true);
            }
        }

        foreach (string directory in _createdDirectories
                     .OrderByDescending(path => path.Length)
                     .ThenBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (Directory.Exists(directory) &&
                    !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch (IOException)
            {
                // Preserve the original failure when rollback cannot remove an empty directory.
            }
            catch (UnauthorizedAccessException)
            {
                // Preserve the original failure when rollback cannot remove an empty directory.
            }
        }

        DeleteStagingDirectory();
        _createdFiles.Clear();
        _createdDirectories.Clear();
        _backups.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_committed)
        {
            Rollback();
        }
        else
        {
            DeleteStagingDirectory();
        }
    }

    private string ResolveRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath) ||
            relativePath.Split(['/', '\\']).Any(part => part is ".." or "."))
        {
            throw new ArgumentException("The staged path must be relative and normalized.", nameof(relativePath));
        }

        string fullPath = Path.GetFullPath(Path.Combine(StagingDirectory, relativePath));
        if (!fullPath.StartsWith(
                StagingDirectory + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The staged path escapes the transaction.", nameof(relativePath));
        }

        return relativePath;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void EnsureOutputDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            return;
        }

        List<string> missing = [];
        string? current = Path.GetFullPath(directory);
        while (!string.IsNullOrEmpty(current) && !Directory.Exists(current))
        {
            missing.Add(current);
            current = Path.GetDirectoryName(current);
        }

        foreach (string path in missing.AsEnumerable().Reverse())
        {
            Directory.CreateDirectory(path);
            _createdDirectories.Add(path);
        }
    }

    private void DeleteStagingDirectory()
    {
        if (Directory.Exists(StagingDirectory))
        {
            Directory.Delete(StagingDirectory, recursive: true);
        }
    }

    private bool IsRollbackPath(string path) =>
        string.Equals(path, _rollbackDirectory, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(
            _rollbackDirectory + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(
            _rollbackDirectory + Path.AltDirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
}
