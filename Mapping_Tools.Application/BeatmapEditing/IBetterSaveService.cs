using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Saves the exact live osu! editor state through the mandatory backup gateway.
/// </summary>
public interface IBetterSaveService
{
    /// <summary>
    ///     Locates the current beatmap, requires matching live editor state, and saves it safely.
    /// </summary>
    /// <param name="cancellationToken">Cancels lookup, live reading, backup, or persistence.</param>
    /// <returns>A typed outcome; ordinary integration and persistence failures are captured.</returns>
    Task<BetterSaveResult> ExecuteAsync(CancellationToken cancellationToken = default);
}

