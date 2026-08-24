namespace Mapping_Tools.Application.Execution;

/// <summary>
///     Runs feature use cases outside the UI thread, serializes invocations per
///     operation identifier, and coordinates cancellation, notifications, and reload.
/// </summary>
public interface IToolExecutionService
{
    /// <summary>
    ///     Executes a typed request or returns an immediate busy result when its key is occupied.
    /// </summary>
    /// <typeparam name="T">The operation-specific value returned on success.</typeparam>
    /// <param name="request">The keyed operation and user-facing name.</param>
    /// <param name="progress">Optional progress receiver, normally created on the frontend synchronization context.</param>
    /// <param name="cancellationToken">Links caller cancellation to targeted and application-wide cancellation.</param>
    /// <returns>A terminal result; operation failures are captured rather than rethrown.</returns>
    Task<ToolExecutionResult<T>> ExecuteAsync<T>(
        ToolExecutionRequest<T> request,
        IProgress<ToolExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cooperatively cancels the active invocation with the specified stable key.
    /// </summary>
    /// <param name="operationId">The feature or command key supplied in its request.</param>
    /// <returns><see langword="true" /> when an active invocation received the signal.</returns>
    bool Cancel(string operationId);

    /// <summary>
    ///     Checks whether a feature currently owns its keyed concurrency slot.
    /// </summary>
    /// <param name="operationId">The feature or command key to inspect.</param>
    /// <returns><see langword="true" /> until its accepted invocation reaches a terminal result.</returns>
    bool IsRunning(string operationId);

    /// <summary>
    ///     Cancels every active invocation and waits for cooperative completion,
    ///     bounded by the supplied shutdown token.
    /// </summary>
    /// <param name="cancellationToken">Limits how long graceful application shutdown waits.</param>
    /// <returns>A task that completes after all accepted operations release their slots.</returns>
    /// <exception cref="OperationCanceledException">The shutdown wait exceeded its cancellation bound.</exception>
    Task StopAsync(CancellationToken cancellationToken = default);
}
