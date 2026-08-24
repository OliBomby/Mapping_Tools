using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Application.Workspace.Contracts;

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

internal sealed class RecordingPlatformLauncher : IPlatformLauncher
{
    public bool AcceptUris { get; init; } = true;

    public bool AcceptFiles { get; init; } = true;

    public bool AcceptFolders { get; init; } = true;

    public List<Uri> OpenedUris { get; } = [];

    public List<string> OpenedFiles { get; } = [];

    public List<string> OpenedFolders { get; } = [];

    public Task<bool> OpenUriAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OpenedUris.Add(uri);
        return Task.FromResult(AcceptUris);
    }

    public Task<bool> OpenFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OpenedFiles.Add(path);
        return Task.FromResult(AcceptFiles);
    }

    public Task<bool> OpenFolderAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OpenedFolders.Add(path);
        return Task.FromResult(AcceptFolders);
    }
}
