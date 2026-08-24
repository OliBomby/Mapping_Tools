using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.SliderMerger;
using Mapping_Tools.Core.Tools.SliderMerger.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class SliderMergerViewModelTests
{
    [TestMethod]
    public async Task RunCommand_WithEverythingAndBezierModePassesProjectValuesAndWorkspacePaths()
    {
        // Arrange
        RecordingMerger service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["one.osu", "two.osu"]);
        var viewModel = Create(service, workspace);
        viewModel.ImportModeSetting = SliderMergerImportMode.Everything;
        viewModel.ConnectionModeSetting = SliderMergerConnectionMode.Bezier;
        viewModel.Leniency = 512;
        viewModel.MergeOnSliderEnd = false;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().Equal("one.osu", "two.osu");
        service.Options.Should().NotBeNull();
        service.Options!.ImportModeSetting.Should().Be(SliderMergerImportMode.Everything);
        service.Options.ConnectionModeSetting.Should().Be(SliderMergerConnectionMode.Bezier);
        service.Options.Leniency.Should().Be(512);
        service.Options.MergeOnSliderEnd.Should().BeFalse();
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunQuickAsync_WithCurrentBeatmapUsesCurrentPath()
    {
        // Arrange
        RecordingMerger service = new();
        var viewModel = Create(
            service,
            new TestBeatmapWorkspace(),
            new RecordingCurrentBeatmapLocator("current.osu"));

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.Paths.Should().Equal("current.osu");
        service.Options!.ImportModeSetting.Should().Be(SliderMergerImportMode.Selected);
    }

    [TestMethod]
    public async Task RunQuickAsync_WithAutoReloadEnabled_ReloadsEditorAfterSuccessfulMerge()
    {
        // Arrange
        RecordingMerger service = new();
        RecordingEditorReloadService reload = new();
        ApplicationSettings settings = new() { AutoReload = true };
        var viewModel = Create(
            service,
            currentBeatmap: new RecordingCurrentBeatmapLocator("current.osu"),
            settings: settings,
            reload: reload);

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        reload.ReloadCount.Should().Be(1);
    }

    [TestMethod]
    public void TimeCodeVisibility_WhenTimeModeIsSelectedIsVisible()
    {
        // Arrange
        var viewModel = Create(new RecordingMerger());

        // Act
        viewModel.ImportModeSetting = SliderMergerImportMode.Time;

        // Assert
        viewModel.TimeCodeVisible.Should().BeTrue();
        viewModel.ConnectionModes.Should().Contain(SliderMergerConnectionMode.Bezier);
    }

    [TestMethod]
    public async Task RunCommand_WithNegativeLeniencyDoesNotInvokeService()
    {
        // Arrange
        RecordingMerger service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = Create(service, workspace);
        viewModel.ImportModeSetting = SliderMergerImportMode.Everything;
        viewModel.Leniency = -1;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().BeNull();
        viewModel.HasErrors.Should().BeTrue();
        viewModel.IsRunning.Should().BeFalse();
    }

    private static SliderMergerViewModel Create(
        RecordingMerger service,
        TestBeatmapWorkspace? workspace = null,
        RecordingCurrentBeatmapLocator? currentBeatmap = null,
        ApplicationSettings? settings = null,
        RecordingEditorReloadService? reload = null)
    {
        var effectiveSettings = settings ?? new ApplicationSettings();
        return new SliderMergerViewModel(
            service,
            new ToolExecutionService(
                new UserNotificationService(),
                reload ?? new RecordingEditorReloadService(),
                effectiveSettings,
                TimeProvider.System),
            currentBeatmap ?? new RecordingCurrentBeatmapLocator(null),
            workspace ?? new TestBeatmapWorkspace(),
            effectiveSettings);
    }

    private sealed class RecordingMerger : ISliderMergerService
    {
        public IReadOnlyList<string>? Paths { get; private set; }

        public SliderMergerOptions? Options { get; private set; }

        public Task<SliderMergerResult> MergeAsync(
            IReadOnlyList<string> paths,
            SliderMergerOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Paths = paths.ToArray();
            Options = options;
            progress?.Report(100);
            return Task.FromResult(new SliderMergerResult(paths, 2));
        }
    }

}
