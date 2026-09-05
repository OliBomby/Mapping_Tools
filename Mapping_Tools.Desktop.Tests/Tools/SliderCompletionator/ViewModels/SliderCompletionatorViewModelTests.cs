using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.SliderCompletionator.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.SliderCompletionator.ViewModels;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.SliderCompletionator.ViewModels;

[TestClass]
public sealed class SliderCompletionatorViewModelTests
{
    [TestMethod]
    public async Task RunCommand_WithEverythingMode_PassesProjectValuesAndWorkspacePaths()
    {
        // Arrange
        RecordingCompletionator service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["one.osu", "two.osu"]);
        var viewModel = Create(service, workspace);
        viewModel.ImportModeSetting = HitObjectSelectionMode.Everything;
        viewModel.FreeVariableSetting = SliderCompletionatorFreeVariable.Length;
        viewModel.Duration = 1.5;
        viewModel.MoveAnchors = true;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().Equal("one.osu", "two.osu");
        service.Options.Should().NotBeNull();
        service.Options!.ImportModeSetting.Should().Be(HitObjectSelectionMode.Everything);
        service.Options.FreeVariableSetting.Should().Be(SliderCompletionatorFreeVariable.Length);
        service.Options.Duration.Should().Be(1.5);
        service.Options.MoveAnchors.Should().BeTrue();
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunQuickAsync_WithCurrentBeatmap_UsesQuickReloadAndSelectedMode()
    {
        // Arrange
        RecordingCompletionator service = new();
        var viewModel = Create(
            service,
            new TestBeatmapWorkspace(),
            new RecordingCurrentBeatmapLocator("current.osu"));

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.Paths.Should().Equal("current.osu");
        service.Options!.ImportModeSetting.Should().Be(HitObjectSelectionMode.Selected);
    }

    [TestMethod]
    public async Task RunCommand_WithSelectedModeAndUnavailableCurrentBeatmap_ThrowsWithoutInvokingService()
    {
        // Arrange
        RecordingCompletionator service = new();
        var viewModel = Create(
            service,
            currentBeatmap: new RecordingCurrentBeatmapLocator(null));

        // Act
        Func<Task> act = () => viewModel.RunCommand.ExecuteAsync(null);
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();

        // Assert
        service.Paths.Should().BeNull();
        exception.Which.Message.Should().Contain("Open a beatmap in osu!");
    }

    [TestMethod]
    public void RunQuickAsync_WithAsynchronousCurrentBeatmapLookupAndCurrentEditorTime_KeepsRunStateOnCallingContext()
    {
        // Arrange
        int callingThread = Environment.CurrentManagedThreadId;
        PumpingSynchronizationContext synchronizationContext = new();
        SynchronizationContext? previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        try
        {
            RecordingCompletionator service = new();
            AsynchronousCurrentBeatmapLocator currentBeatmap = new("current.osu");
            var viewModel = Create(
                service,
                currentBeatmap: currentBeatmap);
            viewModel.UseEndTime = true;
            viewModel.UseCurrentEditorTime = true;
            List<int> stateChangeThreads = [];
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.IsRunning))
                    stateChangeThreads.Add(Environment.CurrentManagedThreadId);
            };

            // Act
            Task run = viewModel.RunQuickAsync(CancellationToken.None);
            currentBeatmap.Complete();
            synchronizationContext.RunUntilCompleted(run);

            // Assert
            stateChangeThreads.Should().NotBeEmpty();
            stateChangeThreads.Should().OnlyContain(thread => thread == callingThread);
            service.Options!.UseEndTime.Should().BeTrue();
            service.Options.UseCurrentEditorTime.Should().BeTrue();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    [TestMethod]
    public async Task RunCommand_WithAlwaysQuickRunAndNonSelectedMode_UsesWorkspacePaths()
    {
        // Arrange
        RecordingCompletionator service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["one.osu", "two.osu"]);
        DesktopApplicationSettings settings = new() { AlwaysQuickRun = true };
        var viewModel = Create(
            service,
            workspace,
            settings: settings);
        viewModel.ImportModeSetting = HitObjectSelectionMode.Everything;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().Equal("one.osu", "two.osu");
    }

    [TestMethod]
    public async Task RunCommand_WithNonFiniteDuration_DoesNotInvokeService()
    {
        // Arrange
        RecordingCompletionator service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = Create(service, workspace);
        viewModel.Duration = double.PositiveInfinity;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().BeNull();
        viewModel.HasErrors.Should().BeTrue();
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void Visibility_WhenEndTimeIsEnabled_HidesDurationAndShowsEndTime()
    {
        // Arrange
        var viewModel = Create(new RecordingCompletionator());

        // Act
        viewModel.UseEndTime = true;

        // Assert
        viewModel.DurationVisible.Should().BeFalse();
        viewModel.EndTimeVisible.Should().BeTrue();
    }

    [TestMethod]
    public void Visibility_WhenCurrentEditorTimeAndLengthAreSelected_HidesEndTimeAndLength()
    {
        // Arrange
        var viewModel = Create(new RecordingCompletionator());
        viewModel.UseEndTime = true;

        // Act
        viewModel.UseCurrentEditorTime = true;
        viewModel.FreeVariableSetting = SliderCompletionatorFreeVariable.Length;

        // Assert
        viewModel.EndTimeVisible.Should().BeFalse();
        viewModel.LengthVisible.Should().BeFalse();
        viewModel.VelocityVisible.Should().BeTrue();
    }

    private static SliderCompletionatorViewModel Create(
        RecordingCompletionator service,
        TestBeatmapWorkspace? workspace = null,
        ICurrentBeatmapLocator? currentBeatmap = null,
        DesktopApplicationSettings? settings = null)
    {
        return new SliderCompletionatorViewModel(
            service,
            new ToolExecutionService(
                new UserNotificationService(),
                new RecordingEditorReloadService(),
                new DesktopApplicationSettings(),
                TimeProvider.System),
            currentBeatmap ?? new RecordingCurrentBeatmapLocator(null),
            workspace ?? new TestBeatmapWorkspace(),
            settings ?? new DesktopApplicationSettings());
    }

    private sealed class AsynchronousCurrentBeatmapLocator(string path) : ICurrentBeatmapLocator
    {
        private readonly TaskCompletionSource<string> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<string> FindCurrentBeatmapAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return completion.Task;
        }

        public void Complete() => completion.TrySetResult(path);
    }

    private sealed class PumpingSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> work = [];

        public void RunUntilCompleted(Task task)
        {
            while (!task.IsCompleted)
            {
                (SendOrPostCallback Callback, object? State) item;
                lock (work)
                {
                    while (work.Count == 0 && !task.IsCompleted) Monitor.Wait(work, 100);
                    if (task.IsCompleted) break;

                    item = work.Dequeue();
                }

                item.Callback(item.State);
            }

            task.GetAwaiter().GetResult();
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (work)
            {
                work.Enqueue((d, state));
                Monitor.PulseAll(work);
            }
        }
    }

    private sealed class RecordingCompletionator : ISliderCompletionatorService
    {
        public IReadOnlyList<string>? Paths { get; private set; }

        public SliderCompletionatorServiceOptions? Options { get; private set; }

        public Task<SliderCompletionatorResult> CompleteAsync(
            IReadOnlyList<string> paths,
            SliderCompletionatorServiceOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Paths = paths.ToArray();
            Options = options;
            progress?.Report(1);
            return Task.FromResult(new SliderCompletionatorResult(paths, 2));
        }
    }

}
