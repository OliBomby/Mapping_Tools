namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>
///     Represents all the commands
///     The exceptions being loops and triggers because these have different syntax.
/// </summary>
public class OtherCommand : Command, IHasEndTime
{
    /// <summary>
    ///     Gets or sets the interpolation curve applied between parameter values.
    /// </summary>
    public EasingType Easing { get; set; }

    /// <summary>
    ///     All other parameters
    /// </summary>
    public double[] Params { get; set; } = [];

    /// <summary>
    ///     Used to describe <see cref="EventType" /> in case it is Unknown.
    /// </summary>
    public string FallbackEventType { get; set; } = string.Empty;

    /// <inheritdoc />
    public double EndTime { get; set; }

}
