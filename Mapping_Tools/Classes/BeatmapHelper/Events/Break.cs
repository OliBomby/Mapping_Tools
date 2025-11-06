using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events;

public class Break : Event, IHasStartTime, IHasEndTime {
    public string EventType { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }

    public Break() { }

    public Break(string line) {
        SetLine(line);
    }

    public override string GetLine() {
        return $"{EventType},{(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant())},{(SaveWithFloatPrecision ? EndTime.ToInvariant() : EndTime.ToRoundInvariant())}";
    }

    public override sealed void SetLine(string line) {
        string[] values = line.Split(',');

        // Either 'Break' or '2' indicates a break. We save the value so we dont accidentally change it.
        if (values[0] != "2" && values[0] != "Break") {
            throw new BeatmapParsingException("This line is not a break.", line);
        }

        EventType = values[0];

        if (TryParseDouble(values[1], out double startTime))
            StartTime = startTime;
        else throw new BeatmapParsingException("Failed to parse start time of break.", line);

        if (TryParseDouble(values[2], out double endTime))
            EndTime = endTime;
        else throw new BeatmapParsingException("Failed to parse end time of break.", line);
    }
}