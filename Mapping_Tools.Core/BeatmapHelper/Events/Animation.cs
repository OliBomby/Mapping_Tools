using Mapping_Tools.Core.MathUtil;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

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
    public string FilePath { get; set; } = string.Empty;

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

    /// <inheritdoc />
    /// <remarks>This legacy model treats one frame delay as the animation duration.</remarks>
    public double Duration
    {
        get => FrameDelay;
        set => FrameDelay = value;
    }

}
