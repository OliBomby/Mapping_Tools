using Mapping_Tools.Application.PatternGallery;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Infrastructure.PatternGallery;

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
            || value.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
            throw new ArgumentException("The collection name must be one relative directory name.", parameterName);
    }
}
