namespace Mapping_Tools.Application.Execution.ToolExecution.Models;

/// <summary>
///     Captures a terminal tool outcome with timing and diagnostics while keeping
///     cancellation and duplicate-run rejection out of exception-driven control flow.
/// </summary>
public sealed class ToolExecutionResult<T>
{
    internal ToolExecutionResult(
        ToolExecutionStatus status,
        T? value,
        Exception? exception,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        bool editorReloaded)
    {
        Status = status;
        Value = value;
        Exception = exception;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        EditorReloaded = editorReloaded;
    }

    /// <summary>
    ///     Distinguishes success, cooperative cancellation, failure, and an occupied concurrency slot.
    /// </summary>
    public ToolExecutionStatus Status { get; }

    /// <summary>
    ///     Carries the typed value only when <see cref="Status" /> is <see cref="ToolExecutionStatus.Succeeded" />.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    ///     Retains the operation or reload failure only when <see cref="Status" /> is
    ///     <see cref="ToolExecutionStatus.Failed" />.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    ///     Records when the accepted invocation acquired its concurrency slot.
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    ///     Records terminal completion; busy rejection uses the same timestamp for start and completion.
    /// </summary>
    public DateTimeOffset CompletedAt { get; }

    /// <summary>
    ///     Confirms that a requested and settings-enabled osu! reload completed successfully.
    /// </summary>
    public bool EditorReloaded { get; }

    /// <summary>
    ///     Measures time spent in accepted work and completion behavior.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;
}

