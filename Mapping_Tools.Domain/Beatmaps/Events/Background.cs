using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

public class Background : Event, IHasStartTime {
    public override string EventType { get; set; } = "Background";
    public double StartTime { get; set; }
    public required string Filename { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
}