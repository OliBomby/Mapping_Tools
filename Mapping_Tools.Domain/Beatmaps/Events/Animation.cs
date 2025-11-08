using Mapping_Tools.Domain.Beatmaps.Types;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.Events;

public class Animation : Event, IHasStoryboardLayer {
    public override string EventType { get; set; } = "Animation";
    public StoryboardLayer Layer { get; set; }
    public Origin Origin { get; set; }

    /// <summary>
    /// This is a partial path to the image file for this sprite.
    /// </summary>
    public required string FilePath { get; set; }

    public Vector2 Pos { get; set; }

    public int FrameCount { get; set; }
    public double FrameDelay { get; set; }
    public LoopType LoopType { get; set; } = LoopType.LoopForever;
}