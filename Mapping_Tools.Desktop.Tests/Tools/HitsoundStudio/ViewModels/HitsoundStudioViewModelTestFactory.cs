using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Avalonia.Controls;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels.Adapters;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.ViewModels;

internal static class HitsoundStudioViewModelTestFactory
{
    internal static (HitsoundStudioViewModel ViewModel, ObservableHitsoundLayer First, ObservableHitsoundLayer Second)
        CreateMixedSelection()
    {
        var viewModel = CreateViewModel(
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

    internal static HitsoundStudioViewModel CreateViewModel(
        RecordingHitsoundStudioService service,
        RecordingAudioGenerator audioGenerator,
        RecordingPlaybackService playback,
        TestCurrentBeatmapDialogService? currentBeatmap = null,
        UserNotificationService? notifications = null,
        TestDialogService? dialogs = null,
        TestBeatmapWorkspace? workspace = null,
        TestFilePicker? filePicker = null,
        Func<Window>? owner = null)
    {
        notifications ??= new UserNotificationService();
        ToolExecutionService execution = new(
            notifications,
            TimeProvider.System);
        return new HitsoundStudioViewModel(
            service,
            audioGenerator,
            playback,
            dialogs ?? new TestDialogService(),
            notifications,
            execution,
            currentBeatmap ?? new TestCurrentBeatmapDialogService(),
            workspace ?? new TestBeatmapWorkspace(),
            filePicker ?? new TestFilePicker(),
            new StubHitsoundStudioFileSystem(),
            new StubProjectStore(),
            new DesktopApplicationSettings(),
            new TestApplicationDirectories(),
            owner ?? (static () => null!));
    }

    internal sealed class RecordingHitsoundStudioService : IHitsoundStudioService
    {
        public Action<IReadOnlyList<HitsoundLayer>>? ReloadAction { get; set; }

        public IReadOnlyList<HitsoundLayer> ImportResult { get; set; } = [];

        public IReadOnlyDictionary<SampleGeneratingArgs, Exception> ValidationFailures { get; set; } =
            new Dictionary<SampleGeneratingArgs, Exception>(new SampleGeneratingArgsComparer());

        public Task<IReadOnlyList<HitsoundLayer>> ImportAsync(
            HitsoundStudioImportRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ImportResult);
        }

        public Task<IReadOnlyList<HitsoundLayer>> ReloadAsync(
            IReadOnlyList<HitsoundLayer> layers,
            CancellationToken cancellationToken = default)
        {
            ReloadAction?.Invoke(layers);
            return Task.FromResult(layers);
        }

        public Task<IReadOnlyDictionary<SampleGeneratingArgs, Exception>> ValidateSamplesAsync(
            IReadOnlyList<SampleGeneratingArgs> samples,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ValidationFailures);
        }

        public Task<HitsoundStudioExportResult> ExportAsync(
            HitsoundStudioServiceOptions project,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<HitsoundStudioExportResult>(null!);
        }
    }

    internal sealed class RecordingAudioGenerator : IAudioGenerator
    {
        private readonly TaskCompletionSource<bool> releaseFirstGeneration =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int generationCount;

        private readonly bool blockFirstGeneration;

        public RecordingAudioGenerator(bool blockFirstGeneration = true)
        {
            this.blockFirstGeneration = blockFirstGeneration;
        }

        public TaskCompletionSource<bool> FirstGenerationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> FirstGenerationCanceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<AudioGenerationRequest> Requests { get; } = [];

        public async Task<AudioClip> GenerateAsync(
            AudioGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            if (Interlocked.Increment(ref generationCount) == 1 && blockFirstGeneration)
            {
                FirstGenerationStarted.TrySetResult(true);
                await using var registration = cancellationToken.Register(() => FirstGenerationCanceled.TrySetResult(true));
                await releaseFirstGeneration.Task;
            }

            return new AudioClip(new AudioFormat(8000, 1), [0.1f]);
        }

        public void ReleaseFirstGeneration()
        {
            releaseFirstGeneration.TrySetResult(true);
        }
    }

    internal sealed class RecordingPlaybackService : IAudioPlaybackService
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

    internal sealed class RecordingPlaybackSession : IAudioPlaybackSession
    {
        public int StopCount { get; private set; }
        public AudioPlaybackState State => AudioPlaybackState.Playing;

        public TimeSpan Position => TimeSpan.Zero;

        public Task Completion => Task.CompletedTask;

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
        public bool FileExists(string path)
        {
            return false;
        }

        public bool DirectoryExists(string path)
        {
            return false;
        }

        public string ReadAllText(string path)
        {
            return string.Empty;
        }

        public void WriteAllText(string path, string text)
        {
        }

        public void Delete(string path)
        {
        }

        public string GetParentFolder(string path)
        {
            return Path.GetDirectoryName(path) ?? string.Empty;
        }

        public string CombinePath(string parent, string child)
        {
            return Path.Combine(parent, child);
        }

        public string? GetParentDirectory(string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }

        public IReadOnlyList<string> EnumerateFiles(
            string directory,
            string searchPattern,
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return [];
        }

        public void EnsureDirectoryExists(string path)
        {
        }

        public byte[] ReadAllBytes(string path)
        {
            return [];
        }

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
