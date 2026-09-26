using Mapping_Tools.Core.MathUtil;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>
///     Represents the playfield background declaration in an osu! events section.
/// </summary>
public class Background : Event, IHasStartTime
{
    /// <summary>
    ///     Gets or sets the original background token, normally <c>0</c>.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the background image path relative to the beatmap folder.
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the optional image offset in osu! playfield coordinates.
    /// </summary>
    public Vector2 Pos { get; set; }

    /// <inheritdoc />
    public double StartTime { get; set; }

}
