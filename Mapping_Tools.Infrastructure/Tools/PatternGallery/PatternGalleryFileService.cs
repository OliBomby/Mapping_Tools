using Mapping_Tools.Application.Tools.PatternGallery.Contracts;
using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Core.Tools.PatternGallery.Models;

namespace Mapping_Tools.Infrastructure.Tools.PatternGallery;

/// <summary>Implements Pattern Gallery collection paths and local file operations.</summary>
public sealed class PatternGalleryFileService : IPatternGalleryFileService
{
    /// <inheritdoc />
    public PatternGalleryCollectionPaths Resolve(
        string basePath,
        PatternGalleryCollectionMetadata metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentNullException.ThrowIfNull(metadata);
        ValidateSegment(metadata.CollectionFolderName, nameof(metadata.CollectionFolderName));
        ValidateSegment(metadata.PatternFilesFolderName, nameof(metadata.PatternFilesFolderName));
        string root = Path.GetFullPath(basePath);
        string collection = Path.Combine(root, metadata.CollectionFolderName);
        string patternFiles = Path.Combine(collection, metadata.PatternFilesFolderName);
        return new PatternGalleryCollectionPaths(
            root,
            collection,
            patternFiles,
            Path.Combine(collection, "project.json"));
    }

    /// <inheritdoc />
    public string GetPatternPath(PatternGalleryCollectionPaths paths, string fileName)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ValidateSegment(fileName, nameof(fileName));
        return Path.Combine(paths.PatternFiles, fileName);
    }

    /// <inheritdoc />
    public void EnsureCollection(PatternGalleryCollectionPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        Directory.CreateDirectory(paths.PatternFiles);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> EnumeratePatternFiles(PatternGalleryCollectionPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (!Directory.Exists(paths.PatternFiles)) return [];

        return Directory.EnumerateFiles(paths.PatternFiles, "*.osu", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();
    }

    /// <inheritdoc />
    public void DeletePattern(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.Delete(path);
    }

    /// <inheritdoc />
    public void CopyPattern(string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, false);
    }

    /// <inheritdoc />
    public byte[] ReadPatternBytes(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.ReadAllBytes(path);
    }

    /// <inheritdoc />
    public byte[]? ReadFileBytes(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> EnumerateCollectionFiles(PatternGalleryCollectionPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (!Directory.Exists(paths.Collection)) return [];

        return Directory.EnumerateFiles(paths.Collection, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(paths.Collection, path)).ToArray();
    }

    /// <inheritdoc />
    public bool CollectionExists(PatternGalleryCollectionPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        return Directory.Exists(paths.Collection);
    }

    /// <inheritdoc />
    public void RemoveEmptyCollection(PatternGalleryCollectionPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (!Directory.Exists(paths.Collection)) return;
        if (Directory.EnumerateFiles(paths.Collection, "*", SearchOption.AllDirectories).Any())
            throw new IOException("The collection contains files that were not part of the undo operation.");

        Directory.Delete(paths.Collection, true);
    }

    /// <inheritdoc />
    public void ApplyCollectionFileChanges(
        PatternGalleryCollectionPaths paths,
        IReadOnlyList<PatternGalleryFileChange> changes,
        bool undo)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(changes);

        var operations = changes.Select(change =>
        {
            string path = Path.GetFullPath(Path.Combine(paths.Collection, change.RelativePath));
            string root = Path.GetFullPath(paths.Collection) + Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!path.StartsWith(root, comparison))
                throw new ArgumentException("The file change must remain inside its collection.", nameof(changes));

            byte[]? expected = undo ? change.After : change.Before;
            byte[]? desired = undo ? change.Before : change.After;
            byte[]? actual = File.Exists(path) ? File.ReadAllBytes(path) : null;
            if (!BytesEqual(actual, expected))
                throw new IOException($"The collection file '{change.RelativePath}' changed outside Mapping Tools.");

            return (path, desired);
        }).ToArray();

        foreach (var (path, desired) in operations)
        {
            if (desired is null)
            {
                File.Delete(path);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, desired);
        }
    }

    private static bool BytesEqual(byte[]? left, byte[]? right)
    {
        return left is null ? right is null : right is not null && left.AsSpan().SequenceEqual(right);
    }

    /// <inheritdoc />
    public void WritePatternBytes(string path, ReadOnlySpan<byte> bytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using FileStream stream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(bytes);
    }

    /// <inheritdoc />
    public PatternGalleryCollectionPaths RenameCollection(
        PatternGalleryCollectionPaths paths,
        string newCollectionFolderName)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ValidateSegment(newCollectionFolderName, nameof(newCollectionFolderName));
        string destination = Path.Combine(paths.Root, newCollectionFolderName);
        if (Directory.Exists(destination)) throw new IOException($"A collection with the name \"{newCollectionFolderName}\" already exists in {paths.Root}.");

        Directory.Move(paths.Collection, destination);
        string patternFolderName = Path.GetFileName(paths.PatternFiles);
        return new PatternGalleryCollectionPaths(
            paths.Root,
            destination,
            Path.Combine(destination, patternFolderName),
            Path.Combine(destination, Path.GetFileName(paths.ProjectFile)));
    }

    private static void ValidateSegment(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (Path.IsPathRooted(value)
            || value is "." or ".."
            || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || value.IndexOfAny(['/', '\\']) >= 0)
            throw new ArgumentException("The collection name must be one relative directory name.", parameterName);
    }
}
