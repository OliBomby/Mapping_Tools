using Mapping_Tools.Core.Classes.MathUtil;
using static Mapping_Tools.Core.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.Classes.BeatmapHelper.Events;
#nullable disable

/// <summary>
///     Represents a storyboard animation whose image path expands into numbered frames.
/// </summary>
public class Animation : Event, IHasDuration
{
    /// <summary>
    ///     Gets or sets the storyboard layer on which frames are drawn.
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
    ///     Gets or sets the animation's storyboard-space anchor position.
    /// </summary>
    public Vector2 Pos { get; set; }

    /// <summary>
    ///     Gets or sets the number of numbered image frames.
    /// </summary>
    public int FrameCount { get; set; }

    /// <summary>
    ///     Gets or sets the time between frames in milliseconds.
    /// </summary>
    public double FrameDelay { get; set; }

    /// <summary>
    ///     Gets or sets whether frame playback repeats or stops after one cycle.
    /// </summary>
    public LoopType LoopType { get; set; }

    /// <summary>
    ///     <inheritdoc />
    ///     <remarks>This legacy model treats one frame delay as the animation duration.</remarks>
    public double Duration
    {
        get => FrameDelay;
        set => FrameDelay = value;
    }

    /// <summary>
    ///     <inheritdoc />
    public override string GetLine()
    {
        return $"Animation,{Layer},{Origin},\"{FilePath}\",{Pos.X.ToInvariant()},{Pos.Y.ToInvariant()},{FrameCount.ToInvariant()},{FrameDelay.ToInvariant()},{LoopType}";
    }

    /// <summary>
    ///     <inheritdoc />
    public override void SetLine(string line)
    {
        string[] values = line.Split(',');

        if (values[0] != "Animation") throw new BeatmapParsingException("This line is not an animation.", line);

        if (Enum.TryParse(values[1], out StoryboardLayer layer))
            Layer = layer;
        else throw new BeatmapParsingException("Failed to parse layer of animation.", line);

        if (Enum.TryParse(values[2], out Origin origin))
            Origin = origin;
        else throw new BeatmapParsingException("Failed to parse origin of animation.", line);

        FilePath = values[3].Trim('"');

        if (!TryParseDouble(values[4], out double x))
            throw new BeatmapParsingException("Failed to parse X position of animation.", line);

        if (!TryParseDouble(values[5], out double y))
            throw new BeatmapParsingException("Failed to parse Y position of animation.", line);

        Pos = new Vector2(x, y);

        if (TryParseInt(values[6], out int frameCount))
            FrameCount = frameCount;
        else throw new BeatmapParsingException("Failed to parse frame count of animation.", line);

        if (TryParseDouble(values[7], out double frameDelay))
            FrameDelay = frameDelay;
        else throw new BeatmapParsingException("Failed to parse frame delay of animation.", line);

        if (Enum.TryParse(values[8], out LoopType loopType))
            LoopType = loopType;
        else throw new BeatmapParsingException("Failed to parse loop type of animation.", line);
    }
}
