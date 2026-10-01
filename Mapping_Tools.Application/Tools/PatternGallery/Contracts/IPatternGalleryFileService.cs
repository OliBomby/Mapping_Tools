using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Core.Tools.PatternGallery.Models;

namespace Mapping_Tools.Application.Tools.PatternGallery.Contracts;

/// <summary>Abstracts collection-file operations from Pattern Gallery use cases.</summary>
public interface IPatternGalleryFileService
{
    /// <summary>Resolves a collection's root, pattern directory, and project file.</summary>
    /// <param name="basePath">The feature's collection root.</param>
    /// <param name="metadata">The persisted collection-folder identity.</param>
    /// <returns>Absolute paths for the collection.</returns>
    PatternGalleryCollectionPaths Resolve(string basePath, PatternGalleryCollectionMetadata metadata);

    /// <summary>Resolves one persisted pattern filename within a collection.</summary>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="fileName">The single-file pattern name.</param>
    /// <returns>The absolute path of the pattern file.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileName" /> is not a single relative filename.</exception>
    string GetPatternPath(PatternGalleryCollectionPaths paths, string fileName);

    /// <summary>Creates the collection and its Pattern Files directory.</summary>
    /// <param name="paths">The collection paths to create.</param>
    void EnsureCollection(PatternGalleryCollectionPaths paths);

    /// <summary>Returns pattern filenames currently present in the collection.</summary>
    /// <param name="paths">The collection paths to inspect.</param>
    /// <returns>`.osu` filenames ordered by the physical directory enumeration.</returns>
    IReadOnlyList<string> EnumeratePatternFiles(PatternGalleryCollectionPaths paths);

    /// <summary>Deletes a pattern file when it exists.</summary>
    /// <param name="path">The pattern path.</param>
    void DeletePattern(string path);

    /// <summary>Copies a source pattern file into a collection.</summary>
    /// <param name="sourcePath">The existing source file.</param>
    /// <param name="destinationPath">The new collection file.</param>
    void CopyPattern(string sourcePath, string destinationPath);

    /// <summary>Reads raw pattern bytes for ZIP export.</summary>
    /// <param name="path">The pattern file.</param>
    /// <returns>The exact source bytes.</returns>
    byte[] ReadPatternBytes(string path);

    /// <summary>Reads a collection file's bytes without treating a missing file as an error.</summary>
    /// <param name="path">The absolute file path.</param>
    /// <returns>The exact bytes, or null when the file does not exist.</returns>
    byte[]? ReadFileBytes(string path);

    /// <summary>Lists relative file paths inside a collection without reading their contents.</summary>
    /// <param name="paths">The collection to inspect.</param>
    /// <returns>Relative file paths.</returns>
    IReadOnlyList<string> EnumerateCollectionFiles(PatternGalleryCollectionPaths paths);

    /// <summary>Gets whether a collection directory currently exists.</summary>
    /// <param name="paths">The collection to inspect.</param>
    /// <returns>True when the collection directory exists.</returns>
    bool CollectionExists(PatternGalleryCollectionPaths paths);

    /// <summary>Removes a collection directory only when it contains no files.</summary>
    /// <param name="paths">The collection to remove.</param>
    void RemoveEmptyCollection(PatternGalleryCollectionPaths paths);

    /// <summary>Applies checked file changes without overwriting unexpected edits.</summary>
    /// <param name="paths">The collection containing the files.</param>
    /// <param name="changes">Changes recorded by one user edit.</param>
    /// <param name="undo">Whether to restore the original bytes.</param>
    void ApplyCollectionFileChanges(
        PatternGalleryCollectionPaths paths,
        IReadOnlyList<PatternGalleryFileChange> changes,
        bool undo);

    /// <summary>Writes raw pattern bytes for ZIP merge import.</summary>
    /// <param name="path">The destination pattern file.</param>
    /// <param name="bytes">The complete pattern file bytes.</param>
    /// <exception cref="IOException">Thrown when the destination already exists.</exception>
    void WritePatternBytes(string path, ReadOnlySpan<byte> bytes);

    /// <summary>Moves a collection directory while preserving its contents.</summary>
    /// <param name="paths">The current collection paths.</param>
    /// <param name="newCollectionFolderName">The new relative collection directory name.</param>
    /// <returns>The moved collection paths.</returns>
    PatternGalleryCollectionPaths RenameCollection(
        PatternGalleryCollectionPaths paths,
        string newCollectionFolderName);
}
