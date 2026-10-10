namespace Mapping_Tools.Application.Execution.ToolExecution.Models;

/// <summary>
///     Combines a tool's typed value with optional completion text.
/// </summary>
public sealed record ToolExecutionOutput<T>
{
    /// <summary>
    ///     Creates a successful operation output.
    /// </summary>
    /// <param name="value">The typed value returned to the initiating view model or command.</param>
    /// <param name="summary">Optional success text published through the notification service.</param>
    public ToolExecutionOutput(
        T value,
        string? summary = null)
    {
        Value = value;
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary;
    }

    /// <summary>
    ///     Carries the operation-specific result without boxing or view-state mutation.
    /// </summary>
    public T Value { get; }

    /// <summary>
    ///     Supplies optional success prose for the notification stream.
    /// </summary>
    public string? Summary { get; }
}
