using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Interactions.Dialogs;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.Tests.TestDoubles;

internal sealed class TestBetterSaveService : IBetterSaveService
{
    public int ExecutionCount { get; private set; }

    public Task<BetterSaveResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        ExecutionCount++;
        return Task.FromResult(new BetterSaveResult(BetterSaveStatus.NoCurrentBeatmap));
    }
}

internal sealed class TestHotkeyBindingCoordinator : IHotkeyBindingCoordinator
{
    public HotkeySettings? QuickRun { get; private set; }

    public HotkeySettings? QuickUndo { get; private set; }

    public HotkeySettings? BetterSave { get; private set; }

    public void ApplyQuickRun(HotkeySettings? hotkey)
    {
        QuickRun = hotkey;
    }

    public void ApplyQuickUndo(HotkeySettings? hotkey)
    {
        QuickUndo = hotkey;
    }

    public void ApplyBetterSave(HotkeySettings? hotkey)
    {
        BetterSave = hotkey;
    }
}

internal sealed class TestBetterSaveOverrideService : IBetterSaveOverrideService
{
    public List<(string SongsPath, bool Enabled)> Configurations { get; } = [];

    public bool Stopped { get; private set; }

    public void Configure(string songsPath, bool enabled)
    {
        Configurations.Add((songsPath, enabled));
    }

    public void Stop()
    {
        Stopped = true;
    }
}

internal sealed class TestBeatmapWorkspace : IBeatmapWorkspace
{
    private readonly List<RecentBeatmap> recentMaps = [];
    private readonly List<string> selectedPaths = [];

    public BeatmapSelectionSource? LastSelectionSource { get; private set; }

    public CurrentBeatmapSelectionResult CurrentBeatmapResult { get; set; } =
        new(CurrentBeatmapSelectionStatus.Unavailable, null);

    public event EventHandler<BeatmapSelectionChangedEventArgs>? SelectionChanged;

    public IReadOnlyList<string> SelectedPaths => selectedPaths.ToArray();

    public IReadOnlyList<RecentBeatmap> RecentMaps => recentMaps.ToArray();

    public bool RestoreMostRecent()
    {
        if (recentMaps.Count == 0) return false;

        SetSelection(
            recentMaps[0].Path.Split('|', StringSplitOptions.RemoveEmptyEntries),
            BeatmapSelectionSource.Startup);
        return true;
    }

    public void SetSelection(
        IEnumerable<string> paths,
        BeatmapSelectionSource source = BeatmapSelectionSource.Programmatic)
    {
        selectedPaths.Clear();
        selectedPaths.AddRange(paths);
        LastSelectionSource = source;
        SelectionChanged?.Invoke(
            this,
            new BeatmapSelectionChangedEventArgs(selectedPaths.ToArray(), source));
    }

    public void ClearSelection(
        BeatmapSelectionSource source = BeatmapSelectionSource.Programmatic)
    {
        SetSelection([], source);
    }

    public bool RemoveRecent(string path)
    {
        return recentMaps.RemoveAll(recent => recent.Path == path) > 0;
    }

    public IReadOnlyList<string> GetMissingSelectedPaths()
    {
        return [];
    }

    public Task<bool> PickBeatmapsAsync(
        bool allowMultiple,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<CurrentBeatmapSelectionResult> SelectCurrentBeatmapAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CurrentBeatmapResult);
    }

    public void SetRecentMaps(params RecentBeatmap[] recentMaps)
    {
        this.recentMaps.Clear();
        this.recentMaps.AddRange(recentMaps);
    }
}

internal sealed class TestBeatmapBackupService : IBeatmapBackupService
{
    public List<(IReadOnlyList<string> Paths, BeatmapBackupReason Reason, bool Force)> CreateRequests { get; } = [];

    public List<(string Backup, string Destination, bool AllowDifferentFilename, bool ReloadEditor)> RestoreRequests { get; } = [];

    public bool RejectFirstRestoreAsIncompatible { get; set; }

    public Task<BeatmapBackupResult> CreateAsync(
        IEnumerable<string> sourcePaths,
        BeatmapBackupReason reason,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        string[] paths = sourcePaths.ToArray();
        CreateRequests.Add((paths, reason, force));
        IReadOnlyList<BeatmapBackupArtifact> artifacts = paths
            .Select(path => new BeatmapBackupArtifact(
                path + ".backup",
                path,
                reason,
                false,
                DateTimeOffset.UnixEpoch))
            .ToArray();
        return Task.FromResult(new BeatmapBackupResult(artifacts, false));
    }

    public Task<BeatmapBackupResult> CreateAsync(
        BeatmapEditingSession session,
        BeatmapBackupReason reason,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CreateRequests.Add(([session.Editor.Path], reason, force));
        return Task.FromResult(
            new BeatmapBackupResult(
                [
                    new BeatmapBackupArtifact(
                        session.Editor.Path + ".backup",
                        session.Editor.Path,
                        reason,
                        session.Source == BeatmapEditingSource.LiveEditor,
                        DateTimeOffset.UnixEpoch),
                ],
                false));
    }

    public Task<BeatmapBackupArtifact?> CreatePeriodicIfChangedAsync(
        BeatmapEditingSession session,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<BeatmapRestoreResult> RestoreAsync(
        string backupPath,
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        RestoreRequests.Add((backupPath, destinationPath, allowDifferentFilename, reloadEditor));
        if (RejectFirstRestoreAsIncompatible && RestoreRequests.Count == 1) throw new BeatmapBackupIncompatibleException("other.osu", "current.osu");

        return Task.FromResult(new BeatmapRestoreResult(
            backupPath,
            destinationPath,
            new BeatmapBackupArtifact(
                destinationPath + ".safety",
                destinationPath,
                BeatmapBackupReason.RestoreSafety,
                false,
                DateTimeOffset.UnixEpoch)));
    }

    public Task<BeatmapRestoreResult?> QuickUndoAsync(
        string destinationPath,
        bool allowDifferentFilename = false,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<BeatmapRestoreResult?>(null);
    }
}

internal sealed class TestQuickUndoCommandService : IQuickUndoCommandService
{
    public int ExecutionCount { get; private set; }

    public Task<QuickUndoCommandResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        ExecutionCount++;
        return Task.FromResult(new QuickUndoCommandResult(QuickUndoCommandStatus.NoBackup));
    }
}

internal sealed class TestFilePicker : IFilePicker
{
    public IReadOnlyList<string> OpenFiles { get; set; } = [];

    public string? SavePath { get; set; }

    public IReadOnlyList<string> Folders { get; set; } = [];

    public Exception? ExceptionToThrow { get; init; }

    public OpenFilePickerRequest? LastOpenRequest { get; private set; }

    public SaveFilePickerRequest? LastSaveRequest { get; private set; }

    public OpenFolderPickerRequest? LastFolderRequest { get; private set; }

    public bool CanOpenFiles { get; init; } = true;

    public bool CanSaveFiles { get; init; } = true;

    public bool CanPickFolders { get; init; } = true;

    public Task<IReadOnlyList<string>> PickOpenFilesAsync(
        OpenFilePickerRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        LastOpenRequest = request;
        return Task.FromResult(OpenFiles);
    }

    public Task<string?> PickSaveFileAsync(
        SaveFilePickerRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        LastSaveRequest = request;
        return Task.FromResult(SavePath);
    }

    public Task<IReadOnlyList<string>> PickFoldersAsync(
        OpenFolderPickerRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        LastFolderRequest = request;
        return Task.FromResult(Folders);
    }
}

internal sealed class TestFileRevealService : IFileRevealService
{
    public List<string> RevealedPaths { get; } = [];

    public Task<bool> RevealAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        RevealedPaths.Add(path);
        return Task.FromResult(true);
    }
}

internal sealed class TestApplicationDirectories : IApplicationDirectories
{
    public string LocalApplicationData => @"C:\Local";

    public string ApplicationData => @"C:\Local\Mapping Tools";

    public string Exports => @"C:\Local\Mapping Tools\Exports";

    public string ConfigurationFile => @"C:\Local\Mapping Tools\config.json";

    public string PreferencesFile => @"C:\Local\Mapping Tools\preferences.json";

    public void EnsureCreated()
    {
    }
}

internal sealed class TestDialogService : IDialogService
{
    public bool BooleanResult { get; set; }

    public int MessageCount { get; private set; }

    public object? LastMessageRequest { get; private set; }

    public Task<TResult> ShowMessageAsync<TResult>(
        MessageDialogRequest<TResult> request,
        CancellationToken cancellationToken = default)
    {
        MessageCount++;
        LastMessageRequest = request;
        return Task.FromResult((TResult)(object)BooleanResult);
    }

    public Task<ValueDialogResult<TValue>> ShowValueAsync<TValue>(
        ValueDialogRequest<TValue> request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ValueDialogResult<TValue>(false, default));
    }
}

internal sealed class ImmediateTestDispatcher : IUiDispatcher
{
    public int PostCount { get; private set; }

    public void Post(Action action)
    {
        PostCount++;
        action();
    }
}
