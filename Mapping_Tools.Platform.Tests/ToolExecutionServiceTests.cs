using Mapping_Tools.ApplicationServices.BeatmapEditing;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class ToolExecutionServiceTests
{
    [TestMethod]
    public async Task SuccessRunsOffCallerThreadReportsProgressReloadsAndPublishesSummary()
    {
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        RecordingReloadService reload = new();
        ToolExecutionService service = CreateService(
            notifications,
            reload,
            new ApplicationSettings { AutoReload = true });
        List<ToolExecutionProgress> progress = [];
        int callerThread = Environment.CurrentManagedThreadId;
        int operationThread = callerThread;
        ToolExecutionRequest<int> request = new(
            "cleaner",
            "Map Cleaner",
            context =>
            {
                operationThread = Environment.CurrentManagedThreadId;
                context.ReportProgress(25, "Loading maps");
                context.ReportProgress(100, "Saved");
                return Task.FromResult(
                    new ToolExecutionOutput<int>(
                        42,
                        "Removed 3 greenlines.",
                        reloadEditor: true));
            });

        ToolExecutionResult<int> result = await service.ExecuteAsync(
            request,
            new InlineProgress<ToolExecutionProgress>(progress.Add));

        Assert.AreEqual(ToolExecutionStatus.Succeeded, result.Status);
        Assert.AreEqual(42, result.Value);
        Assert.AreNotEqual(callerThread, operationThread);
        Assert.AreEqual(2, progress.Count);
        Assert.AreEqual(100, progress[1].Percent);
        Assert.AreEqual(1, reload.ReloadCount);
        Assert.IsTrue(result.EditorReloaded);
        Assert.AreEqual(1, published.Count);
        Assert.AreEqual(UserNotificationSeverity.Success, published[0].Severity);
        Assert.AreEqual("Removed 3 greenlines.", published[0].Message);
        Assert.IsFalse(service.IsRunning("cleaner"));
    }

    [TestMethod]
    public async Task AutoReloadPreferenceSuppressesRequestedReload()
    {
        RecordingReloadService reload = new();
        ToolExecutionService service = CreateService(
            new UserNotificationService(),
            reload,
            new ApplicationSettings { AutoReload = false });
        ToolExecutionRequest<int> request = new(
            "tool",
            "Tool",
            _ => Task.FromResult(
                new ToolExecutionOutput<int>(
                    1,
                    reloadEditor: true)));

        ToolExecutionResult<int> result = await service.ExecuteAsync(request);

        Assert.AreEqual(ToolExecutionStatus.Succeeded, result.Status);
        Assert.AreEqual(0, reload.ReloadCount);
        Assert.IsFalse(result.EditorReloaded);
    }

    [TestMethod]
    public async Task FailureBecomesTypedResultAndErrorNotification()
    {
        UserNotificationService notifications = new();
        UserNotification? published = null;
        notifications.Published += (_, args) => published = args.Notification;
        InvalidDataException failure = new("Invalid timing section.");
        ToolExecutionService service = CreateService(
            notifications,
            new RecordingReloadService(),
            new ApplicationSettings());
        ToolExecutionRequest<int> request = new(
            "timing",
            "Timing Helper",
            _ => Task.FromException<ToolExecutionOutput<int>>(failure));

        ToolExecutionResult<int> result = await service.ExecuteAsync(request);

        Assert.AreEqual(ToolExecutionStatus.Failed, result.Status);
        Assert.AreSame(failure, result.Exception);
        Assert.IsNotNull(published);
        Assert.AreEqual(UserNotificationSeverity.Error, published.Severity);
        Assert.AreSame(failure, published.Exception);
    }

    [TestMethod]
    public async Task BrokenNotificationSubscriberDoesNotChangeSuccessfulResult()
    {
        UserNotificationService notifications = new();
        notifications.Published += (_, _) =>
            throw new InvalidOperationException("Presentation failed.");
        ToolExecutionService service = CreateService(notifications);
        ToolExecutionRequest<int> request = new(
            "cleaner",
            "Map Cleaner",
            _ => Task.FromResult(
                new ToolExecutionOutput<int>(
                    4,
                    "Removed four redundant timing points.")));

        ToolExecutionResult<int> result = await service.ExecuteAsync(request);

        Assert.AreEqual(ToolExecutionStatus.Succeeded, result.Status);
        Assert.AreEqual(4, result.Value);
    }

    [TestMethod]
    public async Task DuplicateOperationReturnsBusyWithoutStartingSecondDelegate()
    {
        ToolExecutionService service = CreateService();
        TaskCompletionSource firstStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        int secondRuns = 0;
        ToolExecutionRequest<int> first = new(
            "sliderator",
            "Sliderator",
            async context =>
            {
                firstStarted.SetResult();
                await release.Task.WaitAsync(context.CancellationToken);
                return new ToolExecutionOutput<int>(1);
            });
        Task<ToolExecutionResult<int>> firstTask = service.ExecuteAsync(first);
        await firstStarted.Task;
        ToolExecutionRequest<int> duplicate = new(
            "sliderator",
            "Sliderator",
            _ =>
            {
                secondRuns++;
                return Task.FromResult(new ToolExecutionOutput<int>(2));
            });

        ToolExecutionResult<int> duplicateResult =
            await service.ExecuteAsync(duplicate);
        release.SetResult();
        ToolExecutionResult<int> firstResult = await firstTask;

        Assert.AreEqual(
            ToolExecutionStatus.AlreadyRunning,
            duplicateResult.Status);
        Assert.AreEqual(0, secondRuns);
        Assert.AreEqual(ToolExecutionStatus.Succeeded, firstResult.Status);
    }

    [TestMethod]
    public async Task TargetedCancelSignalsContextAndReturnsCancelledResult()
    {
        ToolExecutionService service = CreateService();
        TaskCompletionSource started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        ToolExecutionRequest<int> request = new(
            "picturator",
            "Slider Picturator",
            async context =>
            {
                started.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
                return new ToolExecutionOutput<int>(1);
            });
        Task<ToolExecutionResult<int>> execution = service.ExecuteAsync(request);
        await started.Task;

        bool cancelled = service.Cancel("picturator");
        ToolExecutionResult<int> result = await execution;

        Assert.IsTrue(cancelled);
        Assert.AreEqual(ToolExecutionStatus.Cancelled, result.Status);
        Assert.IsFalse(service.Cancel("picturator"));
    }

    [TestMethod]
    public async Task CallerCancellationReturnsCancelledResult()
    {
        ToolExecutionService service = CreateService();
        using CancellationTokenSource source = new();
        ToolExecutionRequest<int> request = new(
            "merger",
            "Mapset Merger",
            async context =>
            {
                source.Cancel();
                await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
                return new ToolExecutionOutput<int>(1);
            });

        ToolExecutionResult<int> result = await service.ExecuteAsync(
            request,
            cancellationToken: source.Token);

        Assert.AreEqual(ToolExecutionStatus.Cancelled, result.Status);
    }

    [TestMethod]
    public async Task StopCancelsAndJoinsEveryActiveOperation()
    {
        ToolExecutionService service = CreateService();
        TaskCompletionSource firstStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<ToolExecutionResult<int>> first = service.ExecuteAsync(
            BlockingRequest("one", firstStarted));
        Task<ToolExecutionResult<int>> second = service.ExecuteAsync(
            BlockingRequest("two", secondStarted));
        await Task.WhenAll(firstStarted.Task, secondStarted.Task);

        await service.StopAsync();
        ToolExecutionResult<int>[] results = await Task.WhenAll(first, second);

        Assert.IsTrue(results.All(
            result => result.Status == ToolExecutionStatus.Cancelled));
        Assert.IsFalse(service.IsRunning("one"));
        Assert.IsFalse(service.IsRunning("two"));
    }

    [TestMethod]
    public void ProgressRejectsNaNAndOutOfRangeValues()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => new ToolExecutionProgress(double.NaN));
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => new ToolExecutionProgress(-0.01));
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => new ToolExecutionProgress(100.01));
    }

    [TestMethod]
    public async Task NotificationCancellationPreventsPublication()
    {
        UserNotificationService notifications = new();
        int published = 0;
        notifications.Published += (_, _) => published++;
        using CancellationTokenSource source = new();
        source.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => notifications.PublishAsync(
                new UserNotification(
                    UserNotificationSeverity.Information,
                    "Title",
                    "Message"),
                source.Token));

        Assert.AreEqual(0, published);
    }

    private static ToolExecutionRequest<int> BlockingRequest(
        string id,
        TaskCompletionSource started)
    {
        return new ToolExecutionRequest<int>(
            id,
            id,
            async context =>
            {
                started.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
                return new ToolExecutionOutput<int>(1);
            });
    }

    private static ToolExecutionService CreateService(
        IUserNotificationService? notifications = null,
        RecordingReloadService? reload = null,
        ApplicationSettings? settings = null)
    {
        return new ToolExecutionService(
            notifications ?? new UserNotificationService(),
            reload ?? new RecordingReloadService(),
            settings ?? new ApplicationSettings(),
            TimeProvider.System);
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public InlineProgress(Action<T> report)
        {
            _report = report;
        }

        public void Report(T value)
        {
            _report(value);
        }
    }

    private sealed class RecordingReloadService : IEditorReloadService
    {
        public int ReloadCount { get; private set; }

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReloadCount++;
            return Task.CompletedTask;
        }
    }
}
