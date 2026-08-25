using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.Sliderator;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class SlideratorViewModelTests
{
    [TestMethod]
    public async Task RunQuickAsync_WithImportedSliderPassesPersistedGraphSettingsToService()
    {
        // Arrange
        RecordingSliderator service = new();
        var viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator("current.osu"));
        viewModel.BeatSnapDivisor = 8;
        viewModel.ManualVelocity = true;
        viewModel.NewVelocity = 1;

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.ImportPath.Should().Be("current.osu");
        service.Project.Should().NotBeNull();
        service.Project!.BeatSnapDivisor.Should().Be(8);
        service.Project.ManualVelocity.Should().BeTrue();
        service.ReloadEditor.Should().BeTrue();
        viewModel.DoEditorRead.Should().BeFalse();
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunFastPlacementAsync_WithVisibleSlider_DoesNotRequestEditorReload()
    {
        // Arrange
        RecordingSliderator service = new();
        var viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator("current.osu"));
        viewModel.LoadedHitObjects.Add(new HitObject("64,64,0,2,0,L|164:64,1,100"));

        // Act
        bool succeeded = await viewModel.RunFastPlacementAsync();

        // Assert
        succeeded.Should().BeTrue();
        service.ReloadEditor.Should().BeFalse();
    }

    [TestMethod]
    public async Task MoveRightAsync_WhenFastPlacementFails_DoesNotAdvance()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());
        viewModel.LoadedHitObjects.Add(new HitObject("64,64,0,2,0,L|164:64,1,100"));
        viewModel.LoadedHitObjects.Add(new HitObject("164,64,1000,2,0,L|264:64,1,100"));
        viewModel.Interaction = new FailedSlideratorInteraction();

        // Act
        await viewModel.MoveRightAsync(true);

        // Assert
        viewModel.VisibleHitObjectIndex.Should().Be(0);
    }

    [TestMethod]
    public async Task ImportCommand_WhenNoSlidersAreReturned_PreservesCurrentPreview()
    {
        // Arrange
        RecordingSliderator service = new() { ReturnEmptyImport = true };
        var viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator("current.osu"));
        HitObject slider = new("64,64,0,2,0,L|164:64,1,100");
        viewModel.LoadedHitObjects.Add(slider);

        // Act
        await viewModel.ImportCommand.ExecuteAsync(null);

        // Assert
        viewModel.LoadedHitObjects.Should().ContainSingle().Which.Should().BeSameAs(slider);
    }

    [TestMethod]
    public async Task RunQuickAsync_WhenImportReturnsNoSliders_DoesNotRunPreviousPreview()
    {
        // Arrange
        RecordingSliderator service = new() { ReturnEmptyImport = true };
        var viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator("current.osu"));
        viewModel.LoadedHitObjects.Add(new HitObject("64,64,0,2,0,L|164:64,1,100"));

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.RunCalled.Should().BeFalse();
    }

    [TestMethod]
    public void EvaluatePreviewProgress_DuringHoldHidesBallAndRepeatsAfterHold()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());
        double duration = viewModel.GraphDuration;

        // Act
        double held = viewModel.EvaluatePreviewProgress(duration + 500);
        double repeated = viewModel.EvaluatePreviewProgress(duration + 1000 + 1);

        // Assert
        held.Should().Be(-1);
        repeated.Should().BeGreaterThanOrEqualTo(0);
    }

    private static SlideratorViewModel Create(
        RecordingSliderator service,
        RecordingCurrentBeatmapLocator? currentBeatmap = null)
    {
        return new SlideratorViewModel(
            service,
            new ToolExecutionService(
                new UserNotificationService(),
                new RecordingEditorReloadService(),
                new ApplicationSettings(),
                TimeProvider.System),
            currentBeatmap ?? new RecordingCurrentBeatmapLocator(null),
            new ApplicationSettings(),
            new TestDialogService());
    }

    private sealed class RecordingSliderator : ISlideratorService
    {
        public string? ImportPath { get; private set; }

        public SlideratorProject? Project { get; private set; }

        public bool ReloadEditor { get; private set; }

        public bool ReturnEmptyImport { get; init; }

        public bool RunCalled { get; private set; }

        public Task<SlideratorImportResult> ImportAsync(
            string path,
            HitObjectSelectionMode mode,
            string? timeCode,
            CancellationToken cancellationToken = default)
        {
            ImportPath = path;
            IReadOnlyList<HitObject> sliders = ReturnEmptyImport
                ? []
                : [new HitObject("64,64,0,2,0,L|164:64,1,100")];
            return Task.FromResult(new SlideratorImportResult(sliders, 1.4, true, true));
        }

        public Task<SlideratorResult> RunAsync(
            string path,
            SlideratorProject project,
            HitObject sourceSlider,
            bool reloadEditor,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default,
            bool preferLiveEditor = true)
        {
            RunCalled = true;
            Project = project;
            ReloadEditor = reloadEditor;
            progress?.Report(1);
            return Task.FromResult(
                new SlideratorResult(
                    path,
                    new SlideratorApplyResult(100, 1, false, 1),
                    reloadEditor));
        }
    }

    private sealed class FailedSlideratorInteraction : ISlideratorInteraction
    {
        public Task<bool> RunFastAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}
