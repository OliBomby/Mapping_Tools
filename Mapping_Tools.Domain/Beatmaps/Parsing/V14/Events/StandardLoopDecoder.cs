using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class StandardLoopDecoder : IDecoder<StandardLoop>
{
    public StandardLoop Decode(string code)
    {
        var values = code.Split(',');

        if (!FileFormatHelper.TryParseDouble(values[1], out double startTime))
            throw new BeatmapParsingException("Failed to parse start time of event param.", code);

        if (!FileFormatHelper.TryParseInt(values[2], out int loopCount))
            throw new BeatmapParsingException("Failed to parse loop count of event param.", code);

        return new StandardLoop
        {
            StartTime = startTime,
            LoopCount = loopCount,
        };
    }
}