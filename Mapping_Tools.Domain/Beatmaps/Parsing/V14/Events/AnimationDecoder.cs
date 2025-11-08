using Mapping_Tools.Domain.Beatmaps.Events;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class AnimationDecoder : IDecoder<Animation> {
    public Animation Decode(string code) {
        string[] values = code.Split(',');

        if (values[0] != "Animation" && values[0] != "6")
            throw new BeatmapParsingException("This line is not an animation.", code);

        if (!Enum.TryParse(values[1], out StoryboardLayer layer))
            throw new BeatmapParsingException("Failed to parse layer of animation.", code);

        if (!Enum.TryParse(values[2], out Origin origin))
            throw new BeatmapParsingException("Failed to parse origin of animation.", code);

        var filePath = values[3].Trim('"');

        if (!FileFormatHelper.TryParseDouble(values[4], out double x))
            throw new BeatmapParsingException("Failed to parse X position of animation.", code);

        if (!FileFormatHelper.TryParseDouble(values[5], out double y))
            throw new BeatmapParsingException("Failed to parse Y position of animation.", code);

        var pos = new Vector2(x, y);

        if (!FileFormatHelper.TryParseInt(values[6], out int frameCount))
            throw new BeatmapParsingException("Failed to parse frame count of animation.", code);

        if (!FileFormatHelper.TryParseDouble(values[7], out double frameDelay))
            throw new BeatmapParsingException("Failed to parse frame delay of animation.", code);

        var loopType = LoopType.LoopForever;

        if (values.Length > 8) {
            if (!Enum.TryParse(values[8], out loopType))
                throw new BeatmapParsingException("Failed to parse loop type of animation.", code);
        }

        return new Animation {
            EventType = values[0],
            Layer = layer,
            Origin = origin,
            FilePath = filePath,
            Pos = pos,
            FrameCount = frameCount,
            FrameDelay = frameDelay,
            LoopType = loopType,
        };
    }
}