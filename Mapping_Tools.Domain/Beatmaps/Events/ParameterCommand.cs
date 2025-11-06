using System.Text;
using Mapping_Tools.Domain.Beatmaps.IO;
using Mapping_Tools.Domain.Beatmaps.Parsing;
using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// Represents the parameter command. This event has a different syntax so it can't be a <see cref="OtherCommand"/>.
/// </summary>
public class ParameterCommand : Command, IHasDuration {
    public override EventType EventType => EventType.P;
    public EasingType Easing { get; set; }
    public double Duration => EndTime - StartTime;
    public double EndTime { get; set; }
    public string Parameter { get; set; }

    public override string GetLine() {
        var builder = new StringBuilder(9);

        builder.Append(EventType.ToString());
        builder.Append(',');
        builder.Append(((int) Easing).ToInvariant());
        builder.Append(',');
        builder.Append(StartTime.ToInvariant());
        builder.Append(',');
        if (!Precision.AlmostEquals(StartTime, EndTime)) {
            builder.Append(EndTime.ToInvariant());
        }

        builder.Append(',');
        builder.Append(Parameter);

        return builder.ToString();
    }

    public override void SetLine(string line) {
        var subLine = RemoveIndents(line);
        var values = subLine.Split(',');

        if (Enum.TryParse(values[1], out EasingType easingType))
            Easing = easingType;
        else throw new BeatmapParsingException("Failed to parse easing of command.", line);

        if (FileFormatHelper.TryParseInt(values[2], out int startTime))
            StartTime = startTime;
        else throw new BeatmapParsingException("Failed to parse start time of param command.", line);

        // Set end time to start time if empty. This accounts for the shorthand
        if (string.IsNullOrEmpty(values[3])) {
            EndTime = StartTime;
        }
        else {
            if (FileFormatHelper.TryParseInt(values[3], out int endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse end time of param command.", line);
        }

        Parameter = values[4];
    }
}