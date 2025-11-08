using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// Deprecated event that appears under "//Background Colour Transformations" in old beatmaps.
/// Idk fully how it works.
/// </summary>
public class BackgroundColourTransformation : Event, IHasStartTime {
    public override string EventType { get; set; } = "Colour";
    public double StartTime { get; set; }
    public double R { get; set; }
    public double G { get; set; }
    public double B { get; set; }
}