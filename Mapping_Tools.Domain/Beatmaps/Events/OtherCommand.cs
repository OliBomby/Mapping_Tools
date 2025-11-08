using Mapping_Tools.Domain.Beatmaps.Types;

namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// Represents all the commands
/// The exceptions being loops and triggers because these have different syntax.
/// </summary>
public class OtherCommand : Command, IHasDuration {
    public EasingType Easing { get; set; }
    public double Duration => EndTime - StartTime;
    public double EndTime { get; set; }

    /// <summary>
    /// All other parameters
    /// </summary>
    public required double[] Params { get; set; }
}