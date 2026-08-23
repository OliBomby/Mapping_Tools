using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

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
    public async Task RunCommand_WithEverythingMode_UsesSelectedWorkspaceMapsAndPublishesLegacyCompletion()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["first.osu", "second.osu"]);
        RecordingPreviewService preview = new();
        var viewModel = CreateViewModel(
            preview,
            workspace);
        viewModel.AddCommand.Execute(null);

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        preview.Options.Should().NotBeNull();
        preview.Paths.Should().Equal("first.osu", "second.osu");
        preview.Options!.ImportModeSetting.Should().Be(
            HitsoundPreviewHelperImportMode.Everything);
        viewModel.ResultSummary.Should().Be("Done!");
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunQuickAsync_WithCurrentBeatmap_RequestsEditorReloadThroughExecutionHost()
    {
        // Arrange
        RecordingPreviewService preview = new();
        RecordingEditorReloadService reload = new();
        var viewModel = CreateViewModel(
            preview,
            reloadService: reload);
        viewModel.AddCommand.Execute(null);

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        preview.Paths.Should().Equal("current.osu");
        reload.ReloadCount.Should().Be(1);
        viewModel.ResultSummary.Should().Be("Placed 1 preview hitsounds.");
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
        RecordingEditorReloadService? reloadService = null)
    {
        UserNotificationService notifications = new();
        ToolExecutionService execution = new(
            notifications,
            reloadService ?? new RecordingEditorReloadService(),
            new ApplicationSettings(),
            TimeProvider.System);
        var windows = windowService ?? new RecordingRhythmGuideWindowService();
        RhythmGuideViewModel rhythmGuide = new(
            new StubRhythmGuideService(),
            execution,
            new TestFilePicker(),
            new RecordingCurrentBeatmapLocator("current.osu"),
            windows,
            new TestApplicationDirectories());
        return new HitsoundPreviewHelperViewModel(
            preview ?? new RecordingPreviewService(),
            execution,
            workspace ?? new TestBeatmapWorkspace(),
            new RecordingCurrentBeatmapLocator("current.osu"),
            new ApplicationSettings(),
            notifications,
            windows,
            rhythmGuide,
            new TestApplicationDirectories());
    }

    private sealed class RecordingPreviewService : IHitsoundPreviewHelperService
    {
        public IReadOnlyList<Vector2> Positions { get; set; } = [];

        public IReadOnlyList<string>? Paths { get; private set; }

        public HitsoundPreviewHelperOptions? Options { get; private set; }

        public Task<HitsoundPreviewHelperResult> ApplyAsync(
            IReadOnlyList<string> paths,
            HitsoundPreviewHelperOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Paths = paths.ToArray();
            Options = options;
            progress?.Report(100);
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
            RhythmGuideOptions options,
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
