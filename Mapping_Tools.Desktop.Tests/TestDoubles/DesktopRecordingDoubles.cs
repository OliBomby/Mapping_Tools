using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Desktop.Tests.TestDoubles;

internal sealed class RecordingCurrentBeatmapLocator(string? path = null) : ICurrentBeatmapLocator
{
    public string? Path { get; set; } = path;

    public int FindCount { get; private set; }

    public Task<string?> FindCurrentBeatmapAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        FindCount++;
        return Task.FromResult(Path);
    }
}

internal sealed class RecordingEditorReloadService : IEditorReloadService
{
    public int ReloadCount { get; private set; }

    public bool FileHadBeenWritten { get; private set; }

    public Func<bool>? FileWrittenResolver { get; init; }

    public Exception? Failure { get; init; }

    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReloadCount++;
        FileHadBeenWritten = FileWrittenResolver?.Invoke() ?? false;
        return Failure is null
            ? Task.CompletedTask
            : Task.FromException(Failure);
    }
}

internal sealed class RecordingLiveBeatmapReader : ILiveBeatmapReader
{
    public RecordingLiveBeatmapReader(LiveBeatmapSnapshot? snapshot)
    {
        Snapshot = snapshot;
    }

    public RecordingLiveBeatmapReader(Exception failure)
    {
        Failure = failure;
    }

    public LiveBeatmapSnapshot? Snapshot { get; set; }

    public Exception? Failure { get; set; }

    public int ReadCount { get; private set; }

    public Task<LiveBeatmapSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCount++;
        return Failure is null
            ? Task.FromResult(Snapshot)
            : Task.FromException<LiveBeatmapSnapshot?>(Failure);
    }
}
