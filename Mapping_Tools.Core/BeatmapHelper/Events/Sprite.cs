using Mapping_Tools.Core.MathUtil;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;
#nullable disable

/// <summary>
///     Represents a static storyboard texture and its initial placement.
/// </summary>
public class Sprite : Event
{
    /// <summary>
    ///     Gets or sets the storyboard layer on which the texture is drawn.
    /// </summary>
    public StoryboardLayer Layer { get; set; }

    /// <summary>
    ///     Gets or sets the texture point anchored to <see cref="Pos" />.
    /// </summary>
    public Origin Origin { get; set; }

    /// <summary>
    ///     This is a partial path to the image file for this sprite.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    ///     Gets or sets the sprite's storyboard-space anchor position.
    /// </summary>
    public Vector2 Pos { get; set; }

    /// <summary>
    ///     <inheritdoc />
    public override string GetLine()
    {
        return $"Sprite,{Layer},{Origin},\"{FilePath}\",{Pos.X.ToInvariant()},{Pos.Y.ToInvariant()}";
    }

    /// <summary>
    ///     <inheritdoc />
    public override void SetLine(string line)
    {
        string[] values = line.Split(',');

        if (values[0] != "Sprite") throw new BeatmapParsingException("This line is not a sprite.", line);

        if (Enum.TryParse(values[1], out StoryboardLayer layer))
            Layer = layer;
        else throw new BeatmapParsingException("Failed to parse layer of sprite.", line);

        if (Enum.TryParse(values[2], out Origin origin))
            Origin = origin;
        else throw new BeatmapParsingException("Failed to parse origin of sprite.", line);

        FilePath = values[3].Trim('"');

        if (!TryParseDouble(values[4], out double x))
            throw new BeatmapParsingException("Failed to parse X position of sprite.", line);

        if (!TryParseDouble(values[5], out double y))
            throw new BeatmapParsingException("Failed to parse Y position of sprite.", line);

        Pos = new Vector2(x, y);
    }
}
