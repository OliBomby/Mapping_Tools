using Mapping_Tools.Core.MathUtil;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>
///     Represents a background video declaration in the beatmap events section.
/// </summary>
public class Video : Event, IHasStartTime
{
    /// <summary>
    ///     Gets or sets the original video token, preserving <c>1</c> or <c>Video</c>.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the video path relative to the beatmap folder.
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the optional video offset in osu! playfield coordinates.
    /// </summary>
    public Vector2 Pos { get; set; }

    /// <inheritdoc />
    public double StartTime { get; set; }

}
