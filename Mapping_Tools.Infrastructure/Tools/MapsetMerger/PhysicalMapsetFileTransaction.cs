using Mapping_Tools.Application.Abstractions;

namespace Mapping_Tools.Infrastructure.Tools.MapsetMerger;

internal sealed class PhysicalMapsetFileTransaction : IBeatmapsetFileTransaction
{
    private readonly Dictionary<string, string> backups = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> createdDirectories = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> createdFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly string rollbackDirectory;
    private readonly string targetDirectory;
    private bool committed;
    private IReadOnlyList<string> outputFiles = [];
    private bool disposed;

    public PhysicalMapsetFileTransaction(string targetDirectory)
    {
        this.targetDirectory = Path.GetFullPath(targetDirectory);
        string parent = Path.GetDirectoryName(this.targetDirectory)
                        ?? throw new ArgumentException("The export path has no parent directory.", nameof(targetDirectory));
        Directory.CreateDirectory(parent);
        StagingDirectory = Path.Combine(
            parent,
            $".{Path.GetFileName(this.targetDirectory)}.mapset-merger-{Guid.NewGuid():N}");
        rollbackDirectory = Path.Combine(StagingDirectory, ".rollback");
        Directory.CreateDirectory(StagingDirectory);
    }

    /// <inheritdoc />
    public string StagingDirectory { get; }

    /// <inheritdoc />
    public string GetStagedPath(string relativePath)
    {
        EnsureNotDisposed();
        string safePath = ResolveRelativePath(relativePath);
        string fullPath = Path.Combine(StagingDirectory, safePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        return fullPath;
    }

    /// <inheritdoc />
    public void CopyToStaging(
        string sourcePath,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("The referenced asset was not found.", sourcePath);

        File.Copy(sourcePath, GetStagedPath(relativePath), true);
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> CommitAsync(
        CancellationToken cancellationToken = default,
        bool overwrite = true,
        bool clearExisting = false)
    {
        EnsureNotDisposed();
        if (committed) return Task.FromResult(outputFiles);

        if (clearExisting && (!overwrite || Path.GetPathRoot(targetDirectory) == targetDirectory))
            throw new ArgumentException("Clearing output files requires overwrite permission and a non-root destination.", nameof(clearExisting));

        string[] stagedFiles = Directory.EnumerateFiles(StagingDirectory, "*", SearchOption.AllDirectories)
            .Where(path => !IsRollbackPath(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (!overwrite)
            foreach (string stagedPath in stagedFiles)
            {
                string targetPath = Path.Combine(targetDirectory, Path.GetRelativePath(StagingDirectory, stagedPath));

                if (File.Exists(targetPath))
                    throw new IOException($"Output already exists: {targetPath}.");
            }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (clearExisting && Directory.Exists(targetDirectory))
                foreach (string path in Directory.EnumerateFiles(targetDirectory, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    BackupFile(path);
                    File.Delete(path);
                }

            foreach (string stagedPath in stagedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = Path.GetRelativePath(StagingDirectory, stagedPath);
                string targetPath = Path.Combine(targetDirectory, relativePath);
                EnsureOutputDirectory(Path.GetDirectoryName(targetPath)!);

                if (File.Exists(targetPath) && !createdFiles.Contains(targetPath) && !backups.ContainsKey(targetPath))
                {
                    if (!overwrite)
                        throw new IOException($"Output already exists: {targetPath}.");

                    BackupFile(targetPath);
                }

                File.Copy(stagedPath, targetPath, overwrite);

                if (!backups.ContainsKey(targetPath))
                    createdFiles.Add(targetPath);
            }

            DeleteStagingDirectory();
            committed = true;
        }
        catch
        {
            Rollback();
            throw;
        }

        outputFiles = stagedFiles.Select(stagedPath =>
            Path.Combine(targetDirectory, Path.GetRelativePath(StagingDirectory, stagedPath))).ToArray();

        return Task.FromResult(outputFiles);
    }

    private void BackupFile(string path)
    {
        string backupPath = Path.Combine(rollbackDirectory, Path.GetRelativePath(targetDirectory, path));
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        File.Copy(path, backupPath, true);
        backups[path] = backupPath;
    }

    /// <inheritdoc />
    public void Rollback()
    {
        if (committed) return;

        foreach (string path in createdFiles)
        {
            if (!File.Exists(path)) continue;

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

        foreach ((string targetPath, string backupPath) in backups)
            if (File.Exists(backupPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(backupPath, targetPath, true);
            }

        foreach (string directory in createdDirectories
                     .OrderByDescending(path => path.Length)
                     .ThenBy(path => path, StringComparer.OrdinalIgnoreCase))
            try
            {
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory);
            }
            catch (IOException)
            {
                // Preserve the original failure when rollback cannot remove an empty directory.
            }
            catch (UnauthorizedAccessException)
            {
                // Preserve the original failure when rollback cannot remove an empty directory.
            }

        DeleteStagingDirectory();
        createdFiles.Clear();
        createdDirectories.Clear();
        backups.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        if (!committed)
            Rollback();
        else
            DeleteStagingDirectory();
    }

    private string ResolveRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath) || relativePath.Split('/', '\\').Any(part => part is ".." or "."))
            throw new ArgumentException("The staged path must be relative and normalized.", nameof(relativePath));

        string fullPath = Path.GetFullPath(Path.Combine(StagingDirectory, relativePath));
        if (!fullPath.StartsWith(
                StagingDirectory + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The staged path escapes the transaction.", nameof(relativePath));

        return relativePath;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private void EnsureOutputDirectory(string directory)
    {
        if (Directory.Exists(directory)) return;

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
            createdDirectories.Add(path);
        }
    }

    private void DeleteStagingDirectory()
    {
        if (Directory.Exists(StagingDirectory)) Directory.Delete(StagingDirectory, true);
    }

    private bool IsRollbackPath(string path)
    {
        return string.Equals(path, rollbackDirectory, StringComparison.OrdinalIgnoreCase)
               || path.StartsWith(
                   rollbackDirectory + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase)
               || path.StartsWith(
                   rollbackDirectory + Path.AltDirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase);
    }
}
