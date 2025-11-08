using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class VideoDecoder : IDecoder<Video>
{
    public Video Decode(string code)
    {
        string[] values = code.Split(',');

        if (values[0] != "1" && values[0] != "Video")
            throw new BeatmapParsingException("This line is not a video.", code);

        if (!FileFormatHelper.TryParseDouble(values[1], out double startTime))
            throw new BeatmapParsingException("Failed to parse start time of video.", code);

        var filename = values[2].Trim('"');

        int xOffset = 0;
        int yOffset = 0;
        if (values.Length > 3)
        {
            if (!FileFormatHelper.TryParseInt(values[3], out xOffset))
                throw new BeatmapParsingException("Failed to parse X offset of video.", code);

            if (!FileFormatHelper.TryParseInt(values[4], out yOffset))
                throw new BeatmapParsingException("Failed to parse Y offset of video.", code);
        }

        return new Video
        {
            EventType = values[0],
            StartTime = startTime,
            Filename = filename,
            XOffset = xOffset,
            YOffset = yOffset,
        };
    }
}