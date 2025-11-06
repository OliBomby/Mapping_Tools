using Mapping_Tools.Core.BeatmapHelper.Types;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

public abstract class Command : Event, IHasStartTime {
    public int Indents { get; set; }
    public virtual EventType EventType { get; set; }
    public double StartTime { get; set; }
}