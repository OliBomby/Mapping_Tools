namespace Mapping_Tools.Application.Execution;

/// <summary>
/// Describes the terminal state of a tool invocation, including the
/// non-exceptional busy and cancellation outcomes absent from BackgroundWorker.
/// </summary>
public enum ToolExecutionStatus
{
    /// <summary>
    /// The operation returned a value and all requested completion behavior succeeded.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Cooperative cancellation stopped the operation or its completion behavior.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The operation or its requested editor reload threw an exception.
    /// </summary>
    Failed,

    /// <summary>
    /// Another invocation with the same operation identifier was already active.
    /// </summary>
    AlreadyRunning
}

/// <summary>
/// Reports bounded percent completion and optional stage text in a format that
/// can drive either a progress bar or a textual status surface.
/// </summary>
public sealed record ToolExecutionProgress
{
    /// <summary>
    /// Creates one progress observation.
    /// </summary>
    /// <param name="percent">Completion from zero through one hundred, inclusive.</param>
    /// <param name="stage">Optional concise text identifying the current phase.</param>
    public ToolExecutionProgress(double percent, string? stage = null)
    {
        if (!double.IsFinite(percent) || percent < 0 || percent > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(percent),
                percent,
                "Tool progress must be a finite percentage from 0 through 100.");
        }

        Percent = percent;
        Stage = string.IsNullOrWhiteSpace(stage) ? null : stage;
    }

    /// <summary>
    /// Supplies a finite zero-to-one-hundred value compatible with legacy tool progress bars.
    /// </summary>
    public double Percent { get; }

    /// <summary>
    /// Optionally identifies the current phase without embedding presentation markup.
    /// </summary>
    public string? Stage { get; }
}

/// <summary>
/// Gives a tool cooperative cancellation and progress reporting while keeping
/// execution mechanics out of its transformation logic.
/// </summary>
public sealed class ToolExecutionContext
{
    private readonly IProgress<ToolExecutionProgress>? _progress;

    internal ToolExecutionContext(
        CancellationToken cancellationToken,
        IProgress<ToolExecutionProgress>? progress)
    {
        CancellationToken = cancellationToken;
        _progress = progress;
    }

    /// <summary>
    /// Signals user cancellation, application shutdown, or targeted cancellation by operation identifier.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Sends one validated progress observation to the caller-supplied progress channel.
    /// </summary>
    /// <param name="percent">Completion from zero through one hundred, inclusive.</param>
    /// <param name="stage">Optional phase text such as "Loading maps" or "Saving results".</param>
    public void ReportProgress(double percent, string? stage = null)
    {
        CancellationToken.ThrowIfCancellationRequested();
        _progress?.Report(new ToolExecutionProgress(percent, stage));
    }
}

/// <summary>
/// Combines a tool's typed value with completion behavior that belongs to the
/// execution coordinator rather than the transformation itself.
/// </summary>
public sealed record ToolExecutionOutput<T>
{
    /// <summary>
    /// Creates a successful operation output.
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
    /// Carries the operation-specific result without boxing or view-state mutation.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Supplies optional success prose for the notification stream.
    /// </summary>
    public string? Summary { get; }

    /// <summary>
    /// Requests editor reload as part of successful completion rather than from a view event handler.
    /// </summary>
    public bool ReloadEditor { get; }
}

/// <summary>
/// Defines one independently cancellable tool invocation and the stable key
/// used to reject duplicate runs of that same feature.
/// </summary>
public sealed class ToolExecutionRequest<T>
{
    /// <summary>
    /// Creates an invocation whose delegate runs on a thread-pool thread.
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
    /// Identifies the concurrency slot and targeted-cancellation handle for this feature.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Names the operation in user notifications without requiring the coordinator to know the feature.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Supplies the work delegate that receives cancellation and progress through its context.
    /// </summary>
    public Func<ToolExecutionContext, Task<ToolExecutionOutput<T>>> Operation { get; }
}

/// <summary>
/// Captures a terminal tool outcome with timing and diagnostics while keeping
/// cancellation and duplicate-run rejection out of exception-driven control flow.
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
    /// Distinguishes success, cooperative cancellation, failure, and an occupied concurrency slot.
    /// </summary>
    public ToolExecutionStatus Status { get; }

    /// <summary>
    /// Carries the typed value only when <see cref="Status"/> is <see cref="ToolExecutionStatus.Succeeded"/>.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Retains the operation or reload failure only when <see cref="Status"/> is <see cref="ToolExecutionStatus.Failed"/>.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Records when the accepted invocation acquired its concurrency slot.
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Records terminal completion; busy rejection uses the same timestamp for start and completion.
    /// </summary>
    public DateTimeOffset CompletedAt { get; }

    /// <summary>
    /// Confirms that a requested and settings-enabled osu! reload completed successfully.
    /// </summary>
    public bool EditorReloaded { get; }

    /// <summary>
    /// Measures time spent in accepted work and completion behavior.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;
}

/// <summary>
/// Runs feature use cases outside the UI thread, serializes invocations per
/// operation identifier, and coordinates cancellation, notifications, and reload.
/// </summary>
public interface IToolExecutionService
{
    /// <summary>
    /// Executes a typed request or returns an immediate busy result when its key is occupied.
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
    /// Cooperatively cancels the active invocation with the specified stable key.
    /// </summary>
    /// <param name="operationId">The feature or command key supplied in its request.</param>
    /// <returns><see langword="true"/> when an active invocation received the signal.</returns>
    bool Cancel(string operationId);

    /// <summary>
    /// Checks whether a feature currently owns its keyed concurrency slot.
    /// </summary>
    /// <param name="operationId">The feature or command key to inspect.</param>
    /// <returns><see langword="true"/> until its accepted invocation reaches a terminal result.</returns>
    bool IsRunning(string operationId);

    /// <summary>
    /// Cancels every active invocation and waits for cooperative completion,
    /// bounded by the supplied shutdown token.
    /// </summary>
    /// <param name="cancellationToken">Limits how long graceful application shutdown waits.</param>
    /// <returns>A task that completes after all accepted operations release their slots.</returns>
    /// <exception cref="OperationCanceledException">The shutdown wait exceeded its cancellation bound.</exception>
    Task StopAsync(CancellationToken cancellationToken = default);
}
