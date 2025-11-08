using Mapping_Tools.Domain.Beatmaps.Events;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class SpriteDecoder : IDecoder<Sprite>
{
    public Sprite Decode(string code)
    {
        string[] values = code.Split(',');

        if (values[0] != "Sprite" && values[0] != "4")
            throw new BeatmapParsingException("This line is not a sprite.", code);

        if (!Enum.TryParse(values[1], out StoryboardLayer layer))
            throw new BeatmapParsingException("Failed to parse layer of sprite.", code);

        if (!Enum.TryParse(values[2], out Origin origin))
            throw new BeatmapParsingException("Failed to parse origin of sprite.", code);

        var filePath = values[3].Trim('"');

        if (!FileFormatHelper.TryParseDouble(values[4], out double x))
            throw new BeatmapParsingException("Failed to parse X position of sprite.", code);

        if (!FileFormatHelper.TryParseDouble(values[5], out double y))
            throw new BeatmapParsingException("Failed to parse Y position of sprite.", code);

        var pos = new Vector2(x, y);

        return new Sprite
        {
            EventType = values[0],
            Layer = layer,
            Origin = origin,
            FilePath = filePath,
            Pos = pos,
        };
    }
}