using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>
///     Represents the standard loop event. This event has a different syntax so it can't be a <see cref="OtherCommand" />.
/// </summary>
public class StandardLoop : Command
{
    /// <summary>
    ///     Gets the standard-loop command token.
    /// </summary>
    public override EventType EventType => EventType.L;

    /// <summary>
    ///     Gets or sets how many times the nested command group repeats.
    /// </summary>
    public int LoopCount { get; set; }

}
