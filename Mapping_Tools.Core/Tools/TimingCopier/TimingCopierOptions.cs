using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.TimingCopier;

/// <summary>
///     Names the object-placement modes supported by Timing Copier.
/// </summary>
public static class TimingCopierResnapModes
{
    /// <summary>
    ///     Moves markers so their beat distances remain stable, then snaps them.
    /// </summary>
    public const string PRESERVE_BEAT_SPACING =
        "Number of beats between objects stays the same";

    /// <summary>
    ///     Snaps movable map content to the copied timing without preserving beat distances.
    /// </summary>
    public const string RESNAP = "Just resnap";

    /// <summary>
    ///     Replaces timing while leaving map content at its existing timestamps.
    /// </summary>
    public const string KEEP_OBJECTS_FIXED = "Don't move objects";
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
    ///     Gets or sets the legacy mode label controlling which target markers move.
    /// </summary>
    public string ResnapMode { get; set; } = TimingCopierResnapModes.PRESERVE_BEAT_SPACING;

    /// <summary>
    ///     Gets or sets the positive snap intervals expressed as fractions of one beat.
    /// </summary>
    public IBeatDivisor[] BeatDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors();
}
