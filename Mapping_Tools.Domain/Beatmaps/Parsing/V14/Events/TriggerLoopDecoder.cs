using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class TriggerLoopDecoder : IDecoder<TriggerLoop>
{
    public TriggerLoop Decode(string code)
    {
        var values = code.Split(',');

        var triggerName = values[1];

        bool durationDefined = false;
        double startTime = 0;
        if (values.Length > 2)
        {
            durationDefined = true;
            if (!FileFormatHelper.TryParseDouble(values[2], out startTime))
                throw new BeatmapParsingException("Failed to parse start time of event param.", code);
        }

        double endTime = 0;
        if (values.Length > 3)
        {
            if (!FileFormatHelper.TryParseDouble(values[3], out endTime))
                throw new BeatmapParsingException("Failed to parse end time of event param.", code);
        }

        return new TriggerLoop
        {
            TriggerName = triggerName,
            DurationDefined = durationDefined,
            StartTime = startTime,
            EndTime = endTime
        };
    }
}