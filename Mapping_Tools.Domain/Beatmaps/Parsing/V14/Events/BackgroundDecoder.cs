using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class BackgroundDecoder : IDecoder<Background>
{
    public Background Decode(string code)
    {
        string[] values = code.Split(',');

        if (values[0] != "0" && values[0] != "Background")
            throw new BeatmapParsingException("This line is not a background.", code);

        if (!FileFormatHelper.TryParseDouble(values[1], out double startTime))
            throw new BeatmapParsingException("Failed to parse start time of background.", code);

        var filename = values[2].Trim('"');

        int xOffset = 0;
        int yOffset = 0;
        if (values.Length > 3)
        {
            if (!FileFormatHelper.TryParseInt(values[3], out xOffset))
                throw new BeatmapParsingException("Failed to parse X offset of background.", code);

            if (!FileFormatHelper.TryParseInt(values[4], out yOffset))
                throw new BeatmapParsingException("Failed to parse Y offset of background.", code);
        }

        return new Background
        {
            EventType = values[0],
            StartTime = startTime,
            Filename = filename,
            XOffset = xOffset,
            YOffset = yOffset,
        };
    }
}