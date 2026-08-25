namespace Mapping_Tools.Core.Tools.SliderCompletionator.Models;

/// <summary>
///     Stores Slider Completionator's persisted settings and transformation inputs.
///     A numeric value of <c>-1</c> means that the corresponding value is preserved.
/// </summary>
public class SliderCompletionatorOptions
{
    /// <summary>Gets or sets the value calculated by the transformation.</summary>
    public SliderCompletionatorFreeVariable FreeVariableSetting { get; set; } =
        SliderCompletionatorFreeVariable.Velocity;

    /// <summary>Gets or sets the requested slider duration in beats, or <c>-1</c> to preserve it.</summary>
    public double Duration { get; set; } = -1;

    /// <summary>Gets or sets the requested slider end time in milliseconds, or <c>-1</c> to preserve it.</summary>
    public double EndTime { get; set; } = -1;

    /// <summary>Gets or sets the requested fraction of the complete slider path, or <c>-1</c> to preserve it.</summary>
    public double Length { get; set; } = 1;

    /// <summary>Gets or sets the requested inherited slider velocity multiplier, or <c>-1</c> to preserve it.</summary>
    public double SliderVelocity { get; set; } = -1;

    /// <summary>Gets or sets whether all slider anchors are moved to the new path length.</summary>
    public bool MoveAnchors { get; set; }

    /// <summary>Gets or sets whether <see cref="EndTime" /> replaces duration input.</summary>
    public bool UseEndTime { get; set; }

    /// <summary>Gets or sets whether the live editor time supplies the end time.</summary>
    public bool UseCurrentEditorTime { get; set; }

    /// <summary>Gets or sets whether slider velocity is delegated to BPM timing points.</summary>
    public bool DelegateToBpm { get; set; }

    /// <summary>Gets or sets whether delegated sliders suppress slider ticks with NaN velocity.</summary>
    public bool RemoveSliderTicks { get; set; }
}
