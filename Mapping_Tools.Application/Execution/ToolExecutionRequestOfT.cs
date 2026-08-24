namespace Mapping_Tools.Application.Execution;

/// <summary>
///     Defines one independently cancellable tool invocation and the stable key
///     used to reject duplicate runs of that same feature.
/// </summary>
public sealed class ToolExecutionRequest<T>
{
    /// <summary>
    ///     Creates an invocation whose delegate runs on a thread-pool thread.
    /// </summary>
    /// <param name="operationId">A stable feature or command key used for concurrency and targeted cancellation.</param>
    /// <param name="displayName">User-facing operation name used in completion and failure messages.</param>
    /// <param name="operation">The asynchronous transformation or use case to execute.</param>
    public ToolExecutionRequest(
        string operationId,
        string displayName,
        Func<ToolExecutionContext, Task<ToolExecutionOutput<T>>> operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        OperationId = operationId;
        DisplayName = displayName;
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
    }

    /// <summary>
    ///     Identifies the concurrency slot and targeted-cancellation handle for this feature.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    ///     Names the operation in user notifications without requiring the coordinator to know the feature.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    ///     Supplies the work delegate that receives cancellation and progress through its context.
    /// </summary>
    public Func<ToolExecutionContext, Task<ToolExecutionOutput<T>>> Operation { get; }
}

