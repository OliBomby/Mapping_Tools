namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14;

public class ComboColourDecoder : IDecoder<ComboColour> {
    public ComboColour Decode(string line) {
        string[] split = line.Split(':');
        string[] commaSplit = split[^1].Split(',');

        if (commaSplit.Length < 3)
            throw new BeatmapParsingException("Invalid combo colour values.", line);

        if (!FileFormatHelper.TryParseInt(commaSplit[0], out int r))
            throw new BeatmapParsingException("Failed to parse red component of colour.", line);

        if (!FileFormatHelper.TryParseInt(commaSplit[1], out int g))
            throw new BeatmapParsingException("Failed to parse green component of colour.", line);

        if (!FileFormatHelper.TryParseInt(commaSplit[2], out int b))
            throw new BeatmapParsingException("Failed to parse blue component of colour.", line);

        return new ComboColour(r, g, b);
    }
}