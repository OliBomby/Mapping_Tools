using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class OtherCommandDecoder : IDecoder<OtherCommand>
{
    public OtherCommand Decode(string code)
    {
        var values = code.Split(',');

        if (!Enum.TryParse(values[0], out CommandType eventType))
            throw new BeatmapParsingException("Failed to parse type of command.", code);

        if (!Enum.TryParse(values[1], out EasingType easingType))
            throw new BeatmapParsingException("Failed to parse easing of command.", code);

        if (!FileFormatHelper.TryParseDouble(values[2], out double startTime))
            throw new BeatmapParsingException("Failed to parse start time of command.", code);

        double endTime;
        if (string.IsNullOrEmpty(values[3]))
        {
            endTime = startTime;
        }
        else
        {
            if (!FileFormatHelper.TryParseDouble(values[3], out endTime))
                throw new BeatmapParsingException("Failed to parse end time of command.", code);
        }

        double[] parameters;
        if (values.Length <= 4 || string.IsNullOrWhiteSpace(values[4]))
        {
            parameters = [];
        }
        else
        {
            parameters = new double[values.Length - 4];
            for (int i = 4; i < values.Length; i++)
            {
                var stringValue = values[i];
                int index = i - 4;

                if (FileFormatHelper.TryParseDouble(stringValue, out double value))
                    parameters[index] = value;
                else
                    throw new BeatmapParsingException($"Failed to parse value at position {i} of command.", code);
            }
        }

        return new OtherCommand
        {
            CommandType = eventType,
            Easing = easingType,
            StartTime = startTime,
            EndTime = endTime,
            Params = parameters,
        };
    }
}