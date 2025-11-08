using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

public class Video : Event, IHasStartTime {
    public string EventType { get; set; }
    public double StartTime { get; set; }
    public string Filename { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
}