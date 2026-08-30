using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;
#nullable disable

/// <summary>
///     Represents a legacy background colour transformation in an osu! events section.
/// </summary>
public class Colour : Event, IHasStartTime
{
    /// <summary>
    ///     Gets or sets the serialized event token, normally <c>3</c>.
    /// </summary>
    public string EventType { get; set; }

    /// <inheritdoc />
    public double StartTime { get; set; }

    /// <summary>
    ///     Gets or sets the background colour applied from <see cref="StartTime" /> onward.
    /// </summary>
    public RgbaColour Color { get; set; }

    /// <summary>
    ///     Initializes an empty background colour transformation.
    /// </summary>
    public Colour()
    {
    }

    /// <inheritdoc />
    public override string GetLine()
    {
        return $"{EventType},{(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant())},{Color.R},{Color.G},{Color.B}";
    }

    /// <inheritdoc />
    public override void SetLine(string line)
    {
        string subLine = RemoveIndents(line);
        string[] values = subLine.Split(',');

        if (values[0] != "3" && values[0] != "Colour")
            throw new BeatmapParsingException("This line is not a background colour transformation.", line);

        EventType = values[0];

        if (TryParseDouble(values[1], out double startTime))
            StartTime = startTime;
        else throw new BeatmapParsingException("Failed to parse start time of background colour transformation.", line);

        if (!TryParseInt(values[2], out int r))
            throw new BeatmapParsingException("Failed to parse red component of background colour transformation.", line);
        if (!TryParseInt(values[3], out int g))
            throw new BeatmapParsingException("Failed to parse green component of background colour transformation.", line);
        if (!TryParseInt(values[4], out int b))
            throw new BeatmapParsingException("Failed to parse blue component of background colour transformation.", line);

        Color = RgbaColour.FromRgb((byte)r, (byte)g, (byte)b);
    }
}
