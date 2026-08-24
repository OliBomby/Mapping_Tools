using Mapping_Tools.Core.Tools.MapsetMerger;

namespace Mapping_Tools.Application.Tools.MapsetMerger;

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

