using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Infrastructure.MapsetMerger;

namespace Mapping_Tools.Infrastructure.Files;

/// <summary>
///     Implements shared raw text, binary, and directory access for beatmapset
///     components on the local filesystem.
/// </summary>
public sealed class PhysicalBeatmapsetFileSystem : IBeatmapsetFileSystem
{
    /// <summary>
    ///     Creates a mapset file system backed by the local filesystem.
    /// </summary>
    public PhysicalBeatmapsetFileSystem()
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ReadAllLines(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.ReadAllLines(path);
    }

    /// <inheritdoc />
    public void WriteAllLines(string path, IEnumerable<string> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(lines);

        PhysicalAtomicFileWriter.WriteLines(
            path,
            lines,
            PhysicalAtomicFileWriter.Utf8WithoutBom,
            path.EndsWith(".osu", StringComparison.OrdinalIgnoreCase)
                ? "\r\n"
                : Environment.NewLine);
    }

    /// <inheritdoc />
    public void Delete(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.Delete(path);
    }

    /// <inheritdoc />
    public string GetParentFolder(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Directory.GetParent(path)?.FullName
               ?? throw new DirectoryNotFoundException($"Path '{path}' does not have a parent folder.");
    }

    /// <inheritdoc />
    public string CombinePath(string parent, string child)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parent);
        ArgumentException.ThrowIfNullOrWhiteSpace(child);
        return Path.Combine(parent, child);
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.Exists(path);
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Directory.Exists(path);
    }

    /// <inheritdoc />
    public string? GetParentDirectory(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return Path.GetDirectoryName(Path.GetFullPath(filePath));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> EnumerateFiles(
        string directory,
        string searchPattern,
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);
        if (!Enum.IsDefined(searchOption))
            throw new ArgumentOutOfRangeException(nameof(searchOption));

        return Directory
            .EnumerateFiles(directory, searchPattern, searchOption)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    /// <inheritdoc />
    public void EnsureDirectoryExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(path);
    }

    /// <inheritdoc />
    public byte[] ReadAllBytes(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.ReadAllBytes(path);
    }

    /// <inheritdoc />
    public void WriteAllBytes(string path, ReadOnlySpan<byte> bytes, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        using FileStream stream = new(
            path,
            overwrite ? FileMode.Create : FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);
        stream.Write(bytes);
    }

    /// <inheritdoc />
    public void CopyFile(
        string sourcePath,
        string destinationPath,
        bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        EnsureParentDirectory(destinationPath);
        File.Copy(sourcePath, destinationPath, overwrite);
    }

    /// <inheritdoc />
    public void MoveFile(
        string sourcePath,
        string destinationPath,
        bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        EnsureParentDirectory(destinationPath);
        File.Move(sourcePath, destinationPath, overwrite);
    }

    /// <inheritdoc />
    public IBeatmapsetFileTransaction BeginTransaction(string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
        return new PhysicalMapsetFileTransaction(targetDirectory);
    }

    private static void EnsureParentDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    }
}
