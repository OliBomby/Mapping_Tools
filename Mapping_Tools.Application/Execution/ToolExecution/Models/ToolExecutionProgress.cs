namespace Mapping_Tools.Application.Execution.ToolExecution.Models;

/// <summary>
///     Reports bounded percent completion and optional stage text in a format that
///     can drive either a progress bar or a textual status surface.
/// </summary>
public sealed record ToolExecutionProgress
{
    /// <summary>
    ///     Creates one progress observation.
    /// </summary>
    /// <param name="percent">Completion from zero through one hundred, inclusive.</param>
    /// <param name="stage">Optional concise text identifying the current phase.</param>
    public ToolExecutionProgress(double percent, string? stage = null)
    {
        if (!double.IsFinite(percent) || percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(
                nameof(percent),
                percent,
                "Tool progress must be a finite percentage from 0 through 100.");

        Percent = percent;
        Stage = string.IsNullOrWhiteSpace(stage) ? null : stage;
    }

    /// <summary>
    ///     Supplies a finite zero-to-one-hundred value compatible with legacy tool progress bars.
    /// </summary>
    public double Percent { get; }

    /// <summary>
    ///     Optionally identifies the current phase without embedding presentation markup.
    /// </summary>
    public string? Stage { get; }
}

