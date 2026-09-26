using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>
///     Represents trigger loop events. Although called loops, these only ever activate once.
/// </summary>
public class TriggerLoop : Command, IHasEndTime
{
    /// <summary>
    ///     Gets the storyboard-trigger command token.
    /// </summary>
    public override EventType EventType => EventType.T;

    /// <summary>
    ///     Gets or sets the gameplay trigger expression controlling the nested commands.
    /// </summary>
    public string TriggerName { get; set; } = string.Empty;

    /// <inheritdoc />
    public double EndTime { get; set; }

}
