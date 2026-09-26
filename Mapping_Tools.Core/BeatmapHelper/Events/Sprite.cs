using Mapping_Tools.Core.MathUtil;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

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
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the sprite's storyboard-space anchor position.
    /// </summary>
    public Vector2 Pos { get; set; }

}
