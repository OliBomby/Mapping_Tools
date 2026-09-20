using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.SliderMerger.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.SliderMerger.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.SliderMerger.ViewModels;

[TestClass]
public sealed class SliderMergerViewModelTests
{
    [TestMethod]
    public async Task RunCommand_WithEverythingAndLinearMode_PassesProjectValuesAndWorkspacePaths()
    {
        // Arrange
        RecordingMerger service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["one.osu", "two.osu"]);
        var viewModel = Create(service, workspace);
        viewModel.ImportModeSetting = HitObjectSelectionMode.Everything;
        viewModel.ConnectionModeSetting = SliderMergerConnectionMode.Linear;
        viewModel.Leniency = 512;
        viewModel.MergeOnSliderEnd = false;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().Equal("one.osu", "two.osu");
        service.Options.Should().NotBeNull();
        service.Options!.ImportModeSetting.Should().Be(HitObjectSelectionMode.Everything);
        service.Options.ConnectionModeSetting.Should().Be(SliderMergerConnectionMode.Linear);
        service.Options.Leniency.Should().Be(512);
        service.Options.MergeOnSliderEnd.Should().BeFalse();
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunQuickAsync_WithCurrentBeatmap_UsesCurrentPath()
    {
        // Arrange
        RecordingMerger service = new();
        TestBeatmapWorkspace workspace = new() { QuickRunPath = "current.osu" };
        var viewModel = Create(service, workspace);

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.Paths.Should().Equal("current.osu");
        service.Options!.ImportModeSetting.Should().Be(HitObjectSelectionMode.Selected);
    }

    [TestMethod]
    public async Task RunCommand_WithSelectedModeAndNoLiveBeatmap_UsesSelectedWorkspaceFallback()
    {
        // Arrange
        RecordingMerger service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = Create(service, workspace);

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().Equal("selected.osu");
    }

    [TestMethod]
    public async Task RunQuickAsync_PassesQuickRunToService()
    {
        // Arrange
        RecordingMerger service = new();
        TestBeatmapWorkspace workspace = new() { QuickRunPath = "current.osu" };
        var viewModel = Create(
            service,
            workspace);

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.QuickRun.Should().BeTrue();
    }

    [TestMethod]
    public void TimeCodeVisibility_WhenTimeModeIsSelected_IsVisible()
    {
        // Arrange
        var viewModel = Create(new RecordingMerger());

        // Act
        viewModel.ImportModeSetting = HitObjectSelectionMode.Time;

        // Assert
        viewModel.TimeCodeVisible.Should().BeTrue();
        viewModel.ConnectionModes.Should().Equal(
            SliderMergerConnectionMode.Move,
            SliderMergerConnectionMode.Linear);
    }

    [TestMethod]
    public async Task RunCommand_WithNegativeLeniency_DoesNotInvokeService()
    {
        // Arrange
        RecordingMerger service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = Create(service, workspace);
        viewModel.ImportModeSetting = HitObjectSelectionMode.Everything;
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
        DesktopApplicationSettings? settings = null)
    {
        var effectiveSettings = settings ?? new DesktopApplicationSettings();
        return new SliderMergerViewModel(
            service,
            new ToolExecutionService(
                new UserNotificationService(),
                TimeProvider.System),
            workspace ?? new TestBeatmapWorkspace(),
            effectiveSettings);
    }

    private sealed class RecordingMerger : ISliderMergerService
    {
        public IReadOnlyList<string>? Paths { get; private set; }

        public SliderMergerServiceOptions? Options { get; private set; }

        public bool QuickRun { get; private set; }

        public Task<SliderMergerResult> MergeAsync(
            IReadOnlyList<string> paths,
            SliderMergerServiceOptions options,
            bool quickRun = false,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Paths = paths.ToArray();
            Options = options;
            QuickRun = quickRun;
            progress?.Report(1);
            return Task.FromResult(new SliderMergerResult(paths, 2));
        }
    }
}
