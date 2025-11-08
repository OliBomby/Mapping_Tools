using Mapping_Tools.Domain.Beatmaps.Parsing;
using Mapping_Tools.Domain.Beatmaps.Types;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.Events;

public class Background : Event, IHasStartTime {
    public string EventType { get; set; }
    public double StartTime { get; set; }
    public string Filename { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }

    public Vector2 GetOffset() {
        return new Vector2(XOffset, YOffset);
    }

    public override string GetLine() {
        // Writing the offset is optional if its 0,0 but we add it anyways because that is what osu! does on later file format versions.
        return $"{EventType},{StartTime.ToRoundInvariant()},\"{Filename}\",{XOffset.ToInvariant()},{YOffset.ToInvariant()}";
    }

    public override void SetLine(string line) {
        string[] values = line.Split(',');

        if (values[0] != "0" && values[0] != "Background") {
            throw new BeatmapParsingException("This line is not a background.", line);
        }

        EventType = values[0];

        // This start time is usually 0 for backgrounds but lets parse it anyways
        if (FileFormatHelper.TryParseDouble(values[1], out double startTime))
            StartTime = startTime;
        else throw new BeatmapParsingException("Failed to parse start time of background.", line);

        Filename = values[2].Trim('"');

        // Writing offset is optional
        if (values.Length > 3) {
            if (FileFormatHelper.TryParseInt(values[3], out int xOffset))
                XOffset = xOffset;
            else throw new BeatmapParsingException("Failed to parse X offset of background.", line);

            if (FileFormatHelper.TryParseInt(values[4], out int yOffset))
                YOffset = yOffset;
            else throw new BeatmapParsingException("Failed to parse Y offset of background.", line);
        } else {
            XOffset = 0;
            YOffset = 0;
        }
    }
}