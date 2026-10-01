using Mapping_Tools.Application.Tools.PatternGallery.Contracts;
using Mapping_Tools.Application.Tools.PatternGallery.Models;

namespace Mapping_Tools.Application.Tools.PatternGallery;

/// <summary>Records only the files touched by a collection edit for later replay.</summary>
public sealed class PatternGalleryFileEdit : IDisposable
{
    private readonly IPatternGalleryFileService files;
    private readonly Action<PatternGalleryCollectionChange> recordChange;
    private readonly IReadOnlyList<CollectionBefore> collections;
    private bool disposed;

    /// <summary>Starts recording edits in the specified collections.</summary>
    /// <param name="files">Reads collection files through the filesystem adapter.</param>
    /// <param name="recordChange">Receives each changed collection when the edit completes.</param>
    /// <param name="paths">The collections the operation can change.</param>
    public PatternGalleryFileEdit(
        IPatternGalleryFileService files,
        Action<PatternGalleryCollectionChange> recordChange,
        params PatternGalleryCollectionPaths[] paths)
    {
        this.files = files ?? throw new ArgumentNullException(nameof(files));
        this.recordChange = recordChange ?? throw new ArgumentNullException(nameof(recordChange));
        collections = paths.Distinct().Select(path => new CollectionBefore(
            path, files.CollectionExists(path), new Dictionary<string, byte[]?>(PathComparer))).ToArray();
    }

    /// <summary>Records a file's original bytes before a use case changes it.</summary>
    /// <param name="path">The absolute path of a file in one of this edit's collections.</param>
    public void CaptureFile(string path)
    {
        string absolutePath = Path.GetFullPath(path);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var collection = collections.FirstOrDefault(collection => absolutePath.StartsWith(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(collection.Paths.Collection)) + Path.DirectorySeparatorChar,
            comparison)) ?? throw new ArgumentException("The file must belong to a recorded collection.", nameof(path));

        string relativePath = Path.GetRelativePath(collection.Paths.Collection, absolutePath);
        if (!collection.Files.ContainsKey(relativePath))
            collection.Files.Add(relativePath, files.ReadFileBytes(absolutePath));
    }

    /// <summary>Completes the edit and reports file or directory changes.</summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        foreach (var before in collections)
        {
            bool afterExists = files.CollectionExists(before.Paths);
            var changes = before.Files.Select(pair => new PatternGalleryFileChange(
                    pair.Key, pair.Value, files.ReadFileBytes(Path.Combine(before.Paths.Collection, pair.Key))))
                .Where(change => !BytesEqual(change.Before, change.After))
                .ToArray();

            if (changes.Length > 0 || before.Exists != afterExists)
                recordChange(new PatternGalleryCollectionChange(files, before.Paths, changes, before.Exists, afterExists));
        }
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static bool BytesEqual(byte[]? left, byte[]? right)
    {
        return left is null ? right is null : right is not null && left.AsSpan().SequenceEqual(right);
    }

    private sealed record CollectionBefore(
        PatternGalleryCollectionPaths Paths,
        bool Exists,
        Dictionary<string, byte[]?> Files);
}
