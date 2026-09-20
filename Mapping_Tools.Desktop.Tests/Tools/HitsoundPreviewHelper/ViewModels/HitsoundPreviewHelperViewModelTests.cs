using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Services;
using Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundPreviewHelper.ViewModels;

[TestClass]
public sealed class HitsoundPreviewHelperViewModelTests
{
    [TestMethod]
    public void AddCopyAndRemoveCommands_WithSelectedZones_KeepCollectionStateConsistent()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AddCommand.Execute(null);
        viewModel.Items[0].IsSelected = true;
        viewModel.CopyCommand.Execute(null);
        viewModel.Items[0].IsSelected = true;
        viewModel.Items[1].IsSelected = true;
        viewModel.RemoveCommand.Execute(null);

        // Assert
        viewModel.Items.Should().BeEmpty();
    }

    [TestMethod]
    public async Task RunCommand_UsesSelectedWorkspaceMapsAndPublishesSuccess()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["first.osu", "second.osu"]);
        RecordingPreviewService preview = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var viewModel = CreateViewModel(
            preview,
            workspace,
            notifications: notifications);
        viewModel.AddCommand.Execute(null);

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        preview.Options.Should().NotBeNull();
        preview.Paths.Should().Equal("first.osu", "second.osu");
        preview.Options!.Items.Should().ContainSingle();
        published.Where(notification =>
                notification.Severity == UserNotificationSeverity.Success
                && notification.Message == "Done!")
            .Should().ContainSingle();
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunQuickAsync_WithCurrentBeatmap_PassesQuickRunToService()
    {
        // Arrange
        RecordingPreviewService preview = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var viewModel = CreateViewModel(preview, notifications: notifications);
        viewModel.AddCommand.Execute(null);

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        preview.Paths.Should().Equal("current.osu");
        preview.QuickRun.Should().BeTrue();
        published.Where(notification =>
                notification.Severity == UserNotificationSeverity.Success
                && notification.Message == "Placed 1 preview hitsounds.")
            .Should().ContainSingle();
    }

    [TestMethod]
    public async Task AddFromSelectionCommand_WithLiveCoordinates_AddsDistinctZones()
    {
        // Arrange
        RecordingPreviewService preview = new()
        {
            Positions = [new Vector2(64, 192), new Vector2(256, 192)],
        };
        var viewModel = CreateViewModel(preview);

        // Act
        await viewModel.AddFromSelectionCommand.ExecuteAsync(null);

        // Assert
        viewModel.Items.Should().HaveCount(2);
        viewModel.Items.Select(item => (item.XPos, item.YPos))
            .Should().Equal((64d, 192d), (256d, 192d));
    }

    [TestMethod]
    public void OpenRhythmGuideCommand_UsesSharedAuxiliaryWindowBoundary()
    {
        // Arrange
        RecordingRhythmGuideWindowService windows = new();
        var viewModel = CreateViewModel(windowService: windows);

        // Act
        viewModel.OpenRhythmGuideCommand.Execute(null);

        // Assert
        windows.ViewModel.Should().NotBeNull();
    }

    private static HitsoundPreviewHelperViewModel CreateViewModel(
        RecordingPreviewService? preview = null,
        TestBeatmapWorkspace? workspace = null,
        RecordingRhythmGuideWindowService? windowService = null,
        IUserNotificationService? notifications = null)
    {
        var notificationService = notifications ?? new UserNotificationService();
        ToolExecutionService execution = new(
            notificationService,
            TimeProvider.System);
        var windows = windowService ?? new RecordingRhythmGuideWindowService();
        var effectiveWorkspace = workspace
                                 ?? new TestBeatmapWorkspace
                                 {
                                     QuickRunPath = "current.osu",
                                 };
        RhythmGuideViewModel rhythmGuide = new(
            new StubRhythmGuideService(),
            execution,
            new TestFilePicker(),
            new TestFileRevealService(),
            new RecordingCurrentBeatmapLocator("current.osu"),
            effectiveWorkspace,
            windows,
            new TestApplicationDirectories());
        return new HitsoundPreviewHelperViewModel(
            preview ?? new RecordingPreviewService(),
            execution,
            effectiveWorkspace,
            new RecordingCurrentBeatmapLocator("current.osu"),
            new DesktopApplicationSettings(),
            notificationService,
            windows,
            rhythmGuide,
            new TestApplicationDirectories());
    }

    private sealed class RecordingPreviewService : IHitsoundPreviewHelperService
    {
        public IReadOnlyList<Vector2> Positions { get; set; } = [];

        public IReadOnlyList<string>? Paths { get; private set; }

        public HitsoundPreviewHelperServiceOptions? Options { get; private set; }

        public bool QuickRun { get; private set; }

        public Task<HitsoundPreviewHelperResult> ApplyAsync(
            IReadOnlyList<string> paths,
            HitsoundPreviewHelperServiceOptions options,
            bool quickRun = false,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Paths = paths.ToArray();
            Options = options;
            QuickRun = quickRun;
            progress?.Report(1);
            return Task.FromResult(new HitsoundPreviewHelperResult(
                paths,
                options.Items.Count));
        }

        public Task<IReadOnlyList<Vector2>> GetSelectedZonePositionsAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Positions);
        }
    }

    private sealed class StubRhythmGuideService : IRhythmGuideService
    {
        public Task<RhythmGuideResult> GenerateAsync(
            RhythmGuideServiceOptions.RhythmGuideRunOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RhythmGuideResult(
                options.ExportPath,
                0,
                options.ExportMode));
        }
    }

    private sealed class RecordingRhythmGuideWindowService : IRhythmGuideWindowService
    {
        public RhythmGuideViewModel? ViewModel { get; private set; }

        public void Show(RhythmGuideViewModel viewModel)
        {
            ViewModel = viewModel;
        }
    }
}
