using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Application.Execution.ToolExecution;

/// <summary>
///     Replaces view-owned BackgroundWorkers with keyed, thread-pool execution and
///     one process-wide cancellation and completion policy.
/// </summary>
public sealed class ToolExecutionService : IToolExecutionService
{
    private readonly object gate = new();
    private readonly IUserNotificationService notifications;
    private readonly IEditorReloadService reloadService;

    private readonly Dictionary<string, RunningOperation> running =
        new(StringComparer.Ordinal);

    private readonly ApplicationSettings settings;
    private readonly CancellationTokenSource stopping = new();
    private readonly TimeProvider timeProvider;

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
        this.notifications = notifications
                             ?? throw new ArgumentNullException(nameof(notifications));
        this.reloadService = reloadService
                             ?? throw new ArgumentNullException(nameof(reloadService));
        this.settings = settings
                        ?? throw new ArgumentNullException(nameof(settings));
        this.timeProvider = timeProvider
                            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public Task<ToolExecutionResult<T>> ExecuteAsync<T>(
        ToolExecutionRequest<T> request,
        IProgress<ToolExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var startedAt = timeProvider.GetUtcNow();
        var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            stopping.Token);

        lock (gate)
        {
            if (running.ContainsKey(request.OperationId))
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

            var operation = new RunningOperation(linked);
            running.Add(request.OperationId, operation);

            var task = RunAsync(
                request,
                progress,
                linked,
                startedAt);
            operation.Task = task;
            return task;
        }
    }

    /// <inheritdoc />
    public bool Cancel(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        RunningOperation operation;
        lock (gate)
        {
            if (!running.TryGetValue(operationId, out operation!)) return false;
        }

        operation.TryCancel();
        return true;
    }

    /// <inheritdoc />
    public bool IsRunning(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        lock (gate)
        {
            return running.ContainsKey(operationId);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        stopping.Cancel();
        Task[] tasks;
        RunningOperation[] operations;
        lock (gate)
        {
            operations = running.Values.ToArray();
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
            if (output.ReloadEditor && settings.AutoReload)
            {
                await reloadService.ReloadAsync(linked.Token).ConfigureAwait(false);
                reloaded = true;
            }

            if (output.Summary is not null)
                await PublishSafelyAsync(
                        new UserNotification.Models.UserNotification(
                            UserNotificationSeverity.Success,
                            request.DisplayName,
                            output.Summary))
                    .ConfigureAwait(false);

            return new ToolExecutionResult<T>(
                ToolExecutionStatus.Succeeded,
                output.Value,
                null,
                startedAt,
                timeProvider.GetUtcNow(),
                reloaded);
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
            return new ToolExecutionResult<T>(
                ToolExecutionStatus.Cancelled,
                default,
                null,
                startedAt,
                timeProvider.GetUtcNow(),
                false);
        }
        catch (Exception exception)
        {
            await PublishSafelyAsync(
                    new UserNotification.Models.UserNotification(
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
                timeProvider.GetUtcNow(),
                false);
        }
        finally
        {
            lock (gate)
            {
                running.Remove(request.OperationId);
            }

            linked.Dispose();
        }
    }

    private async Task PublishSafelyAsync(UserNotification.Models.UserNotification notification)
    {
        try
        {
            await notifications.PublishAsync(
                    notification,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // Presentation subscribers must not change the tool's terminal result.
        }
    }

    private sealed class RunningOperation(CancellationTokenSource cancellation)
    {
        internal CancellationTokenSource Cancellation { get; } = cancellation;

        internal Task Task { get; set; } = Task.CompletedTask;

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
