namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// Represents the standard loop event. This event has a different syntax so it can't be a <see cref="OtherCommand"/>.
/// </summary>
public class StandardLoop : Command {
    public override string EventType { get; set; } = "L";
    public override CommandType CommandType => CommandType.L;

    public int LoopCount { get; set; }
}