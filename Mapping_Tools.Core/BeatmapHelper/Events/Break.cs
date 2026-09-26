using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>
///     Represents a gameplay break interval from the beatmap events section.
/// </summary>
public class Break : Event, IHasStartTime, IHasEndTime
{
    /// <summary>
    ///     Creates an uninitialized break event for property-based construction.
    /// </summary>
    public Break() { }


    /// <summary>
    ///     Gets or sets the original break token, preserving <c>2</c> or <c>Break</c>.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <inheritdoc />
    public double EndTime { get; set; }

    /// <inheritdoc />
    public double StartTime { get; set; }

}
