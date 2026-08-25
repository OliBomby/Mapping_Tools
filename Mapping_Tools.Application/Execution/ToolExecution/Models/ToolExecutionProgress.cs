namespace Mapping_Tools.Application.Execution.ToolExecution.Models;

/// <summary>
///     Reports bounded normalized completion and optional stage text in a format that
///     can drive either a progress bar or a textual status surface.
/// </summary>
public sealed record ToolExecutionProgress
{
    /// <summary>
    ///     Creates one progress observation.
    /// </summary>
    /// <param name="progress">Completion from zero through one, inclusive.</param>
    /// <param name="stage">Optional concise text identifying the current phase.</param>
    public ToolExecutionProgress(double progress, string? stage = null)
    {
        if (!double.IsFinite(progress) || progress < 0 || progress > 1)
            throw new ArgumentOutOfRangeException(
                nameof(progress),
                progress,
                "Tool progress must be a finite value from 0 through 1.");

        Progress = progress;
        Stage = string.IsNullOrWhiteSpace(stage) ? null : stage;
    }

    /// <summary>
    ///     Supplies a finite normalized completion value from zero through one.
    /// </summary>
    public double Progress { get; }

    /// <summary>
    ///     Optionally identifies the current phase without embedding presentation markup.
    /// </summary>
    public string? Stage { get; }
}
