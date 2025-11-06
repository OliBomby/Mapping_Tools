using Mapping_Tools.Domain.Beatmaps.IO;
using Mapping_Tools.Domain.Beatmaps.Parsing;
using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// Represents trigger loop events. Although called loops, these only ever activate once.
/// </summary>
public class TriggerLoop : Command, IHasDuration {
    public override EventType EventType => EventType.T;
    public double Duration => EndTime - StartTime;
    public double EndTime { get; set; }
    public string TriggerName { get; set; }
    public bool DurationDefined { get; set; }

    public override string GetLine() {
        return !DurationDefined
            ? $"{EventType},{TriggerName}"
            : $"{EventType},{TriggerName},{StartTime.ToRoundInvariant()},{EndTime.ToRoundInvariant()}";
    }

    public override void SetLine(string line) {
        var subLine = RemoveIndents(line);
        var values = subLine.Split(',');

        TriggerName = values[1];

        if (values.Length > 2) {
            DurationDefined = true;
            if (FileFormatHelper.TryParseDouble(values[2], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("Failed to parse start time of event param.", line);
        }
        else {
            DurationDefined = false;
        }

        if (values.Length > 3) {
            if (FileFormatHelper.TryParseDouble(values[3], out double endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse end time of event param.", line);
        }
    }
}