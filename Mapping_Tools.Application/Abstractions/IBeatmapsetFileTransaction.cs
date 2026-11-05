namespace Mapping_Tools.Application.Abstractions;

/// <summary>
///     Stages files and atomically applies a complete mapset output with
///     rollback support.
/// </summary>
public interface IBeatmapsetFileTransaction : IDisposable
{
    /// <summary>
    ///     Gets the temporary staging directory that holds uncommitted output.
    /// </summary>
    string StagingDirectory { get; }

    /// <summary>
    ///     Gets a writable staged path for a safe relative output path.
    /// </summary>
    /// <param name="relativePath">The output path relative to the export root.</param>
    /// <returns>A path that may be passed to the mapset file system.</returns>
    string GetStagedPath(string relativePath);

    /// <summary>
    ///     Copies one binary source file into the staged output.
    /// </summary>
    /// <param name="sourcePath">The existing source file.</param>
    /// <param name="relativePath">The output path relative to the export root.</param>
    /// <param name="cancellationToken">Cancels before and after the binary copy.</param>
    void CopyToStaging(
        string sourcePath,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Commits all staged files and restores prior output on failure.
    /// </summary>
    /// <param name="cancellationToken">Cancels between staged file replacements.</param>
    /// <param name="overwrite">Whether staged files may replace existing output files.</param>
    /// <param name="clearExisting">Whether to remove existing output files before committing; requires overwrite permission.</param>
    /// <returns>The final absolute paths of the committed files in deterministic order.</returns>
    Task<IReadOnlyList<string>> CommitAsync(
        CancellationToken cancellationToken = default,
        bool overwrite = true,
        bool clearExisting = false);

    /// <summary>
    ///     Discards staged output and restores an interrupted commit.
    /// </summary>
    void Rollback();
}
