namespace Mapping_Tools.Application.Execution.ToolExecution.Models;

/// <summary>
///     Describes the terminal state of a tool invocation, including the
///     non-exceptional busy and cancellation outcomes absent from BackgroundWorker.
/// </summary>
public enum ToolExecutionStatus
{
    /// <summary>
    ///     The operation returned a value and all requested completion behavior succeeded.
    /// </summary>
    Succeeded,

    /// <summary>
    ///     Cooperative cancellation stopped the operation or its completion behavior.
    /// </summary>
    Cancelled,

    /// <summary>
    ///     The operation or its requested editor reload threw an exception.
    /// </summary>
    Failed,

    /// <summary>
    ///     Another invocation with the same operation identifier was already active.
    /// </summary>
    AlreadyRunning,
}

