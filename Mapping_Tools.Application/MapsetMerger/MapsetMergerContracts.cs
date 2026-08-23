using Mapping_Tools.Core.Tools.MapsetMerger;

namespace Mapping_Tools.Application.MapsetMerger;

/// <summary>
///     The serializable Mapset Merger project state, retaining the former WPF
///     property names for automatic recovery and project compatibility.
/// </summary>
public sealed class MapsetMergerProject : MapsetMergerOptions
{
    /// <summary>Gets or sets the destination directory for merged files.</summary>
    public string ExportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the source mapsets in their merge order.</summary>
    public List<MapsetItem> Mapsets { get; set; } = [];

    /// <summary>One persisted source mapset entry.</summary>
    public sealed class MapsetItem
    {
        /// <summary>Gets or sets the output folder and reference prefix.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the source mapset directory.</summary>
        public string Path { get; set; } = string.Empty;
    }
}

/// <summary>Reports the files emitted by one successful merge.</summary>
/// <param name="MapsetsMerged">Number of source mapsets processed.</param>
/// <param name="BeatmapsWritten">Number of merged <c>.osu</c> files written.</param>
/// <param name="StoryboardsWritten">Number of external <c>.osb</c> files written.</param>
/// <param name="AssetsCopied">Number of binary asset files copied.</param>
public sealed record MapsetMergerResult(
    int MapsetsMerged,
    int BeatmapsWritten,
    int StoryboardsWritten,
    int AssetsCopied);

/// <summary>
///     Runs Mapset Merger against disk-only source documents and an export transaction.
/// </summary>
public interface IMapsetMergerService
{
    /// <summary>
    ///     Reads all requested source mapsets, rewrites document references, stages
    ///     every output, and commits only after the complete export succeeds.
    /// </summary>
    /// <param name="project">The validated merge project.</param>
    /// <param name="progress">Optional aggregate percentage reporting.</param>
    /// <param name="cancellationToken">Cancels parsing, staging, or commit.</param>
    /// <returns>Counts for the committed output.</returns>
    Task<MapsetMergerResult> MergeAsync(
        MapsetMergerProject project,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Abstracts directory enumeration and transactional binary output for Mapset Merger.
/// </summary>
public interface IMapsetFileSystem
{
    /// <summary>Gets whether a local directory exists.</summary>
    /// <param name="path">The directory path.</param>
    bool DirectoryExists(string path);

    /// <summary>Gets whether a local file exists.</summary>
    /// <param name="path">The file path.</param>
    bool FileExists(string path);

    /// <summary>Enumerates matching files recursively in deterministic order.</summary>
    /// <param name="directory">The source directory.</param>
    /// <param name="searchPattern">The filename pattern, such as <c>*.osu</c>.</param>
    IReadOnlyList<string> EnumerateFiles(string directory, string searchPattern);

    /// <summary>Starts a transaction that stages changes for one export directory.</summary>
    /// <param name="targetDirectory">The final export directory.</param>
    /// <returns>A disposable transaction that rolls back until committed.</returns>
    IMapsetFileTransaction BeginTransaction(string targetDirectory);
}

/// <summary>Stages files and atomically applies an export with rollback support.</summary>
public interface IMapsetFileTransaction : IDisposable
{
    /// <summary>Gets the temporary staging directory.</summary>
    string StagingDirectory { get; }

    /// <summary>Gets a writable staged path for a safe relative output path.</summary>
    /// <param name="relativePath">The output path relative to the export root.</param>
    /// <returns>A path that may be passed to the text-file store.</returns>
    string GetStagedPath(string relativePath);

    /// <summary>Copies one binary source file into the staged output.</summary>
    /// <param name="sourcePath">The existing source file.</param>
    /// <param name="relativePath">The output path relative to the export root.</param>
    /// <param name="cancellationToken">Cancels before and after the binary copy.</param>
    void CopyToStaging(
        string sourcePath,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>Commits all staged files and restores prior output on failure.</summary>
    /// <param name="cancellationToken">Cancels between staged file replacements.</param>
    /// <returns>A task completed after the commit is durable.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Discards staged output and restores an interrupted commit.</summary>
    void Rollback();
}
