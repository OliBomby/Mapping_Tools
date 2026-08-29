using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Interactions.Dialogs;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Desktop.Settings.Models;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.Tools.TumourGenerator.Templates;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.TumourGenerator.Models;
using Mapping_Tools.Desktop.Tools.TumourGenerator.ViewModels;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.TumourGenerator.ViewModels;

[TestClass]
public sealed class TumourGeneratorViewModelTests
{
    [TestMethod]
    public async Task RunQuickAsync_WithCurrentBeatmap_UsesSelectedSlidersAndReloadsEditor()
    {
        // Arrange
        RecordingGenerator service = new();
        RecordingEditorReloadService reload = new();
        var viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator("current.osu"),
            reload,
            true);

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.RunPaths.Should().Equal("current.osu");
        service.Project.Should().NotBeNull();
        service.Project!.ImportModeSetting.Should().Be(HitObjectSelectionMode.Selected);
        service.ReloadEditor.Should().BeTrue();
        reload.ReloadCount.Should().Be(1);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task ImportCommand_WhenNoSliders_ReturnsEmptyStateMessageWithoutReplacingPreview()
    {
        // Arrange
        RecordingGenerator service = new() { ReturnEmptyImport = true };
        TestDialogService dialogs = new();
        var viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator("current.osu"),
            dialogs: dialogs);
        var original = viewModel.PreviewHitObject;

        // Act
        await viewModel.ImportCommand.ExecuteAsync(null);

        // Assert
        dialogs.MessageCount.Should().BeGreaterThan(0);
        ((MessageDialogRequest<bool>)dialogs.LastMessageRequest!).Message
            .Should().Be("Could not find any sliders in imported hit objects.");
        viewModel.PreviewHitObject.Should().BeSameAs(original);
    }

    [DataTestMethod]
    [DataRow(HitObjectSelectionMode.Bookmarked)]
    [DataRow(HitObjectSelectionMode.Time)]
    [DataRow(HitObjectSelectionMode.Everything)]
    public async Task ImportCommand_WithNonSelectedMode_UsesWorkspacePathWithoutLookingForLiveEditor(
        HitObjectSelectionMode mode)
    {
        // Arrange
        RecordingGenerator service = new();
        RecordingCurrentBeatmapLocator currentBeatmap = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = Create(
            service,
            currentBeatmap,
            workspace: workspace);
        viewModel.ImportModeSetting = mode;

        // Act
        await viewModel.ImportCommand.ExecuteAsync(null);

        // Assert
        service.ImportPath.Should().Be("selected.osu");
        currentBeatmap.FindCount.Should().Be(0);
    }

    [TestMethod]
    public async Task ImportCommand_WhenServiceFails_ShowsErrorDialog()
    {
        // Arrange
        RecordingGenerator service = new() { ImportException = new IOException("import failed") };
        TestDialogService dialogs = new();
        var viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator("current.osu"),
            dialogs: dialogs);

        // Act
        await viewModel.ImportCommand.ExecuteAsync(null);

        // Assert
        dialogs.MessageCount.Should().BeGreaterThan(0);
        ((MessageDialogRequest<bool>)dialogs.LastMessageRequest!).Message
            .Should().Be("import failed");
    }

    [TestMethod]
    public async Task RunQuickAsync_WhenCurrentBeatmapIsMissing_ShowsEmptyTargetMessage()
    {
        // Arrange
        TestDialogService dialogs = new();
        var viewModel = Create(
            new RecordingGenerator(),
            new RecordingCurrentBeatmapLocator(null),
            dialogs: dialogs);

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        ((MessageDialogRequest<bool>)dialogs.LastMessageRequest!).Message
            .Should().Contain("Select at least one beatmap");
    }

    [TestMethod]
    public void ShellProjectFeature_SnapshotAndInstall_PreserveLayersGraphsAndPreview()
    {
        // Arrange
        var viewModel = Create(new RecordingGenerator());
        viewModel.CurrentLayer!.TumourTemplateEnum = TumourTemplate.Square;
        viewModel.CurrentLayer.TumourParameter = TumourLayer.GetGraphState(12);
        viewModel.CurrentLayer.Name = "Custom";
        viewModel.PreviewHitObject = new HitObject("32,64,100,2,0,L|200:64,1,168");
        IShellProjectFeature<TumourGeneratorProject> feature = viewModel;

        // Act
        var snapshot = feature.Snapshot();
        viewModel.CurrentLayer.Name = "Changed";
        feature.Install(snapshot);

        // Assert
        feature.ProjectDefinition.AutoSaveFileName.Should().Be("tumourgeneratorproject.json");
        viewModel.CurrentLayer!.Name.Should().Be("Custom");
        viewModel.CurrentLayer.TumourTemplateEnum.Should().Be(TumourTemplate.Square);
        viewModel.CurrentLayer.TumourParameter.GetValue(0).Should().Be(12);
        viewModel.PreviewHitObject.Line.Should().Contain("32,64,100");
    }

    [TestMethod]
    public void LayerCommands_AddCopyRemoveAndReorder_PreserveSelectionRules()
    {
        // Arrange
        var viewModel = Create(new RecordingGenerator());

        // Act
        viewModel.AddCommand.Execute(null);
        viewModel.CopyCommand.Execute(null);
        viewModel.LowerCommand.Execute(null);
        viewModel.RemoveCommand.Execute(null);

        // Assert
        viewModel.TumourLayers.Should().HaveCount(2);
        viewModel.CurrentLayerIndex.Should().Be(0);
        viewModel.TumourLayers.Select(layer => layer.Name).Should().Contain("Layer 2");
    }

    [TestMethod]
    public async Task PreviewRequest_UpdatesPreviewObjectAndLayerRanges()
    {
        // Arrange
        RecordingGenerator service = new();
        var viewModel = Create(service);
        viewModel.PreviewHitObject = new HitObject("32,64,100,2,0,L|200:64,1,168");

        // Act
        for (int attempt = 0; attempt < 50 && viewModel.TumouredPreviewHitObject is null; attempt++) await Task.Delay(10);

        // Assert
        viewModel.TumouredPreviewHitObject.Should().NotBeNull();
        viewModel.LayerRangeSliderMaxes.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Dispose_DetachesLayerSubscriptionsAndStopsFuturePreviewRequests()
    {
        // Arrange
        RecordingGenerator service = new();
        var viewModel = Create(service);
        for (int attempt = 0; attempt < 50 && viewModel.TumouredPreviewHitObject is null; attempt++) await Task.Delay(10);
        var previewBeforeDispose = viewModel.TumouredPreviewHitObject;

        // Act
        viewModel.Dispose();
        viewModel.CurrentLayer!.Name = "After dispose";
        await Task.Delay(20);

        // Assert
        viewModel.TumouredPreviewHitObject.Should().BeSameAs(previewBeforeDispose);
    }

    private static TumourGeneratorViewModel Create(
        RecordingGenerator service,
        RecordingCurrentBeatmapLocator? locator = null,
        RecordingEditorReloadService? reload = null,
        bool autoReload = false,
        TestDialogService? dialogs = null,
        TestBeatmapWorkspace? workspace = null)
    {
        DesktopApplicationSettings settings = new() { AutoReload = autoReload };
        TumourGeneratorViewModel viewModel = new(
            service,
            new ToolExecutionService(
                new UserNotificationService(),
                reload ?? new RecordingEditorReloadService(),
                settings,
                TimeProvider.System),
            locator ?? new RecordingCurrentBeatmapLocator(null),
            workspace ?? new TestBeatmapWorkspace(),
            settings,
            dialogs ?? new TestDialogService());
        viewModel.Activate();
        return viewModel;
    }

    private sealed class RecordingGenerator : ITumourGeneratorService
    {
        public string? ImportPath { get; private set; }

        public bool ReturnEmptyImport { get; init; }

        public Exception? ImportException { get; init; }

        public bool RunCalled { get; private set; }

        public bool ReloadEditor { get; private set; }

        public IReadOnlyList<string>? RunPaths { get; private set; }

        public TumourGeneratorServiceOptions? Project { get; private set; }

        public Task<TumourImportResult> ImportAsync(
            string path,
            HitObjectSelectionMode mode,
            string? timeCode,
            CancellationToken cancellationToken = default)
        {
            ImportPath = path;
            return ImportException is not null
                ? Task.FromException<TumourImportResult>(ImportException)
                : Task.FromResult(new TumourImportResult(
                    ReturnEmptyImport ? [] : [new HitObject("64,64,0,2,0,L|164:64,1,100")],
                    4,
                    true));
        }

        public Task<TumourRunResult> RunAsync(
            IReadOnlyList<string> paths,
            TumourGeneratorServiceOptions project,
            bool reloadEditor,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            RunCalled = true;
            RunPaths = paths;
            Project = project;
            ReloadEditor = reloadEditor;
            progress?.Report(1);
            return Task.FromResult(new TumourRunResult(paths, 1, reloadEditor));
        }
    }

}
