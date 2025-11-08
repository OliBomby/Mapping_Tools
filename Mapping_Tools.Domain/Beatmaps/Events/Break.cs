using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

public class Break : Event, IHasStartTime, IHasDuration, IComparable<Break> {
    public override string EventType { get; set; } = "Break";
    public double StartTime { get; set; }
    public double Duration => EndTime - StartTime;
    public double EndTime { get; set; }

    /// <inheritdoc/>
    public int CompareTo(Break? other) {
        return other == null ? 1 : StartTime.CompareTo(other.StartTime);
    }
}