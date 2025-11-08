using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// Represents trigger loop events. Although called loops, these only ever activate once.
/// </summary>
public class TriggerLoop : Command, IHasDuration {
    public override string EventType { get; set; } = "T";
    public override CommandType CommandType => CommandType.T;
    public double Duration => EndTime - StartTime;
    public double EndTime { get; set; }
    public required string TriggerName { get; set; }
    public bool DurationDefined { get; set; }
}