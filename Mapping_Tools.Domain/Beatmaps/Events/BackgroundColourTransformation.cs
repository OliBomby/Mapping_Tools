using Mapping_Tools.Domain.Beatmaps.Parsing;
using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// Deprecated event that appears under "//Background Colour Transformations" in old beatmaps.
/// Idk fully how it works.
/// </summary>
public class BackgroundColourTransformation : Event, IHasStartTime {
    public double StartTime { get; set; }
    public double R { get; set; }
    public double G { get; set; }
    public double B { get; set; }

    public BackgroundColourTransformation() { }

    public BackgroundColourTransformation(string line) {
        SetLine(line);
    }

    public override string GetLine() {
        return $"3,{StartTime.ToRoundInvariant()},{R.ToRoundInvariant()},{G.ToRoundInvariant()},{B.ToRoundInvariant()}";
    }

    public sealed override void SetLine(string line) {
        string[] values = line.Split(',');

        if (values[0] != "3" && values[0] != "Colour") {
            throw new BeatmapParsingException("This line is not a background colour transformation.", line);
        }

        if (FileFormatHelper.TryParseDouble(values[1], out double startTime))
            StartTime = startTime;
        else throw new BeatmapParsingException("Failed to parse start time of background colour transformation.", line);

        if (FileFormatHelper.TryParseDouble(values[2], out double r))
            R = r;
        else throw new BeatmapParsingException("Failed to parse red value of background colour transformation.", line);

        if (FileFormatHelper.TryParseDouble(values[3], out double g))
            G = g;
        else throw new BeatmapParsingException("Failed to parse green value of background colour transformation.", line);

        if (FileFormatHelper.TryParseDouble(values[4], out double b))
            B = b;
        else throw new BeatmapParsingException("Failed to parse blue value of background colour transformation.", line);
    }
}