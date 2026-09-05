namespace Mapping_Tools.Application.Workspace.Contracts;

/// <summary>
///     Locates the beatmap currently open in osu! without exposing process-memory
///     or Editor Reader types to the workspace.
/// </summary>
public interface ICurrentBeatmapLocator
{
    /// <summary>
    ///     Resolves osu!'s current beatmap to a local file.
    /// </summary>
    /// <param name="cancellationToken">Cancels process discovery or editor-state reading.</param>
    /// <returns>The path of the beatmap currently open in osu!.</returns>
    /// <exception cref="InvalidOperationException">
    ///     osu! is unavailable or has no current beatmap.
    /// </exception>
    Task<string> FindCurrentBeatmapAsync(
        CancellationToken cancellationToken = default);
}
