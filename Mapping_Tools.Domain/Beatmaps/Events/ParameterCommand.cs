using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// Represents the parameter command. This event has a different syntax so it can't be a <see cref="OtherCommand"/>.
/// </summary>
public class ParameterCommand : Command, IHasDuration {
    public override string EventType { get; set; } = "P";
    public override CommandType CommandType => CommandType.P;
    public EasingType Easing { get; set; }
    public double Duration => EndTime - StartTime;
    public double EndTime { get; set; }
    public required string Parameter { get; set; }
}