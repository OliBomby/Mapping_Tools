using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

public abstract class Command : Event, IHasStartTime {
    public int Indents { get; set; }
    public virtual EventType EventType { get; set; }
    public double StartTime { get; set; }
}