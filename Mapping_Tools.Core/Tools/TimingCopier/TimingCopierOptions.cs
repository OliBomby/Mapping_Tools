using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.TimingCopier;

/// <summary>
///     Identifies how Timing Copier moves target map content after copying timing.
/// </summary>
public enum TimingCopierResnapMode
{
    /// <summary>
    ///     Moves markers so their beat distances remain stable, then snaps them.
    /// </summary>
    PreserveBeatSpacing,

    /// <summary>
    ///     Snaps movable map content to the copied timing without preserving beat distances.
    /// </summary>
    Resnap,

    /// <summary>
    ///     Replaces timing while leaving map content at its existing timestamps.
    /// </summary>
    KeepObjectsFixed,
}

/// <summary>
///     Stores the timing source, targets, resnapping mode, and beat divisors used by Timing Copier.
/// </summary>
public class TimingCopierOptions
{
    /// <summary>Gets or sets the beatmap whose timing is copied.</summary>
    public string ImportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets vertical-bar-separated beatmap targets.</summary>
    public string ExportPath { get; set; } = string.Empty;

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
