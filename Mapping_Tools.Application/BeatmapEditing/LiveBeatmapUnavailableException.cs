using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Reports that a caller required current editor state but it could not be
///     safely associated with the requested beatmap.
/// </summary>
public sealed class LiveBeatmapUnavailableException : Exception
{
    /// <summary>
    ///     Creates an availability error with a user-facing explanation.
    /// </summary>
    /// <param name="message">Why live editor state could not be supplied.</param>
    public LiveBeatmapUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Wraps the process or validation failure that prevented a live read.
    /// </summary>
    /// <param name="message">Why live editor state could not be supplied.</param>
    /// <param name="innerException">The low-level read or validation failure.</param>
    public LiveBeatmapUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
