using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.TimingHelper;

/// <summary>
///     Stores the marker sources and timing rules used by Timing Helper.
/// </summary>
public class TimingHelperOptions
{
    /// <summary>Gets or sets whether hit objects are used as timing markers.</summary>
    public bool Objects { get; set; } = true;

    /// <summary>Gets or sets whether editor bookmarks are used as timing markers.</summary>
    public bool Bookmarks { get; set; } = true;

    /// <summary>Gets or sets whether inherited timing points are used as markers.</summary>
    public bool Greenlines { get; set; } = true;

    /// <summary>Gets or sets whether red timing points are used as markers and retained during processing.</summary>
    public bool Redlines { get; set; } = true;

    /// <summary>Gets or sets whether inserted redlines omit their first barline.</summary>
    public bool OmitBarline { get; set; }

    /// <summary>Gets or sets the maximum marker timing error tolerated in milliseconds.</summary>
    public double Leniency { get; set; } = 3;

    /// <summary>
    ///     Gets or sets the requested beat distance between markers, or <c>-1</c>
    ///     to infer the distance from the existing marker positions.
    /// </summary>
    public double BeatsBetween { get; set; } = -1;

    /// <summary>Gets or sets the beat fractions considered while resnapping markers.</summary>
    public IBeatDivisor[] BeatDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors();
}
