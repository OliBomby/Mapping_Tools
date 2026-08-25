using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Workspace.Contracts;

namespace Mapping_Tools.Application.Tests.TestDoubles;

internal sealed class RecordingProgress<T> : IProgress<T>
{
    public List<T> Values { get; } = [];

    public void Report(T value)
    {
        Values.Add(value);
    }
}

internal sealed class RecordingBeatmapFileSystem : IBeatmapFileSystem
{
    public HashSet<string> ExistingPaths { get; } = new(StringComparer.Ordinal);

    public Func<string, bool>? FileExistsResolver { get; init; }

    public Func<string, string?>? ParentDirectoryResolver { get; init; }

    public bool FileExists(string path)
    {
        return FileExistsResolver?.Invoke(path) ?? ExistingPaths.Contains(path);
    }

    public string? GetParentDirectory(string filePath)
    {
        return ParentDirectoryResolver?.Invoke(filePath)
            ?? Path.GetDirectoryName(filePath);
    }
}

internal sealed class RecordingFilePicker : IFilePicker
{
    public bool CanOpenFiles { get; init; } = true;

    public bool CanSaveFiles { get; init; } = true;

    public bool CanPickFolders { get; init; } = true;

    public IReadOnlyList<string> OpenFiles { get; init; } = [];

    public string? SavePath { get; init; }

    public IReadOnlyList<string> Folders { get; init; } = [];

    public OpenFilePickerRequest? LastOpenRequest { get; private set; }

    public SaveFilePickerRequest? LastSaveRequest { get; private set; }

    public OpenFolderPickerRequest? LastFolderRequest { get; private set; }

    public Task<IReadOnlyList<string>> PickOpenFilesAsync(
        OpenFilePickerRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!CanOpenFiles) throw new NotSupportedException("Opening files is not supported by this test picker.");

        LastOpenRequest = request;
        return Task.FromResult(OpenFiles);
    }

    public Task<string?> PickSaveFileAsync(
        SaveFilePickerRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!CanSaveFiles) throw new NotSupportedException("Saving files is not supported by this test picker.");

        LastSaveRequest = request;
        return Task.FromResult(SavePath);
    }

    public Task<IReadOnlyList<string>> PickFoldersAsync(
        OpenFolderPickerRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!CanPickFolders) throw new NotSupportedException("Picking folders is not supported by this test picker.");

        LastFolderRequest = request;
        return Task.FromResult(Folders);
    }
}
