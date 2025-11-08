using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class BackgroundColourTransformationDecoder : IDecoder<BackgroundColourTransformation>
{
    public BackgroundColourTransformation Decode(string code)
    {
        string[] values = code.Split(',');

        if (values[0] != "3" && values[0] != "Colour")
            throw new BeatmapParsingException("This line is not a background colour transformation.", code);

        if (!FileFormatHelper.TryParseDouble(values[1], out double startTime))
            throw new BeatmapParsingException("Failed to parse start time of background colour transformation.", code);

        if (!FileFormatHelper.TryParseDouble(values[2], out double r))
            throw new BeatmapParsingException("Failed to parse red value of background colour transformation.", code);

        if (!FileFormatHelper.TryParseDouble(values[3], out double g))
            throw new BeatmapParsingException("Failed to parse green value of background colour transformation.", code);

        if (!FileFormatHelper.TryParseDouble(values[4], out double b))
            throw new BeatmapParsingException("Failed to parse blue value of background colour transformation.", code);

        return new BackgroundColourTransformation
        {
            EventType = values[0],
            StartTime = startTime,
            R = r,
            G = g,
            B = b,
        };
    }
}