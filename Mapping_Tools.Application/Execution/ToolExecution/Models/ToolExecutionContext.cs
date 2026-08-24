namespace Mapping_Tools.Application.Execution.ToolExecution.Models;

/// <summary>
///     Gives a tool cooperative cancellation and progress reporting while keeping
///     execution mechanics out of its transformation logic.
/// </summary>
public sealed class ToolExecutionContext
{
    private readonly IProgress<ToolExecutionProgress>? progress;

    internal ToolExecutionContext(
        CancellationToken cancellationToken,
        IProgress<ToolExecutionProgress>? progress)
    {
        CancellationToken = cancellationToken;
        this.progress = progress;
    }

    /// <summary>
    ///     Signals user cancellation, application shutdown, or targeted cancellation by operation identifier.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    ///     Sends one validated progress observation to the caller-supplied progress channel.
    /// </summary>
    /// <param name="percent">Completion from zero through one hundred, inclusive.</param>
    /// <param name="stage">Optional phase text such as "Loading maps" or "Saving results".</param>
    public void ReportProgress(double percent, string? stage = null)
    {
        CancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new ToolExecutionProgress(percent, stage));
    }
}

