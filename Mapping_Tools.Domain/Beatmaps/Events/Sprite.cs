using Mapping_Tools.Domain.Beatmaps.Types;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.Events;

public class Sprite : Event, IHasStoryboardLayer {
    public override string EventType { get; set; } = "Sprite";
    public StoryboardLayer Layer { get; set; }
    public Origin Origin { get; set; }

    /// <summary>
    /// This is a partial path to the image file for this sprite.
    /// </summary>
    public required string FilePath { get; set; }

    public Vector2 Pos { get; set; }
}