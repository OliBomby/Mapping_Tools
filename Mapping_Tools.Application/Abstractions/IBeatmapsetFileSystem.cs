namespace Mapping_Tools.Application.Abstractions;

/// <summary>
///     Provides raw text, binary, and directory operations for beatmapset
///     components, including the transaction used by complete mapset exports.
/// </summary>
public interface IBeatmapsetFileSystem : ITextFileStore
{
    /// <summary>
    ///     Determines whether a path currently resolves to a physical file.
    /// </summary>
    /// <param name="path">The mapset component path to inspect.</param>
    /// <returns><see langword="true" /> only when the file exists.</returns>
    bool FileExists(string path);

    /// <summary>
    ///     Determines whether a path currently resolves to a physical directory.
    /// </summary>
    /// <param name="path">The mapset directory path to inspect.</param>
    /// <returns><see langword="true" /> only when the directory exists.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    ///     Resolves the directory containing a mapset component path.
    /// </summary>
    /// <param name="filePath">The file path whose containing directory is required.</param>
    /// <returns>The containing directory, or <see langword="null" /> when it has none.</returns>
    string? GetParentDirectory(string filePath);

    /// <summary>
    ///     Enumerates matching mapset files in deterministic path order.
    /// </summary>
    /// <param name="directory">The directory whose files are inspected.</param>
    /// <param name="searchPattern">The filename pattern, such as <c>*.osu</c>.</param>
    /// <param name="searchOption">Whether nested directories are included.</param>
    /// <returns>The matching paths ordered case-insensitively, then ordinally.</returns>
    IReadOnlyList<string> EnumerateFiles(
        string directory,
        string searchPattern,
        SearchOption searchOption = SearchOption.TopDirectoryOnly);

    /// <summary>
    ///     Creates a mapset directory and any missing parents.
    /// </summary>
    /// <param name="path">The directory to create.</param>
    void EnsureDirectoryExists(string path);

    /// <summary>
    ///     Reads a mapset component as its exact binary contents.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <returns>The complete file contents.</returns>
    byte[] ReadAllBytes(string path);

    /// <summary>
    ///     Writes exact binary contents to a mapset component.
    /// </summary>
    /// <param name="path">The destination file.</param>
    /// <param name="bytes">The complete contents to write.</param>
    /// <param name="overwrite">Whether an existing destination may be replaced.</param>
    void WriteAllBytes(string path, ReadOnlySpan<byte> bytes, bool overwrite = false);

    /// <summary>
    ///     Copies one mapset component and optionally replaces its destination.
    /// </summary>
    /// <param name="sourcePath">The existing source file.</param>
    /// <param name="destinationPath">The destination file.</param>
    /// <param name="overwrite">Whether an existing destination may be replaced.</param>
    void CopyFile(string sourcePath, string destinationPath, bool overwrite = false);

    /// <summary>
    ///     Moves one mapset component and optionally replaces its destination.
    /// </summary>
    /// <param name="sourcePath">The existing source file.</param>
    /// <param name="destinationPath">The destination file.</param>
    /// <param name="overwrite">Whether an existing destination may be replaced.</param>
    void MoveFile(string sourcePath, string destinationPath, bool overwrite = false);

    /// <summary>
    ///     Starts a transaction that stages changes for one mapset directory.
    /// </summary>
    /// <param name="targetDirectory">The final export directory.</param>
    /// <returns>A disposable transaction that rolls back until committed.</returns>
    IBeatmapsetFileTransaction BeginTransaction(string targetDirectory);
}
