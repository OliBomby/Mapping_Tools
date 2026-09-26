using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>
///     Represents a legacy background colour transformation in an osu! events section.
/// </summary>
public class Colour : Event, IHasStartTime
{
    /// <summary>
    ///     Initializes an empty background colour transformation.
    /// </summary>
    public Colour()
    {
    }

    /// <summary>
    ///     Gets or sets the serialized event token, normally <c>3</c>.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the background colour applied from <see cref="StartTime" /> onward.
    /// </summary>
    public RgbaColour Color { get; set; }

    /// <inheritdoc />
    public double StartTime { get; set; }

}
