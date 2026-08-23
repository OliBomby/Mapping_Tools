using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Application.PatternGallery;

/// <summary>Describes the physical folders owned by one Pattern Gallery collection.</summary>
public sealed record PatternGalleryCollectionPaths(
    string Root,
    string Collection,
    string PatternFiles,
    string ProjectFile);

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

/// <summary>One pattern file included in a collection archive.</summary>
public sealed record PatternGalleryArchiveFile(string FileName, byte[] Content);

/// <summary>In-memory representation of a validated Pattern Gallery ZIP file.</summary>
public sealed record PatternGalleryArchive(
    string CollectionFolderName,
    string ProjectFileName,
    string ProjectJson,
    IReadOnlyList<PatternGalleryArchiveFile> PatternFiles);

/// <summary>Abstracts safe ZIP creation, reading, and extraction.</summary>
public interface IPatternGalleryArchiveService
{
    /// <summary>Creates a collection ZIP with a project JSON entry and pattern files.</summary>
    /// <param name="archivePath">The destination ZIP path.</param>
    /// <param name="collectionFolderName">The root folder inside the archive.</param>
    /// <param name="projectFileName">The project JSON filename inside the root folder.</param>
    /// <param name="projectJson">The serialized project document.</param>
    /// <param name="patternFiles">The pattern files to include.</param>
    /// <param name="cancellationToken">Cancels before the archive is committed.</param>
    Task ExportAsync(
        string archivePath,
        string collectionFolderName,
        string projectFileName,
        string projectJson,
        IReadOnlyList<PatternGalleryArchiveFile> patternFiles,
        CancellationToken cancellationToken = default);

    /// <summary>Reads and validates a collection ZIP without extracting it.</summary>
    /// <param name="archivePath">The existing ZIP path.</param>
    /// <param name="cancellationToken">Cancels between entry reads.</param>
    /// <returns>The project entry and `.osu` files in the archive.</returns>
    Task<PatternGalleryArchive> ReadAsync(
        string archivePath,
        CancellationToken cancellationToken = default);

    /// <summary>Extracts a validated archive below the supplied collection root.</summary>
    /// <param name="archivePath">The existing ZIP path.</param>
    /// <param name="basePath">The directory below which the archive root is created.</param>
    /// <param name="cancellationToken">Cancels before the next entry is written.</param>
    Task ExtractAsync(
        string archivePath,
        string basePath,
        CancellationToken cancellationToken = default);
}

/// <summary>Reports one completed Pattern Gallery placement.</summary>
public sealed record PatternGalleryRunResult(int PatternCount, string Message);

/// <summary>Reports a collection restore's indexed-file changes.</summary>
public sealed record PatternGalleryRestoreResult(int RemovedCount, int AddedCount);

/// <summary>Loads pattern data, imports patterns, restores collections, and places patterns.</summary>
public interface IPatternGalleryService
{
    /// <summary>Loads a stored pattern beatmap for presentation.</summary>
    /// <param name="pattern">The indexed pattern to load.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels the beatmap read.</param>
    /// <returns>The loaded beatmap with stacking information updated.</returns>
    Task<Beatmap> LoadBeatmapAsync(
        PatternGalleryPattern pattern,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);

    /// <summary>Imports raw object and timing-point text as a new pattern file.</summary>
    /// <param name="name">The display name.</param>
    /// <param name="hitObjectText">Newline-separated osu! hit-object lines.</param>
    /// <param name="timingPointText">Newline-separated osu! timing-point lines.</param>
    /// <param name="globalSv">The source global slider multiplier.</param>
    /// <param name="gameMode">The source game mode.</param>
    /// <param name="project">The collection receiving the pattern.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels parsing or writing.</param>
    /// <returns>The new indexed pattern.</returns>
    Task<PatternGalleryPattern> ImportCodeAsync(
        string name,
        string hitObjectText,
        string timingPointText,
        double globalSv,
        GameMode gameMode,
        PatternGalleryProject project,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);

    /// <summary>Imports a beatmap file, retaining only the requested objects when configured.</summary>
    /// <param name="sourcePath">The source `.osu` file.</param>
    /// <param name="name">The display name.</param>
    /// <param name="filter">An optional legacy time-code query.</param>
    /// <param name="startTime">An optional lower time bound.</param>
    /// <param name="endTime">An optional upper time bound.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels reading or writing.</param>
    /// <returns>The new indexed pattern.</returns>
    Task<PatternGalleryPattern> ImportFileAsync(
        string sourcePath,
        string name,
        string? filter,
        double startTime,
        double endTime,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);

    /// <summary>Imports the hit objects selected in the current live editor state.</summary>
    /// <param name="sourcePath">The beatmap expected to be open in osu!.</param>
    /// <param name="name">The display name.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels live reading or writing.</param>
    /// <returns>The new indexed pattern.</returns>
    Task<PatternGalleryPattern> ImportSelectedAsync(
        string sourcePath,
        string name,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);

    /// <summary>Places selected patterns into one target beatmap and saves it safely.</summary>
    /// <param name="targetPath">The beatmap to edit.</param>
    /// <param name="patterns">Selected pattern metadata.</param>
    /// <param name="project">The complete placement option snapshot.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="quick">Whether editor reload is requested after saving.</param>
    /// <param name="progress">Receives zero-to-one-hundred progress.</param>
    /// <param name="cancellationToken">Cancels before or between placements.</param>
    /// <returns>The successful placement count and legacy completion message.</returns>
    Task<PatternGalleryRunResult> ExportAsync(
        string targetPath,
        IReadOnlyList<PatternGalleryPattern> patterns,
        PatternGalleryProject project,
        PatternGalleryCollectionPaths paths,
        bool quick,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes selected metadata and their physical pattern files.</summary>
    /// <param name="patterns">Patterns to remove.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels before deletion.</param>
    Task DeleteAsync(
        IReadOnlyList<PatternGalleryPattern> patterns,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);

    /// <summary>Reconciles indexed metadata with the physical Pattern Files directory.</summary>
    /// <param name="project">The collection to modify.</param>
    /// <param name="paths">The resolved collection paths.</param>
    /// <param name="cancellationToken">Cancels between pattern reads.</param>
    /// <returns>The number of removed and newly indexed files.</returns>
    Task<PatternGalleryRestoreResult> RestoreAsync(
        PatternGalleryProject project,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default);
}
