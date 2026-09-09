using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels.Adapters;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.ViewModels;

[TestClass]
public sealed class HitsoundStudioViewModelTests
{
    [TestMethod]
    public async Task PreviewCommand_WhenSupersededDuringGeneration_StopsStaleSessionAndKeepsLatestPlaying()
    {
        // Arrange
        RecordingHitsoundStudioService service = new();
        RecordingAudioGenerator audioGenerator = new();
        RecordingPlaybackService playback = new();
        HitsoundStudioViewModel viewModel = CreateViewModel(service, audioGenerator, playback);
        ObservableHitsoundLayer layer = new(
            new HitsoundLayer(
                "layer",
                SampleSet.Normal,
                Hitsound.Normal,
                new SampleGeneratingArgs("sample.wav"),
                new LayerImportArgs()));
        viewModel.SetSelection([layer]);

        // Act
        Task firstPreview = viewModel.PreviewCommand.ExecuteAsync(null);
        await audioGenerator.FirstGenerationStarted.Task;
        Task secondPreview = viewModel.PreviewCommand.ExecuteAsync(null);
        await audioGenerator.FirstGenerationCanceled.Task;
        audioGenerator.ReleaseFirstGeneration();
        await firstPreview;
        await secondPreview;

        // Assert
        playback.Sessions.Should().HaveCount(2);
        playback.Sessions[0].StopCount.Should().Be(1);
        playback.Sessions[1].StopCount.Should().Be(0);

        await viewModel.DisposeAsync();
        playback.Sessions[1].StopCount.Should().Be(1);
    }

    [TestMethod]
    public void SetSelection_WithMixedLayerValues_ShowsNeutralEditorValues()
    {
        // Arrange
        var (viewModel, _, _) = CreateMixedSelection();

        // Act
        viewModel.SetSelection(viewModel.Layers);

        // Assert
        viewModel.EditName.Should().BeEmpty();
        viewModel.EditSampleSet.Should().BeNull();
        viewModel.EditHitsound.Should().BeNull();
        viewModel.EditTimes.Should().BeEmpty();
        viewModel.EditSamplePath.Should().BeEmpty();
        viewModel.EditSampleVolume.Should().BeEmpty();
        viewModel.EditImportType.Should().BeNull();
        viewModel.EditImportDiscriminateVolumes.Should().BeFalse();
    }

    [TestMethod]
    public void SetSelection_WithEquivalentTimeLists_ShowsCommonTimes()
    {
        // Arrange
        var (viewModel, first, second) = CreateMixedSelection();
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
        var (viewModel, first, second) = CreateMixedSelection();
        viewModel.SetSelection([first]);

        // Act
        viewModel.SetSelection([first, second]);

        // Assert
        viewModel.EditName.Should().BeEmpty();
        viewModel.EditTimes.Should().BeEmpty();
    }

    [TestMethod]
    public void EditSharedValues_WithMultipleSelectedLayers_UpdatesEveryLayer()
    {
        // Arrange
        var (viewModel, first, second) = CreateMixedSelection();
        viewModel.SetSelection(viewModel.Layers);

        // Act
        viewModel.EditName = "shared";
        viewModel.EditSampleSet = SampleSet.Soft;
        viewModel.EditHitsound = Hitsound.Clap;
        viewModel.EditTimes = [300, 100];
        viewModel.EditSampleVolume = "25";
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
    public async Task LoadBaseBeatmapCommand_WhenCurrentBeatmapIsAvailable_SetsBaseBeatmap()
    {
        // Arrange
        RecordingCurrentBeatmapLocator currentBeatmap = new("current.osu");
        HitsoundStudioViewModel viewModel = CreateViewModel(
            new RecordingHitsoundStudioService(),
            new RecordingAudioGenerator(),
            new RecordingPlaybackService(),
            currentBeatmap);

        // Act
        await viewModel.LoadBaseBeatmapCommand.ExecuteAsync(null);

        // Assert
        viewModel.BaseBeatmap.Should().Be("current.osu");
        currentBeatmap.FindCount.Should().Be(1);
    }

    [TestMethod]
    public async Task LoadBaseBeatmapCommand_WhenCurrentBeatmapIsUnavailable_PreservesBaseBeatmapAndReportsError()
    {
        // Arrange
        RecordingCurrentBeatmapLocator currentBeatmap = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        HitsoundStudioViewModel viewModel = CreateViewModel(
            new RecordingHitsoundStudioService(),
            new RecordingAudioGenerator(),
            new RecordingPlaybackService(),
            currentBeatmap,
            notifications);
        viewModel.BaseBeatmap = "existing.osu";

        // Act
        await viewModel.LoadBaseBeatmapCommand.ExecuteAsync(null);

        // Assert
        viewModel.BaseBeatmap.Should().Be("existing.osu");
        UserNotification notification = published.Should().ContainSingle().Which;
        notification.Severity.Should().Be(UserNotificationSeverity.Error);
        notification.Title.Should().Be("Load current beatmap failed");
        notification.Message.Should().Be(
            "Open a beatmap in osu! before using the current editor state.");
    }

    private static (HitsoundStudioViewModel ViewModel, ObservableHitsoundLayer First, ObservableHitsoundLayer Second)
        CreateMixedSelection()
    {
        HitsoundStudioViewModel viewModel = CreateViewModel(
            new RecordingHitsoundStudioService(),
            new RecordingAudioGenerator(),
            new RecordingPlaybackService());
        ObservableHitsoundLayer first = new(new HitsoundLayer(
            "first",
            SampleSet.Normal,
            Hitsound.Normal,
            new SampleGeneratingArgs("first.wav", 0.5, 0, 0, -1, -1, -1, -1, -1),
            new LayerImportArgs()));
        first.Times = [100];
        ObservableHitsoundLayer second = new(new HitsoundLayer(
            "second",
            SampleSet.Drum,
            Hitsound.Whistle,
            new SampleGeneratingArgs("second.wav", 0.75, 0, 0, -1, -1, -1, -1, -1),
            new LayerImportArgs(ImportType.Hitsounds)
            {
                DiscriminateVolumes = true,
            }));
        second.Times = [200];
        viewModel.Layers.Add(first);
        viewModel.Layers.Add(second);

        return (viewModel, first, second);
    }

    private static HitsoundStudioViewModel CreateViewModel(
        RecordingHitsoundStudioService service,
        RecordingAudioGenerator audioGenerator,
        RecordingPlaybackService playback,
        RecordingCurrentBeatmapLocator? currentBeatmap = null,
        UserNotificationService? notifications = null)
    {
        notifications ??= new UserNotificationService();
        ToolExecutionService execution = new(
            notifications,
            new RecordingEditorReloadService(),
            new DesktopApplicationSettings(),
            TimeProvider.System);
        return new HitsoundStudioViewModel(
            service,
            audioGenerator,
            playback,
            new TestDialogService(),
            notifications,
            execution,
            currentBeatmap ?? new RecordingCurrentBeatmapLocator(),
            new TestBeatmapWorkspace(),
            new TestFilePicker(),
            new StubHitsoundStudioFileSystem(),
            new StubProjectStore(),
            new DesktopApplicationSettings(),
            new TestApplicationDirectories(),
            static () => null!);
    }

    private sealed class RecordingHitsoundStudioService : IHitsoundStudioService
    {
        public Task<IReadOnlyList<HitsoundLayer>> ImportAsync(
            HitsoundStudioImportRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<HitsoundLayer>>([]);
        }

        public Task<IReadOnlyList<HitsoundLayer>> ReloadAsync(
            IReadOnlyList<HitsoundLayer> layers,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<HitsoundLayer>>([]);
        }

        public Task<IReadOnlyDictionary<SampleGeneratingArgs, Exception>> ValidateSamplesAsync(
            IReadOnlyList<SampleGeneratingArgs> samples,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<SampleGeneratingArgs, Exception>>(
                new Dictionary<SampleGeneratingArgs, Exception>());
        }

        public Task<HitsoundStudioExportResult> ExportAsync(
            HitsoundStudioServiceOptions project,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<HitsoundStudioExportResult>(null!);
        }
    }

    private sealed class RecordingAudioGenerator : IAudioGenerator
    {
        private readonly TaskCompletionSource<bool> firstGenerationStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> firstGenerationCanceled =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> releaseFirstGeneration =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int generationCount;

        public TaskCompletionSource<bool> FirstGenerationStarted => firstGenerationStarted;

        public TaskCompletionSource<bool> FirstGenerationCanceled => firstGenerationCanceled;

        public void ReleaseFirstGeneration()
        {
            releaseFirstGeneration.TrySetResult(true);
        }

        public async Task<AudioClip> GenerateAsync(
            AudioGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref generationCount) == 1)
            {
                firstGenerationStarted.TrySetResult(true);
                using CancellationTokenRegistration registration = cancellationToken.Register(
                    () => firstGenerationCanceled.TrySetResult(true));
                await releaseFirstGeneration.Task;
            }

            return new AudioClip(new AudioFormat(8000, 1), [0.1f]);
        }
    }

    private sealed class RecordingPlaybackService : IAudioPlaybackService
    {
        public List<RecordingPlaybackSession> Sessions { get; } = [];

        public Task<IAudioPlaybackSession> PlayAsync(
            AudioClip clip,
            AudioPlaybackOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var session = new RecordingPlaybackSession();
            Sessions.Add(session);
            return Task.FromResult<IAudioPlaybackSession>(session);
        }
    }

    private sealed class RecordingPlaybackSession : IAudioPlaybackSession
    {
        public AudioPlaybackState State => AudioPlaybackState.Playing;

        public TimeSpan Position => TimeSpan.Zero;

        public Task Completion => Task.CompletedTask;

        public int StopCount { get; private set; }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public ValueTask StopAsync()
        {
            StopCount++;
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return StopAsync();
        }
    }

    private sealed class StubHitsoundStudioFileSystem : IBeatmapsetFileSystem
    {
        public bool FileExists(string path) => false;

        public bool DirectoryExists(string path) => false;

        public IReadOnlyList<string> ReadAllLines(string path) => [];

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
        }

        public void Delete(string path)
        {
        }

        public string GetParentFolder(string path) => Path.GetDirectoryName(path) ?? string.Empty;

        public string CombinePath(string parent, string child) => Path.Combine(parent, child);

        public string? GetParentDirectory(string filePath) => Path.GetDirectoryName(filePath);

        public IReadOnlyList<string> EnumerateFiles(
            string directory,
            string searchPattern,
            SearchOption searchOption = SearchOption.TopDirectoryOnly) => [];

        public void EnsureDirectoryExists(string path)
        {
        }

        public byte[] ReadAllBytes(string path) => [];

        public void WriteAllBytes(string path, ReadOnlySpan<byte> bytes, bool overwrite = false)
        {
        }

        public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
        {
        }

        public void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
        {
        }

        public IBeatmapsetFileTransaction BeginTransaction(string targetDirectory)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubProjectStore : IProjectStore
    {
        public void EnsureDirectoryExists(string path)
        {
        }

        public Task SaveAsync<TProject>(
            string path,
            TProject project,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<TProject> LoadAsync<TProject>(
            string path,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<TProject>(new NotSupportedException());
        }
    }
}
