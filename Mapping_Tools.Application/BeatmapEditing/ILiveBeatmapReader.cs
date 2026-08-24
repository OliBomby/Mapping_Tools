using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Reads the current osu! editor state while keeping process discovery and
///     memory-reading details outside the application layer.
/// </summary>
public interface ILiveBeatmapReader
{
    /// <summary>
    ///     Attempts to capture a validated editor snapshot.
    /// </summary>
    /// <param name="cancellationToken">Cancels process discovery or a pending memory read.</param>
    /// <returns>
    ///     The live snapshot, or <see langword="null" /> when osu! is not running or
    ///     no beatmap is open in its editor.
    /// </returns>
    Task<LiveBeatmapSnapshot?> ReadAsync(CancellationToken cancellationToken = default);
}

