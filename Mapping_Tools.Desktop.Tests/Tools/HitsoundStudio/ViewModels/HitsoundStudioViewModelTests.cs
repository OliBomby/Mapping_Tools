using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels.Adapters;
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
        HitsoundStudioViewModel viewModel = CreateViewModel(service);
        viewModel.SelectedLayer = new ObservableHitsoundLayer(
            new HitsoundLayer(
                "layer",
                SampleSet.Normal,
                Hitsound.Normal,
                new SampleGeneratingArgs("sample.wav"),
                new LayerImportArgs()));

        // Act
        Task firstPreview = viewModel.PreviewCommand.ExecuteAsync(null);
        await service.FirstPreviewStarted.Task;
        Task secondPreview = viewModel.PreviewCommand.ExecuteAsync(null);
        await service.FirstPreviewCanceled.Task;
        service.ReleaseFirstPreview();
        await firstPreview;
        await secondPreview;

        // Assert
        service.Sessions.Should().HaveCount(2);
        service.Sessions[0].StopCount.Should().Be(1);
        service.Sessions[1].StopCount.Should().Be(0);
        viewModel.ResultSummary.Should().Be("Playing selected layer.");

        await viewModel.DisposeAsync();
        service.Sessions[1].StopCount.Should().Be(1);
    }

    private static HitsoundStudioViewModel CreateViewModel(RecordingHitsoundStudioService service)
    {
        UserNotificationService notifications = new();
        ToolExecutionService execution = new(
            notifications,
            new RecordingEditorReloadService(),
            new ApplicationSettings(),
            TimeProvider.System);
        return new HitsoundStudioViewModel(
            service,
            new TestDialogService(),
            execution,
            new RecordingCurrentBeatmapLocator(),
            new TestBeatmapWorkspace(),
            new TestFilePicker(),
            new StubHitsoundStudioFileSystem(),
            new StubProjectStore(),
            new ApplicationSettings(),
            new TestApplicationDirectories());
    }

    private sealed class RecordingHitsoundStudioService : IHitsoundStudioService
    {
        private readonly TaskCompletionSource<bool> firstPreviewStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> firstPreviewCanceled =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> releaseFirstPreview =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int previewCount;

        public List<RecordingPlaybackSession> Sessions { get; } = [];

        public TaskCompletionSource<bool> FirstPreviewStarted => firstPreviewStarted;

        public TaskCompletionSource<bool> FirstPreviewCanceled => firstPreviewCanceled;

        public void ReleaseFirstPreview()
        {
            releaseFirstPreview.TrySetResult(true);
        }

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

        public async Task<IAudioPlaybackSession> PreviewAsync(
            SampleGeneratingArgs sample,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref previewCount) == 1)
            {
                firstPreviewStarted.TrySetResult(true);
                using CancellationTokenRegistration registration = cancellationToken.Register(
                    () => firstPreviewCanceled.TrySetResult(true));
                await releaseFirstPreview.Task;
            }

            var session = new RecordingPlaybackSession();
            Sessions.Add(session);
            return session;
        }

        public Task<HitsoundStudioExportResult> ExportAsync(
            HitsoundStudioServiceOptions project,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<HitsoundStudioExportResult>(null!);
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
