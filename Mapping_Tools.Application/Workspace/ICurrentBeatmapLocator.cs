namespace Mapping_Tools.Application.Workspace;

/// <summary>
///     Locates the beatmap currently open in osu! without exposing process-memory
///     or Editor Reader types to the workspace.
/// </summary>
public interface ICurrentBeatmapLocator
{
    /// <summary>
    ///     Attempts to resolve osu!'s current beatmap to a local file.
    /// </summary>
    /// <param name="cancellationToken">Cancels process discovery or editor-state reading.</param>
    /// <returns>
    ///     The candidate local path, or <see langword="null" /> when osu! or its
    ///     current beatmap cannot be determined.
    /// </returns>
    Task<string?> FindCurrentBeatmapAsync(
        CancellationToken cancellationToken = default);
}
