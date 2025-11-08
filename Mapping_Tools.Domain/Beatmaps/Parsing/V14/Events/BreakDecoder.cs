using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class BreakDecoder : IDecoder<Break>
{
    public Break Decode(string code)
    {
        string[] values = code.Split(',');

        if (values[0] != "2" && values[0] != "Break")
            throw new BeatmapParsingException("This line is not a break.", code);

        if (!FileFormatHelper.TryParseDouble(values[1], out double startTime))
            throw new BeatmapParsingException("Failed to parse start time of break.", code);

        if (!FileFormatHelper.TryParseDouble(values[2], out double endTime))
            throw new BeatmapParsingException("Failed to parse end time of break.", code);

        return new Break
        {
            EventType = values[0],
            StartTime = startTime,
            EndTime = endTime,
        };
    }
}