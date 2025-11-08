using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

public abstract class Command : Event, IHasStartTime {
    public virtual CommandType CommandType { get; set; }
    public double StartTime { get; set; }
}