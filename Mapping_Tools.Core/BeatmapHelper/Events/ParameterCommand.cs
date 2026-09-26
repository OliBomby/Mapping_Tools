using System.Text;
using Mapping_Tools.Core.MathUtil;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>
///     Represents the parameter command. This event has a different syntax so it can't be a <see cref="OtherCommand" />.
/// </summary>
public class ParameterCommand : Command, IHasEndTime
{
    /// <summary>
    ///     Gets the storyboard parameter-command token.
    /// </summary>
    public override EventType EventType => EventType.P;

    /// <summary>
    ///     Gets or sets the interpolation curve applied to the parameter toggle.
    /// </summary>
    public EasingType Easing { get; set; }

    /// <summary>
    ///     Gets or sets the storyboard parameter token, such as additive blending
    ///     or horizontal/vertical flipping.
    /// </summary>
    public string Parameter { get; set; } = string.Empty;

    /// <inheritdoc />
    public double EndTime { get; set; }

}
