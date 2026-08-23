using static Mapping_Tools.Core.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.Classes.BeatmapHelper.Events;

/// <summary>
///     Represents trigger loop events. Although called loops, these only ever activate once.
/// </summary>
#nullable disable
public class TriggerLoop : Command, IHasEndTime
{
    /// <summary>
    ///     Gets the storyboard-trigger command token.
    /// </summary>
    public override EventType EventType => EventType.T;

    /// <summary>
    ///     Gets or sets the gameplay trigger expression controlling the nested commands.
    /// </summary>
    public string TriggerName { get; set; }

    /// <summary>
    ///     <inheritdoc />
    public double EndTime { get; set; }

    /// <summary>
    ///     <inheritdoc />
    public override string GetLine()
    {
        return
            $"{EventType},{TriggerName},{(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant())},{(SaveWithFloatPrecision ? EndTime.ToInvariant() : EndTime.ToRoundInvariant())}";
    }

    /// <summary>
    ///     <inheritdoc />
    public override void SetLine(string line)
    {
        string subLine = RemoveIndents(line);
        string[] values = subLine.Split(',');

        TriggerName = values[1];

        if (TryParseDouble(values[2], out double startTime))
            StartTime = startTime;
        else throw new BeatmapParsingException("Failed to parse start time of event param.", line);

        if (TryParseDouble(values[3], out double endTime))
            EndTime = endTime;
        else throw new BeatmapParsingException("Failed to parse end time of event param.", line);
    }
}
