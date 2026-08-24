namespace Mapping_Tools.Application.Execution.ToolExecution.Models;

/// <summary>
///     Combines a tool's typed value with completion behavior that belongs to the
///     execution coordinator rather than the transformation itself.
/// </summary>
public sealed record ToolExecutionOutput<T>
{
    /// <summary>
    ///     Creates a successful operation output.
    /// </summary>
    /// <param name="value">The typed value returned to the initiating view model or command.</param>
    /// <param name="summary">Optional success text published through the notification service.</param>
    /// <param name="reloadEditor">Whether osu! should reload after the operation succeeds.</param>
    public ToolExecutionOutput(
        T value,
        string? summary = null,
        bool reloadEditor = false)
    {
        Value = value;
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary;
        ReloadEditor = reloadEditor;
    }

    /// <summary>
    ///     Carries the operation-specific result without boxing or view-state mutation.
    /// </summary>
    public T Value { get; }

    /// <summary>
    ///     Supplies optional success prose for the notification stream.
    /// </summary>
    public string? Summary { get; }

    /// <summary>
    ///     Requests editor reload as part of successful completion rather than from a view event handler.
    /// </summary>
    public bool ReloadEditor { get; }
}

