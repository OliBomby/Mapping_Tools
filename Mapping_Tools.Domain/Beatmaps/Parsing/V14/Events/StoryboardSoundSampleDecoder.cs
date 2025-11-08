using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class StoryboardSoundSampleDecoder : IDecoder<StoryboardSoundSample>
{
    public StoryboardSoundSample Decode(string code)
    {
        string[] values = code.Split(',');

        if (values[0] != "Sample" && values[0] != "5")
            throw new BeatmapParsingException("This line is not a storyboarded sample.", code);

        if (!FileFormatHelper.TryParseDouble(values[1], out double t))
            throw new BeatmapParsingException("Failed to parse time of storyboarded sample.", code);

        if (!Enum.TryParse(values[2], out StoryboardLayer layer))
            throw new BeatmapParsingException("Failed to parse layer of storyboarded sample.", code);

        var filePath = values[3].Trim('"');

        double volume = 100;
        if (values.Length > 4)
        {
            if (!FileFormatHelper.TryParseDouble(values[4], out volume))
                throw new BeatmapParsingException("Failed to parse volume of storyboarded sample.", code);
        }

        return new StoryboardSoundSample
        {
            StartTime = t,
            Layer = layer,
            FilePath = filePath,
            Volume = volume,
        };
    }
}