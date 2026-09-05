using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.Sliderator.Models;
using Mapping_Tools.Desktop.Tools.Sliderator.ViewModels;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.Sliderator.ViewModels;

[TestClass]
public sealed class SlideratorViewModelTests
{
    [TestMethod]
    public async Task RunQuickAsync_WithImportedSlider_PassesPersistedGraphSettingsToService()
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
        viewModel.ManualVelocity.Should().BeTrue();
        service.ReloadEditor.Should().BeTrue();
        viewModel.DoEditorRead.Should().BeFalse();
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void DefaultGraphState_UsesUnitPositionViewport()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());

        // Act
        var state = viewModel.GraphState;

        // Assert
        state.MinX.Should().Be(0);
        state.MinY.Should().Be(0);
        state.MaxX.Should().Be(viewModel.GraphBeats);
        state.MaxY.Should().Be(1);
        state.Anchors[0].Pos.Should().Be(new Vector2(0, 0));
        state.Anchors[^1].Pos.Should().Be(new Vector2((float)viewModel.GraphBeats, 1));
    }

    [TestMethod]
    public void GraphState_WhenAnchorExceedsVelocityLimit_ClipsTheAnchor()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());
        viewModel.VelocityLimit = 0.5;
        viewModel.GraphState = new GraphState(
            [
                new GraphAnchor(new Vector2(0, 0)),
                new GraphAnchor(new Vector2(1, 0.2f)),
                new GraphAnchor(new Vector2(2, 0.9f)),
            ],
            0,
            0,
            2,
            1);
        GraphState candidate = viewModel.GraphState.Clone();
        candidate.Anchors[1].Pos = new Vector2(1, 1);

        // Act
        viewModel.GraphState = candidate;

        // Assert
        viewModel.GraphState.Anchors[1].Pos.Y.Should().BeLessThan(1);
        viewModel.IsGraphWithinVelocityLimit(viewModel.GraphState).Should().BeTrue();
    }

    [TestMethod]
    public void GraphState_WhenExistingGraphExceedsVelocityLimit_AllowsEditThatDoesNotIncreaseMaximumSlope()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());
        viewModel.VelocityLimit = 0.5;
        viewModel.GraphState = new GraphState(
            [
                new GraphAnchor(new Vector2(0, 0)),
                new GraphAnchor(new Vector2(1, 0.2f)),
                new GraphAnchor(new Vector2(2, 1)),
                new GraphAnchor(new Vector2(3, 2)),
            ],
            0,
            0,
            3,
            2);
        GraphState candidate = viewModel.GraphState.Clone();
        candidate.Anchors[2].Pos = new Vector2(2, 1.1f);

        // Act
        viewModel.GraphState = candidate;

        // Assert
        viewModel.GraphState.Anchors[2].Pos.Y.Should().BeApproximately(1.1f, 0.0001f);
    }

    [TestMethod]
    public void GraphBeats_WhenChanged_UpdatesGraphStateWidthAndScalesAnchors()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());
        viewModel.GraphState = new GraphState(
            [
                new GraphAnchor(new Vector2(2, 0)),
                new GraphAnchor(new Vector2(4, 0.5f)),
                new GraphAnchor(new Vector2(7, 1)),
            ],
            2,
            0,
            7,
            1);

        // Act
        viewModel.GraphBeats = 10;

        // Assert
        viewModel.GraphState.MaxX.Should().Be(12);
        viewModel.GraphState.MaxX.Should().Be(viewModel.GraphState.MinX + viewModel.GraphBeats);
        viewModel.GraphState.Anchors.Select(anchor => anchor.Pos).Should().Equal(
            new Vector2(2, 0),
            new Vector2(6, 0.5f),
            new Vector2(12, 1));
    }

    [TestMethod]
    public void GraphState_WhenAssignedWithDifferentWidth_UpdatesGraphBeats()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());
        GraphState state = new(
            [
                new GraphAnchor(new Vector2(1, 0)),
                new GraphAnchor(new Vector2(6, 1)),
            ],
            1,
            0,
            6,
            1);

        // Act
        viewModel.GraphState = state;

        // Assert
        viewModel.GraphBeats.Should().Be(5);
        viewModel.GraphState.MaxX.Should().Be(viewModel.GraphState.MinX + viewModel.GraphBeats);
    }

    [TestMethod]
    public void InstallProject_WithDefaultGraph_UsesProjectGraph()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());
        viewModel.GraphModeSetting = SlideratorGraphMode.Position;
        SlideratorProject project = new();

        // Act
        ((IShellProjectFeature<SlideratorProject>)viewModel).Install(project);

        // Assert
        viewModel.GraphState.MinX.Should().Be(0);
        viewModel.GraphState.MaxX.Should().Be(project.GraphBeats);
        viewModel.GraphState.Anchors.Select(anchor => anchor.Pos).Should().Equal(
            new Vector2(0, 0),
            new Vector2((float)project.GraphBeats, 1));
    }

    [TestMethod]
    public void InstallProject_WithPersistedLoadedSliders_RestoresListSelectionAndEditorReadState()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());
        HitObject firstSlider = new("64,64,0,2,0,L|164:64,1,100");
        HitObject secondSlider = new("164,64,1000,2,0,L|264:64,1,100");
        SlideratorProject project = new()
        {
            LoadedHitObjects = [firstSlider, secondSlider],
            VisibleHitObjectIndex = 1,
            DoEditorRead = true,
        };

        // Act
        ((IShellProjectFeature<SlideratorProject>)viewModel).Install(project);

        // Assert
        viewModel.LoadedHitObjects.Should().Equal(firstSlider, secondSlider);
        viewModel.VisibleHitObjectIndex.Should().Be(1);
        viewModel.VisibleHitObject.Should().BeSameAs(secondSlider);
        viewModel.DoEditorRead.Should().BeTrue();
    }

    [TestMethod]
    public void Snapshot_WithLoadedSliders_PreservesImportedListAndSelection()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());
        HitObject firstSlider = new("64,64,0,2,0,L|164:64,1,100");
        HitObject secondSlider = new("164,64,1000,2,0,L|264:64,1,100");
        viewModel.LoadedHitObjects.Add(firstSlider);
        viewModel.LoadedHitObjects.Add(secondSlider);
        viewModel.VisibleHitObjectIndex = 1;

        // Act
        SlideratorProject snapshot = ((IShellProjectFeature<SlideratorProject>)viewModel).Snapshot();

        // Assert
        snapshot.LoadedHitObjects.Should().Equal(firstSlider, secondSlider);
        snapshot.VisibleHitObjectIndex.Should().Be(1);
    }

    [TestMethod]
    public async Task ClearGraphCommand_ResetsTheGraphToCurrentModeDefaults()
    {
        // Arrange
        TestDialogService dialogs = new() { BooleanResult = true };
        var viewModel = Create(new RecordingSliderator(), dialogs: dialogs);
        viewModel.GraphState = new GraphState(
            [new GraphAnchor(new Vector2(0, 0)), new GraphAnchor(new Vector2(0.25f, 0.9f)), new GraphAnchor(new Vector2(1, 1))],
            0,
            0,
            1,
            1);

        // Act
        await viewModel.ClearGraphCommand.ExecuteAsync(null);

        // Assert
        viewModel.GraphState.Anchors.Should().HaveCount(2);
        viewModel.GraphState.Anchors[0].Pos.Should().Be(new Vector2(0, 0));
        viewModel.GraphState.Anchors[1].Pos.Should().Be(new Vector2((float)viewModel.GraphBeats, 1));
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
    public async Task ImportCommand_WithSelectedModeAndUnavailableCurrentBeatmap_ShowsErrorDialogWithoutInvokingService()
    {
        // Arrange
        RecordingSliderator service = new();
        TestDialogService dialogs = new();
        var viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator(null),
            dialogs);

        // Act
        await viewModel.ImportCommand.ExecuteAsync(null);

        // Assert
        service.ImportPath.Should().BeNull();
        ((MessageDialogRequest<bool>)dialogs.LastMessageRequest!).Message
            .Should().Contain("Open a beatmap in osu!");
    }

    [DataTestMethod]
    [DataRow(HitObjectSelectionMode.Bookmarked)]
    [DataRow(HitObjectSelectionMode.Time)]
    [DataRow(HitObjectSelectionMode.Everything)]
    public async Task ImportCommand_WithNonSelectedMode_UsesWorkspacePathWithoutLookingForLiveEditor(
        HitObjectSelectionMode mode)
    {
        // Arrange
        RecordingSliderator service = new();
        RecordingCurrentBeatmapLocator currentBeatmap = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = Create(service, currentBeatmap, workspace: workspace);
        viewModel.ImportModeSetting = mode;

        // Act
        await viewModel.ImportCommand.ExecuteAsync(null);

        // Assert
        service.ImportPath.Should().Be("selected.osu");
        currentBeatmap.FindCount.Should().Be(0);
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
    public void EvaluatePreviewProgress_DuringHold_HidesBallAndRepeatsAfterHold()
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

    [TestMethod]
    public void EvaluatePreviewProgress_AfterElapsedTime_AdvancesThroughSlider()
    {
        // Arrange
        var viewModel = Create(new RecordingSliderator());

        // Act
        double initial = viewModel.EvaluatePreviewProgress(1);
        double later = viewModel.EvaluatePreviewProgress(viewModel.GraphDuration / 2);

        // Assert
        later.Should().BeGreaterThan(initial);
    }

    private static SlideratorViewModel Create(
        RecordingSliderator service,
        RecordingCurrentBeatmapLocator? currentBeatmap = null,
        TestDialogService? dialogs = null,
        TestBeatmapWorkspace? workspace = null)
    {
        return new SlideratorViewModel(
            service,
            new ToolExecutionService(
                new UserNotificationService(),
                new RecordingEditorReloadService(),
                new DesktopApplicationSettings(),
                TimeProvider.System),
            currentBeatmap ?? new RecordingCurrentBeatmapLocator(null),
            workspace ?? new TestBeatmapWorkspace(),
            new DesktopApplicationSettings(),
            dialogs ?? new TestDialogService());
    }

    private sealed class RecordingSliderator : ISlideratorService
    {
        public string? ImportPath { get; private set; }

        public SlideratorServiceOptions? Project { get; private set; }

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
            SlideratorServiceOptions project,
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
