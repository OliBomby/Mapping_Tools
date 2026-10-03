using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Application.Localization;
using Avalonia.Controls;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.ViewModels;

[TestClass]
public sealed class HitsoundStudioViewModelTests
{
    [TestMethod]
    public void Constructor_DefaultSample_UsesAutomaticSampleSetAndInvariantVolume()
    {
        // Arrange
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService());

        // Act
        var defaultSample = viewModel.DefaultSample;

        // Assert
        defaultSample.SampleSet.Should().Be(SampleSet.None);
        defaultSample.SampleArgs.Volume.Should().Be(-0.01d);
    }

    [TestMethod]
    public async Task PreviewCommand_WhenSupersededDuringGeneration_StopsStaleSessionAndKeepsLatestPlaying()
    {
        // Arrange
        HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService service = new();
        HitsoundStudioViewModelTestFactory.RecordingAudioGenerator audioGenerator = new();
        HitsoundStudioViewModelTestFactory.RecordingPlaybackService playback = new();
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(service, audioGenerator, playback);
        ObservableHitsoundLayer layer = new(
            new HitsoundLayer(
                "layer",
                SampleSet.Normal,
                Hitsound.Normal,
                new SampleGeneratingArgs("sample.wav"),
                new LayerImportArgs()));
        viewModel.SetSelection([layer]);

        // Act
        var firstPreview = viewModel.PreviewCommand.ExecuteAsync(null);
        await audioGenerator.FirstGenerationStarted.Task;
        var secondPreview = viewModel.PreviewCommand.ExecuteAsync(null);
        await audioGenerator.FirstGenerationCanceled.Task;
        audioGenerator.ReleaseFirstGeneration();
        await firstPreview;
        await secondPreview;
        int[] stopCountsAfterPreviews = playback.Sessions.Select(session => session.StopCount).ToArray();

        await viewModel.DisposeAsync();

        // Assert
        playback.Sessions.Should().HaveCount(2);
        stopCountsAfterPreviews.Should().Equal(1, 0);
        playback.Sessions[1].StopCount.Should().Be(1);
    }

    [TestMethod]
    public void SetSelection_WithMixedLayerValues_ShowsNeutralEditorValues()
    {
        // Arrange
        var (viewModel, _, _) = HitsoundStudioViewModelTestFactory.CreateMixedSelection();

        // Act
        viewModel.SetSelection(viewModel.Layers);

        // Assert
        viewModel.EditName.Should().BeEmpty();
        viewModel.EditSampleSet.Should().BeNull();
        viewModel.EditHitsound.Should().BeNull();
        viewModel.EditTimes.Should().BeEmpty();
        viewModel.EditSamplePath.Should().BeEmpty();
        viewModel.EditSampleVolume.Should().Be(1);
        viewModel.EditImportType.Should().BeNull();
        viewModel.EditImportDiscriminateVolumes.Should().BeFalse();
    }

    [TestMethod]
    public void SetSelection_WithEquivalentTimeLists_ShowsCommonTimes()
    {
        // Arrange
        var (viewModel, first, second) = HitsoundStudioViewModelTestFactory.CreateMixedSelection();
        second.Times = first.Times.ToList();

        // Act
        viewModel.SetSelection(viewModel.Layers);

        // Assert
        viewModel.EditTimes.Should().Equal(100);
    }

    [TestMethod]
    public void SetSelection_WhenSelectionGrowsWithoutChangingFirstSelectedLayer_RefreshesEditorValues()
    {
        // Arrange
        var (viewModel, first, second) = HitsoundStudioViewModelTestFactory.CreateMixedSelection();
        viewModel.SetSelection([first]);

        // Act
        viewModel.SetSelection([first, second]);

        // Assert
        viewModel.EditName.Should().BeEmpty();
        viewModel.EditTimes.Should().BeEmpty();
    }

    [TestMethod]
    public void SelectedLayers_WhenCollectionChangesDirectly_RefreshesEditorState()
    {
        // Arrange
        var (viewModel, first, _) = HitsoundStudioViewModelTestFactory.CreateMixedSelection();

        // Act
        viewModel.SelectedLayers.Add(first);

        // Assert
        viewModel.HasSelectedLayer.Should().BeTrue();
        viewModel.EditName.Should().Be("first");
        viewModel.EditTimes.Should().Equal(100);
    }

    [TestMethod]
    public void MoveSelectedLayers_WhenSelectedLayerMovesDown_PreservesSelection()
    {
        // Arrange
        var (viewModel, first, second) = HitsoundStudioViewModelTestFactory.CreateMixedSelection();
        viewModel.SetSelection([first]);

        // Act
        viewModel.MoveSelectedLayers(1);

        // Assert
        viewModel.Layers.Should().Equal(second, first);
        viewModel.SelectedLayers.Should().Equal(first);
    }

    [TestMethod]
    public void EditorColumnWidth_WhenLayersAreAbsentOrPresent_UsesCollapsedOrStarSizing()
    {
        // Arrange
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService());
        ObservableHitsoundLayer layer = new(new HitsoundLayer(
            "layer",
            SampleSet.Normal,
            Hitsound.Normal,
            new SampleGeneratingArgs("sample.wav"),
            new LayerImportArgs()));

        // Act
        var emptyWidth = viewModel.EditorColumnWidth;
        viewModel.Layers.Add(layer);
        var populatedWidth = viewModel.EditorColumnWidth;

        // Assert
        emptyWidth.Should().Be(new GridLength(0));
        populatedWidth.Should().Be(GridLength.Star);
    }

    [TestMethod]
    public void EditSharedValues_WithMultipleSelectedLayers_UpdatesEveryLayer()
    {
        // Arrange
        var (viewModel, first, second) = HitsoundStudioViewModelTestFactory.CreateMixedSelection();
        viewModel.SetSelection(viewModel.Layers);

        // Act
        viewModel.EditName = "shared";
        viewModel.EditSampleSet = SampleSet.Soft;
        viewModel.EditHitsound = Hitsound.Clap;
        viewModel.EditTimes = [300, 100];
        viewModel.EditSampleVolume = 0.25;
        viewModel.EditImportType = ImportType.Storyboard;
        viewModel.EditImportDiscriminateVolumes = true;

        // Assert
        first.Name.Should().Be("shared");
        second.Name.Should().Be("shared");
        first.SampleSet.Should().Be(SampleSet.Soft);
        second.SampleSet.Should().Be(SampleSet.Soft);
        first.Hitsound.Should().Be(Hitsound.Clap);
        second.Hitsound.Should().Be(Hitsound.Clap);
        first.Times.Should().Equal(100, 300);
        second.Times.Should().Equal(100, 300);
        first.SampleArgs.Volume.Should().BeApproximately(0.25, 0.0001);
        second.SampleArgs.Volume.Should().BeApproximately(0.25, 0.0001);
        first.ImportArgs.ImportType.Should().Be(ImportType.Storyboard);
        second.ImportArgs.ImportType.Should().Be(ImportType.Storyboard);
        first.ImportArgs.DiscriminateVolumes.Should().BeTrue();
        second.ImportArgs.DiscriminateVolumes.Should().BeTrue();
    }

    [TestMethod]
    public async Task ReloadCommand_WhenSourceChangesTimes_RefreshesLayerAndEditorState()
    {
        // Arrange
        HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService service = new();
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            service,
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService());
        ObservableHitsoundLayer layer = new(new HitsoundLayer(
            "layer",
            SampleSet.Normal,
            Hitsound.Normal,
            new SampleGeneratingArgs("sample.wav"),
            new LayerImportArgs()));
        layer.Times = [100];
        List<string?> changedProperties = [];
        layer.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);
        viewModel.Layers.Add(layer);
        viewModel.SetSelection([layer]);
        service.ReloadAction = layers => layers[0].Times = [200, 400];

        // Act
        await viewModel.ReloadCommand.ExecuteAsync(null);

        // Assert
        layer.Times.Should().Equal(200, 400);
        viewModel.EditTimes.Should().Equal(200, 400);
        changedProperties.Should().Contain(nameof(ObservableHitsoundLayer.Times));
    }

    [TestMethod]
    public async Task LoadBaseBeatmapCommand_WhenCurrentBeatmapIsAvailable_SetsBaseBeatmap()
    {
        // Arrange
        TestCurrentBeatmapDialogService currentBeatmap = new() { Path = "current.osu" };
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService(),
            currentBeatmap);

        // Act
        await viewModel.LoadBaseBeatmapCommand.ExecuteAsync(null);

        // Assert
        viewModel.BaseBeatmap.Should().Be("current.osu");
        currentBeatmap.FetchCount.Should().Be(1);
    }

    [TestMethod]
    public async Task LoadBaseBeatmapCommand_WhenCurrentBeatmapIsUnavailable_PreservesBaseBeatmap()
    {
        // Arrange
        TestCurrentBeatmapDialogService currentBeatmap = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService(),
            currentBeatmap,
            notifications);
        viewModel.BaseBeatmap = "existing.osu";

        // Act
        await viewModel.LoadBaseBeatmapCommand.ExecuteAsync(null);

        // Assert
        viewModel.BaseBeatmap.Should().Be("existing.osu");
        published.Should().BeEmpty();
        currentBeatmap.FetchCount.Should().Be(1);
    }

    [TestMethod]
    public async Task PickBaseBeatmapCommand_UsesSharedBeatmapPickerLocation()
    {
        // Arrange
        TestFilePicker filePicker = new() { OpenFiles = ["base.osu"] };
        TestBeatmapWorkspace workspace = new()
        {
            BeatmapPickerStartLocation = @"C:\Maps",
        };
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService(),
            workspace: workspace,
            filePicker: filePicker);

        // Act
        await viewModel.PickBaseBeatmapCommand.ExecuteAsync(null);

        // Assert
        viewModel.BaseBeatmap.Should().Be("base.osu");
        filePicker.LastOpenRequest!.SuggestedStartLocation.Should().Be(@"C:\Maps");
    }

    [TestMethod]
    public async Task ValidateSamplesCommand_WithInvalidLayers_ShowsAffectedLayerNames()
    {
        // Arrange
        HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService service = new();
        TestDialogService dialogs = new();
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            service,
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService(),
            dialogs: dialogs);
        ObservableHitsoundLayer invalidLayer = new(new HitsoundLayer(
            "missing layer",
            SampleSet.Normal,
            Hitsound.Normal,
            new SampleGeneratingArgs("missing.wav"),
            new LayerImportArgs()));
        ObservableHitsoundLayer validLayer = new(new HitsoundLayer(
            "valid layer",
            SampleSet.Normal,
            Hitsound.Clap,
            new SampleGeneratingArgs("valid.wav"),
            new LayerImportArgs()));
        viewModel.Layers.Add(invalidLayer);
        viewModel.Layers.Add(validLayer);
        service.ValidationFailures = new Dictionary<SampleGeneratingArgs, Exception>(
            new SampleGeneratingArgsComparer())
        {
            [invalidLayer.SampleArgs.Snapshot()] = new FileNotFoundException("missing.wav"),
        };

        // Act
        await viewModel.ValidateSamplesCommand.ExecuteAsync(null);

        // Assert
        var request = dialogs.LastMessageRequest
            .Should()
            .BeOfType<MessageDialogRequest<bool>>()
            .Subject;
        request.Message.Should().Contain("Could not find the following samples:")
            .And.Contain("missing layer")
            .And.NotContain("valid layer");
    }

    [TestMethod]
    public async Task ValidateSamplesCommand_WhenAllLayersAreValid_ShowsSuccessMessage()
    {
        // Arrange
        TestDialogService dialogs = new();
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService(),
            dialogs: dialogs);

        // Act
        await viewModel.ValidateSamplesCommand.ExecuteAsync(null);

        // Assert
        var request = dialogs.LastMessageRequest
            .Should()
            .BeOfType<MessageDialogRequest<bool>>()
            .Subject;
        request.Message.Should().Be("All samples are valid!");
        request.Details.Should().BeNull();
    }

    [TestMethod]
    public async Task ValidateSamplesCommand_WithSingleDecoderFailure_ShowsExceptionDetails()
    {
        // Arrange
        HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService service = new();
        TestDialogService dialogs = new();
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            service,
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService(),
            dialogs: dialogs);
        ObservableHitsoundLayer layer = new(new HitsoundLayer(
            "broken layer",
            SampleSet.Normal,
            Hitsound.Normal,
            new SampleGeneratingArgs("broken.wav"),
            new LayerImportArgs()));
        viewModel.Layers.Add(layer);
        service.ValidationFailures = new Dictionary<SampleGeneratingArgs, Exception>(
            new SampleGeneratingArgsComparer())
        {
            [layer.SampleArgs.Snapshot()] = new InvalidOperationException("decoder failed"),
        };

        // Act
        await viewModel.ValidateSamplesCommand.ExecuteAsync(null);

        // Assert
        var request = dialogs.LastMessageRequest
            .Should()
            .BeOfType<MessageDialogRequest<bool>>()
            .Subject;
        request.Message.Should().Contain(ApplicationText.Format(DesktopStrings.HitsoundStudio_LayerFailure, "broken layer",
            ApplicationStrings.Exception_UnexpectedFailure));
        request.Details.Should().Contain("decoder failed");
    }

    [TestMethod]
    public async Task ValidateSamplesCommand_WithMultipleDecoderFailures_PreservesEveryExceptionInDetails()
    {
        // Arrange
        HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService service = new();
        TestDialogService dialogs = new();
        var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            service,
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService(),
            dialogs: dialogs);
        ObservableHitsoundLayer firstLayer = new(new HitsoundLayer(
            "first broken layer",
            SampleSet.Normal,
            Hitsound.Normal,
            new SampleGeneratingArgs("first-broken.wav"),
            new LayerImportArgs()));
        ObservableHitsoundLayer secondLayer = new(new HitsoundLayer(
            "second broken layer",
            SampleSet.Normal,
            Hitsound.Normal,
            new SampleGeneratingArgs("second-broken.wav"),
            new LayerImportArgs()));
        viewModel.Layers.Add(firstLayer);
        viewModel.Layers.Add(secondLayer);
        service.ValidationFailures = new Dictionary<SampleGeneratingArgs, Exception>(
            new SampleGeneratingArgsComparer())
        {
            [firstLayer.SampleArgs.Snapshot()] = new InvalidOperationException("first decoder failure"),
            [secondLayer.SampleArgs.Snapshot()] = new InvalidOperationException("second decoder failure"),
        };

        // Act
        await viewModel.ValidateSamplesCommand.ExecuteAsync(null);

        // Assert
        var request = dialogs.LastMessageRequest
            .Should()
            .BeOfType<MessageDialogRequest<bool>>()
            .Subject;
        request.Details.Should().Contain("first decoder failure")
            .And.Contain("second decoder failure");
    }


}
