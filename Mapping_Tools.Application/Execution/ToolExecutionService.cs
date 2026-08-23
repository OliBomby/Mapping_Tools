using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Application.Execution;

/// <summary>
///     Replaces view-owned BackgroundWorkers with keyed, thread-pool execution and
///     one process-wide cancellation and completion policy.
/// </summary>
public sealed class ToolExecutionService : IToolExecutionService
{
    private readonly object _gate = new();
    private readonly IUserNotificationService _notifications;
    private readonly IEditorReloadService _reloadService;

    private readonly Dictionary<string, RunningOperation> _running =
        new(StringComparer.Ordinal);

    private readonly ApplicationSettings _settings;
    private readonly CancellationTokenSource _stopping = new();
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Creates the coordinator that owns duplicate-run prevention, application
    ///     shutdown cancellation, notifications, and post-success editor reload.
    /// </summary>
    /// <param name="notifications">The frontend-neutral outcome stream.</param>
    /// <param name="reloadService">The platform adapter invoked for successful reload requests.</param>
    /// <param name="settings">The live AutoReload preference.</param>
    /// <param name="timeProvider">Supplies deterministic result timestamps.</param>
    public ToolExecutionService(
        IUserNotificationService notifications,
        IEditorReloadService reloadService,
        ApplicationSettings settings,
        TimeProvider timeProvider)
    {
        _notifications = notifications
                         ?? throw new ArgumentNullException(nameof(notifications));
        _reloadService = reloadService
                         ?? throw new ArgumentNullException(nameof(reloadService));
        _settings = settings
                    ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider
                        ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public Task<ToolExecutionResult<T>> ExecuteAsync<T>(
        ToolExecutionRequest<T> request,
        IProgress<ToolExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var startedAt = _timeProvider.GetUtcNow();
        var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _stopping.Token);

        lock (_gate)
        {
            if (_running.ContainsKey(request.OperationId))
            {
                linked.Dispose();
                return Task.FromResult(
                    new ToolExecutionResult<T>(
                        ToolExecutionStatus.AlreadyRunning,
                        default,
                        null,
                        startedAt,
                        startedAt,
                        false));
            }

            var task = RunAsync(
                request,
                progress,
                linked,
                startedAt);
            _running.Add(
                request.OperationId,
                new RunningOperation(linked, task));
            return task;
        }
    }

    /// <inheritdoc />
    public bool Cancel(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        RunningOperation operation;
        lock (_gate)
        {
            if (!_running.TryGetValue(operationId, out operation!)) return false;
        }

        operation.TryCancel();
        return true;
    }

    /// <inheritdoc />
    public bool IsRunning(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        lock (_gate)
        {
            return _running.ContainsKey(operationId);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _stopping.Cancel();
        Task[] tasks;
        RunningOperation[] operations;
        lock (_gate)
        {
            operations = _running.Values.ToArray();
            tasks = operations.Select(operation => operation.Task).ToArray();
        }

        foreach (var operation in operations) operation.TryCancel();

        if (tasks.Length > 0)
            await Task.WhenAll(tasks)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
    }

    private async Task<ToolExecutionResult<T>> RunAsync<T>(
        ToolExecutionRequest<T> request,
        IProgress<ToolExecutionProgress>? progress,
        CancellationTokenSource linked,
        DateTimeOffset startedAt)
    {
        try
        {
            ToolExecutionContext context = new(linked.Token, progress);
            var output = await Task.Run(
                    () => request.Operation(context),
                    linked.Token)
                .ConfigureAwait(false);
            linked.Token.ThrowIfCancellationRequested();

            bool reloaded = false;
            if (output.ReloadEditor && _settings.AutoReload)
            {
                await _reloadService.ReloadAsync(linked.Token).ConfigureAwait(false);
                reloaded = true;
            }

            if (output.Summary is not null)
                await PublishSafelyAsync(
                        new UserNotification(
                            UserNotificationSeverity.Success,
                            request.DisplayName,
                            output.Summary))
                    .ConfigureAwait(false);

            return new ToolExecutionResult<T>(
                ToolExecutionStatus.Succeeded,
                output.Value,
                null,
                startedAt,
                _timeProvider.GetUtcNow(),
                reloaded);
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
            return new ToolExecutionResult<T>(
                ToolExecutionStatus.Cancelled,
                default,
                null,
                startedAt,
                _timeProvider.GetUtcNow(),
                false);
        }
        catch (Exception exception)
        {
            await PublishSafelyAsync(
                    new UserNotification(
                        UserNotificationSeverity.Error,
                        request.DisplayName,
                        exception.Message,
                        exception))
                .ConfigureAwait(false);
            return new ToolExecutionResult<T>(
                ToolExecutionStatus.Failed,
                default,
                exception,
                startedAt,
                _timeProvider.GetUtcNow(),
                false);
        }
        finally
        {
            lock (_gate)
            {
                _running.Remove(request.OperationId);
            }

            linked.Dispose();
        }
    }

    private async Task PublishSafelyAsync(UserNotification notification)
    {
        try
        {
            await _notifications.PublishAsync(
                    notification,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // Presentation subscribers must not change the tool's terminal result.
        }
    }

    private sealed record RunningOperation(
        CancellationTokenSource Cancellation,
        Task Task)
    {
        internal void TryCancel()
        {
            try
            {
                Cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The operation reached its terminal result between lookup and cancellation.
            }
        }
    }
}
