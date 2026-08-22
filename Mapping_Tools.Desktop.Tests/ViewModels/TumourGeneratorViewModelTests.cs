using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.TumourGenerator;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Tools.TumourGenerating;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class TumourGeneratorViewModelTests
{
    [TestMethod]
    public async Task RunQuickAsync_WithCurrentBeatmapUsesSelectedSlidersAndReloadsEditor()
    {
        // Arrange
        RecordingGenerator service = new();
        RecordingReload reload = new();
        TumourGeneratorViewModel viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator("current.osu"),
            reload,
            autoReload: true);

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        service.RunPaths.Should().Equal("current.osu");
        service.Project.Should().NotBeNull();
        service.Project!.ImportModeSetting.Should().Be(TumourImportMode.Selected);
        service.ReloadEditor.Should().BeTrue();
        reload.ReloadCount.Should().Be(1);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task ImportCommand_WhenNoSlidersReturnsEmptyStateMessageWithoutReplacingPreview()
    {
        // Arrange
        RecordingGenerator service = new() { ReturnEmptyImport = true };
        TestDialogService dialogs = new();
        TumourGeneratorViewModel viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator("current.osu"),
            dialogs: dialogs);
        HitObject original = viewModel.PreviewHitObject;

        // Act
        await ((IAsyncRelayCommand)viewModel.ImportCommand).ExecuteAsync(null);

        // Assert
        dialogs.MessageCount.Should().BeGreaterThan(0);
        ((MessageDialogRequest<bool>)dialogs.LastMessageRequest!).Message
            .Should().Be("Could not find any sliders in imported hit objects.");
        viewModel.PreviewHitObject.Should().BeSameAs(original);
    }

    [TestMethod]
    public async Task ImportCommand_WhenServiceFails_ShowsErrorDialog()
    {
        // Arrange
        RecordingGenerator service = new() { ImportException = new IOException("import failed") };
        TestDialogService dialogs = new();
        TumourGeneratorViewModel viewModel = Create(
            service,
            new RecordingCurrentBeatmapLocator("current.osu"),
            dialogs: dialogs);

        // Act
        await ((IAsyncRelayCommand)viewModel.ImportCommand).ExecuteAsync(null);

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
        TumourGeneratorViewModel viewModel = Create(
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
    public async Task RunCommand_WithInvalidProjectStopsBeforeServiceAndReportsValidation()
    {
        // Arrange
        RecordingGenerator service = new();
        TumourGeneratorViewModel viewModel = Create(service);
        viewModel.TumourLayers.Clear();
        viewModel.TumourLayers.Should().BeEmpty();
        viewModel.RunCommand.CanExecute(null).Should().BeTrue();
        viewModel.ValidateSettings().Should().BeFalse();

        // Act
        await ((IAsyncRelayCommand)viewModel.RunCommand).ExecuteAsync(null);

        // Assert
        service.RunCalled.Should().BeFalse();
        viewModel.ResultSummary.Should().Contain("invalid");
    }

    [TestMethod]
    public void ValidateSettings_WithNonFiniteGraphAnchorRejectsTheProject()
    {
        // Arrange
        TumourGeneratorViewModel viewModel = Create(new RecordingGenerator());
        viewModel.CurrentLayer!.TumourScale.Anchors[0].Pos = new Vector2(double.NaN, 0);

        // Act
        bool valid = viewModel.ValidateSettings();

        // Assert
        valid.Should().BeFalse();
        viewModel.ResultSummary.Should().Contain("invalid");
    }

    [TestMethod]
    public void ShellProjectFeature_SnapshotAndInstall_PreserveLayersGraphsAndPreview()
    {
        // Arrange
        TumourGeneratorViewModel viewModel = Create(new RecordingGenerator());
        viewModel.CurrentLayer!.TumourTemplateEnum = TumourTemplate.Square;
        viewModel.CurrentLayer.TumourParameter = TumourLayer.GetGraphState(12);
        viewModel.CurrentLayer.Name = "Custom";
        viewModel.PreviewHitObject = new HitObject("32,64,100,2,0,L|200:64,1,168");
        IShellProjectFeature feature = viewModel;

        // Act
        TumourGeneratorProject snapshot = (TumourGeneratorProject)feature.Snapshot();
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
        TumourGeneratorViewModel viewModel = Create(new RecordingGenerator());

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
        TumourGeneratorViewModel viewModel = Create(service);
        viewModel.PreviewHitObject = new HitObject("32,64,100,2,0,L|200:64,1,168");

        // Act
        for (var attempt = 0; attempt < 50 && viewModel.TumouredPreviewHitObject is null; attempt++)
        {
            await Task.Delay(10);
        }

        // Assert
        service.PreviewCalled.Should().BeGreaterThan(0);
        viewModel.TumouredPreviewHitObject.Should().NotBeNull();
        viewModel.LayerRangeSliderMaxes.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Dispose_DetachesLayerSubscriptionsAndStopsFuturePreviewRequests()
    {
        // Arrange
        RecordingGenerator service = new();
        TumourGeneratorViewModel viewModel = Create(service);
        for (var attempt = 0; attempt < 50 && service.PreviewCalled == 0; attempt++)
        {
            await Task.Delay(10);
        }

        int previewCallsBeforeDispose = service.PreviewCalled;

        // Act
        viewModel.Dispose();
        viewModel.CurrentLayer!.Name = "After dispose";
        await Task.Delay(20);

        // Assert
        service.PreviewCalled.Should().Be(previewCallsBeforeDispose);
    }

    private static TumourGeneratorViewModel Create(
        RecordingGenerator service,
        RecordingCurrentBeatmapLocator? locator = null,
        RecordingReload? reload = null,
        bool autoReload = false,
        TestDialogService? dialogs = null)
    {
        ApplicationSettings settings = new() { AutoReload = autoReload };
        TumourGeneratorViewModel viewModel = new(
            service,
            new ToolExecutionService(
                new UserNotificationService(),
                reload ?? new RecordingReload(),
                settings,
                TimeProvider.System),
            locator ?? new RecordingCurrentBeatmapLocator(null),
            new TestBeatmapWorkspace(),
            settings,
            dialogs ?? new TestDialogService());
        viewModel.Activate();
        return viewModel;
    }

    private sealed class RecordingGenerator : ITumourGeneratorService
    {
        public bool ReturnEmptyImport { get; init; }

        public Exception? ImportException { get; init; }

        public int PreviewCalled { get; private set; }

        public bool RunCalled { get; private set; }

        public bool ReloadEditor { get; private set; }

        public IReadOnlyList<string>? RunPaths { get; private set; }

        public TumourGeneratorProject? Project { get; private set; }

        public Task<TumourImportResult> ImportAsync(
            string path,
            TumourImportMode mode,
            string? timeCode,
            CancellationToken cancellationToken = default) =>
            ImportException is not null
                ? Task.FromException<TumourImportResult>(ImportException)
                : Task.FromResult(new TumourImportResult(
                ReturnEmptyImport ? [] : [new HitObject("64,64,0,2,0,L|164:64,1,100")],
                4,
                true));

        public Task<TumourPreviewResult> PreviewAsync(
            HitObject previewHitObject,
            TumourGeneratorOptions options,
            CancellationToken cancellationToken = default)
        {
            PreviewCalled++;
            return Task.FromResult(new TumourPreviewResult(
                previewHitObject.DeepCopy(),
                [previewHitObject.PixelLength]));
        }

        public Task<TumourRunResult> RunAsync(
            IReadOnlyList<string> paths,
            TumourGeneratorProject project,
            bool reloadEditor,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            RunCalled = true;
            RunPaths = paths;
            Project = project;
            ReloadEditor = reloadEditor;
            progress?.Report(100);
            return Task.FromResult(new TumourRunResult(paths, 1, reloadEditor));
        }
    }

    private sealed class RecordingCurrentBeatmapLocator(string? path) : ICurrentBeatmapLocator
    {
        public Task<string?> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(path);
    }

    private sealed class RecordingReload : IEditorReloadService
    {
        public int ReloadCount { get; private set; }

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            ReloadCount++;
            return Task.CompletedTask;
        }
    }
}
