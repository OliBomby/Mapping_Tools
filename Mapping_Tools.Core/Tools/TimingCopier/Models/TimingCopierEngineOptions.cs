using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.TimingCopier.Models;

/// <summary>
///     Stores the timing source, targets, resnapping mode, and beat divisors used by Timing Copier.
/// </summary>
public class TimingCopierEngineOptions
{
    /// <summary>
    ///     Gets or sets how target markers are positioned after timing is copied.
    /// </summary>
    public TimingCopierResnapMode ResnapMode { get; set; } = TimingCopierResnapMode.PreserveBeatSpacing;

    /// <summary>
    ///     Gets or sets the positive snap intervals expressed as fractions of one beat.
    /// </summary>
    public IBeatDivisor[] BeatDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors();
}
