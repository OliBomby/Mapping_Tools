using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class ParameterCommandDecoder : IDecoder<ParameterCommand>
{
    public ParameterCommand Decode(string code)
    {
        var values = code.Split(',');

        if (!Enum.TryParse(values[1], out EasingType easingType))
            throw new BeatmapParsingException("Failed to parse easing of command.", code);

        if (!FileFormatHelper.TryParseInt(values[2], out int startTime))
            throw new BeatmapParsingException("Failed to parse start time of param command.", code);

        int endTime;
        if (string.IsNullOrEmpty(values[3]))
        {
            endTime = startTime;
        }
        else
        {
            if (!FileFormatHelper.TryParseInt(values[3], out endTime))
                throw new BeatmapParsingException("Failed to parse end time of param command.", code);
        }

        var parameter = values[4];

        return new ParameterCommand
        {
            Easing = easingType,
            StartTime = startTime,
            EndTime = endTime,
            Parameter = parameter
        };
    }
}