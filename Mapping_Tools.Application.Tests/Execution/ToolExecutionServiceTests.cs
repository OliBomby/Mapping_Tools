using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Execution;

[TestClass]
public sealed class ToolExecutionServiceTests
{
    [TestMethod]
    public async Task ExecuteAsync_WithSuccessfulTool_RunsOffThreadAndPublishesSummary()
    {
        // Arrange
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        RecordingEditorReloadService reload = new();
        var service = CreateService(
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
                context.ReportProgress(0.25, "Loading maps");
                context.ReportProgress(1, "Saved");
                return Task.FromResult(
                    new ToolExecutionOutput<int>(
                        42,
                        "Removed 3 greenlines.",
                        true));
            });

        // Act
        var result = await service.ExecuteAsync(
            request,
            new InlineProgress<ToolExecutionProgress>(progress.Add));

        // Assert
        result.Status.Should().Be(ToolExecutionStatus.Succeeded);
        result.Value.Should().Be(42);
        operationThread.Should().NotBe(callerThread);
        progress.Count.Should().Be(2);
        progress[1].Progress.Should().Be(1);
        reload.ReloadCount.Should().Be(1);
        result.EditorReloaded.Should().BeTrue();
        published.Count.Should().Be(1);
        published[0].Severity.Should().Be(UserNotificationSeverity.Success);
        published[0].Message.Should().Be("Removed 3 greenlines.");
        service.IsRunning("cleaner").Should().BeFalse();
    }

    [TestMethod]
    public async Task ExecuteAsync_WithAutoReloadDisabled_SuppressesRequestedReload()
    {
        // Arrange
        RecordingEditorReloadService reload = new();
        var service = CreateService(
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

        // Act
        var result = await service.ExecuteAsync(request);

        // Assert
        result.Status.Should().Be(ToolExecutionStatus.Succeeded);
        reload.ReloadCount.Should().Be(0);
        result.EditorReloaded.Should().BeFalse();
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenToolFails_ReturnsFailureAndNotification()
    {
        // Arrange
        UserNotificationService notifications = new();
        UserNotification? published = null;
        notifications.Published += (_, args) => published = args.Notification;
        InvalidDataException failure = new("Invalid timing section.");
        var service = CreateService(
            notifications,
            new RecordingEditorReloadService(),
            new ApplicationSettings());
        ToolExecutionRequest<int> request = new(
            "timing",
            "Timing Helper",
            _ => Task.FromException<ToolExecutionOutput<int>>(failure));

        // Act
        var result = await service.ExecuteAsync(request);

        // Assert
        result.Status.Should().Be(ToolExecutionStatus.Failed);
        result.Exception.Should().BeSameAs(failure);
        published.Should().NotBeNull();
        published.Severity.Should().Be(UserNotificationSeverity.Error);
        published.Exception.Should().BeSameAs(failure);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithBrokenNotificationSubscriber_PreservesSuccess()
    {
        // Arrange
        UserNotificationService notifications = new();
        notifications.Published += (_, _) =>
            throw new InvalidOperationException("Presentation failed.");
        var service = CreateService(notifications);
        ToolExecutionRequest<int> request = new(
            "cleaner",
            "Map Cleaner",
            _ => Task.FromResult(
                new ToolExecutionOutput<int>(
                    4,
                    "Removed four redundant timing points.")));

        // Act
        var result = await service.ExecuteAsync(request);

        // Assert
        result.Status.Should().Be(ToolExecutionStatus.Succeeded);
        result.Value.Should().Be(4);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithDuplicateOperation_ReturnsBusyWithoutSecondDelegate()
    {
        // Arrange
        var service = CreateService();
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
        var firstTask = service.ExecuteAsync(first);
        await firstStarted.Task;
        ToolExecutionRequest<int> duplicate = new(
            "sliderator",
            "Sliderator",
            _ =>
            {
                secondRuns++;
                return Task.FromResult(new ToolExecutionOutput<int>(2));
            });

        // Act
        var duplicateResult =
            await service.ExecuteAsync(duplicate);
        release.SetResult();
        var firstResult = await firstTask;

        // Assert
        duplicateResult.Status.Should().Be(ToolExecutionStatus.AlreadyRunning);
        secondRuns.Should().Be(0);
        firstResult.Status.Should().Be(ToolExecutionStatus.Succeeded);
    }

    [TestMethod]
    public async Task Cancel_WithTargetedOperation_CancelsContextAndReturnsCancelled()
    {
        // Arrange
        var service = CreateService();
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
        var execution = service.ExecuteAsync(request);
        await started.Task;

        // Act
        bool cancelled = service.Cancel("picturator");
        var result = await execution;

        // Assert
        cancelled.Should().BeTrue();
        result.Status.Should().Be(ToolExecutionStatus.Cancelled);
        service.Cancel("picturator").Should().BeFalse();
    }

    [TestMethod]
    public async Task ExecuteAsync_WithCallerCancellation_ReturnsCancelled()
    {
        // Arrange
        var service = CreateService();
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

        // Act
        var result = await service.ExecuteAsync(
            request,
            cancellationToken: source.Token);

        // Assert
        result.Status.Should().Be(ToolExecutionStatus.Cancelled);
    }

    [TestMethod]
    public async Task StopAsync_WithActiveOperations_CancelsAndJoinsAll()
    {
        // Arrange
        var service = CreateService();
        TaskCompletionSource firstStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = service.ExecuteAsync(
            BlockingRequest("one", firstStarted));
        var second = service.ExecuteAsync(
            BlockingRequest("two", secondStarted));
        await Task.WhenAll(firstStarted.Task, secondStarted.Task);

        // Act
        await service.StopAsync();
        var results = await Task.WhenAll(first, second);

        // Assert
        results.All(result => result.Status == ToolExecutionStatus.Cancelled).Should().BeTrue();
        service.IsRunning("one").Should().BeFalse();
        service.IsRunning("two").Should().BeFalse();
    }

    [TestMethod]
    public void Constructor_WithInvalidProgress_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // Act
        Action act1 = () => new ToolExecutionProgress(double.NaN);

        // Assert
        act1.Should().Throw<ArgumentOutOfRangeException>();
        Action act2 = () => new ToolExecutionProgress(-0.01);

        act2.Should().Throw<ArgumentOutOfRangeException>();
        Action act3 = () => new ToolExecutionProgress(1.01);

        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public async Task PublishAsync_WithCancellation_DoesNotPublish()
    {
        // Arrange
        UserNotificationService notifications = new();
        int published = 0;
        notifications.Published += (_, _) => published++;
        using CancellationTokenSource source = new();
        source.Cancel();

        // Act
        var act4 = () => notifications.PublishAsync(
            new UserNotification(
                UserNotificationSeverity.Information,
                "Title",
                "Message"),
            source.Token);

        // Assert
        await act4.Should().ThrowAsync<OperationCanceledException>();

        published.Should().Be(0);
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
        RecordingEditorReloadService? reload = null,
        ApplicationSettings? settings = null)
    {
        return new ToolExecutionService(
            notifications ?? new UserNotificationService(),
            reload ?? new RecordingEditorReloadService(),
            settings ?? new ApplicationSettings(),
            TimeProvider.System);
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> report;

        public InlineProgress(Action<T> report)
        {
            this.report = report;
        }

        public void Report(T value)
        {
            report(value);
        }
    }

}
