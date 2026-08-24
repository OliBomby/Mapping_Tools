using Mapping_Tools.Core.Tools.MapsetMerger;

namespace Mapping_Tools.Application.Tools.MapsetMerger;

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
