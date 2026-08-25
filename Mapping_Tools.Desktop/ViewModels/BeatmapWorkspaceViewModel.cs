using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Interactions.Dialogs;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Presents current-map selection and safety-copy actions in the desktop shell.
/// </summary>
public sealed partial class BeatmapWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly IBeatmapWorkspace workspace;
    private readonly IBeatmapBackupService backupService;
    private readonly IQuickUndoCommandService quickUndoService;
    private readonly IFilePicker filePicker;
    private readonly IFileRevealService fileRevealService;
    private readonly IApplicationDirectories applicationDirectories;
    private readonly ApplicationSettings settings;
    private readonly IDialogService dialogs;
    private readonly IUserNotificationService notifications;
    private readonly IUiDispatcher dispatcher;
    private bool disposed;

    /// <summary>
    /// Creates shell workspace state over the process-lifetime selection and backup services.
    /// </summary>
    /// <param name="workspace">Owns selected paths and recent-map history.</param>
    /// <param name="backupService">Creates and restores durable safety copies.</param>
    /// <param name="quickUndoService">Runs the same newest-backup restore used by the global hotkey.</param>
    /// <param name="filePicker">Presents the native restore-source picker.</param>
    /// <param name="fileRevealService">Opens application-owned folders in the platform file manager.</param>
    /// <param name="applicationDirectories">Supplies the Mapping Tools data directory.</param>
    /// <param name="settings">Supplies the configured backups directory and reload policy.</param>
    /// <param name="dialogs">Asks for an explicit override when backup metadata differs.</param>
    /// <param name="notifications">Publishes completion and recoverable failure outcomes.</param>
    /// <param name="dispatcher">Marshals workspace notifications onto the UI thread.</param>
    public BeatmapWorkspaceViewModel(
        IBeatmapWorkspace workspace,
        IBeatmapBackupService backupService,
        IQuickUndoCommandService quickUndoService,
        IFilePicker filePicker,
        IFileRevealService fileRevealService,
        IApplicationDirectories applicationDirectories,
        ApplicationSettings settings,
        IDialogService dialogs,
        IUserNotificationService notifications,
        IUiDispatcher dispatcher)
    {
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        this.quickUndoService = quickUndoService ?? throw new ArgumentNullException(nameof(quickUndoService));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.fileRevealService = fileRevealService ?? throw new ArgumentNullException(nameof(fileRevealService));
        this.applicationDirectories = applicationDirectories ?? throw new ArgumentNullException(nameof(applicationDirectories));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        this.workspace.SelectionChanged += OnSelectionChanged;
        this.workspace.RestoreMostRecent();
        RefreshSelection(this.workspace.SelectedPaths);
    }

    /// <summary>Gets selected filenames joined in tool-consumption order.</summary>
    [ObservableProperty]
    public partial string SelectedMapNames { get; private set; } = string.Empty;

    /// <summary>Gets full selected paths separated by lines for the shell tooltip.</summary>
    [ObservableProperty]
    public partial string SelectedMapToolTip { get; private set; } = string.Empty;

    /// <summary>Gets the legacy singular or plural selected-map count label.</summary>
    [ObservableProperty]
    public partial string SelectedMapCountText { get; private set; } = "(0) maps total";

    /// <summary>Gets whether at least one beatmap is selected.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateBackupCommand))]
    public partial bool HasSelection { get; private set; }

    /// <summary>Gets whether exactly one beatmap is available as a restore destination.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestoreBackupCommand))]
    public partial bool HasSingleSelection { get; private set; }

    /// <summary>
    /// Installs paths supplied by the platform drag-and-drop adapter.
    /// </summary>
    /// <param name="paths">Local file or directory paths in drop order.</param>
    public void SetDroppedPaths(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        workspace.SetSelection(paths, BeatmapSelectionSource.DragAndDrop);
    }

    /// <summary>Stops observing process-lifetime workspace changes.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        workspace.SelectionChanged -= OnSelectionChanged;
    }

    [RelayCommand]
    private Task OpenBeatmapAsync() =>
        RunUserOperationAsync(
            () => workspace.PickBeatmapsAsync(true),
            "Open beatmap");

    [RelayCommand]
    private async Task OpenCurrentBeatmapAsync()
    {
        await RunUserOperationAsync(async () =>
        {
            CurrentBeatmapSelectionResult result =
                await workspace.SelectCurrentBeatmapAsync();
            if (result.Status == CurrentBeatmapSelectionStatus.Unavailable)
            {
                await PublishAsync(
                    UserNotificationSeverity.Warning,
                    "Current beatmap unavailable",
                    "Mapping Tools could not determine the beatmap open in osu!.");
            }
            else if (result.Status == CurrentBeatmapSelectionStatus.FileMissing)
            {
                await PublishAsync(
                    UserNotificationSeverity.Warning,
                    "Current beatmap is missing",
                    $"The path reported by osu! does not exist: {result.Path}");
            }
        }, "Open current beatmap");
    }

    private bool CanCreateBackup() => HasSelection;

    [RelayCommand(CanExecute = nameof(CanCreateBackup))]
    private Task CreateBackupAsync() =>
        RunUserOperationAsync(async () =>
        {
            BeatmapBackupResult result = await backupService.CreateAsync(
                workspace.SelectedPaths,
                BeatmapBackupReason.User,
                force: true);
            int count = result.Artifacts.Count;
            await PublishAsync(
                UserNotificationSeverity.Success,
                "Backup created",
                count == 1
                    ? "The selected beatmap was copied to the backups folder."
                    : $"{count} selected beatmaps were copied to the backups folder.");
        }, "Generate backup");

    private bool CanRestoreBackup() => HasSingleSelection;

    [RelayCommand(CanExecute = nameof(CanRestoreBackup))]
    private Task RestoreBackupAsync() =>
        RunUserOperationAsync(async () =>
        {
            IReadOnlyList<string> selected = await filePicker.PickOpenFilesAsync(
                new OpenFilePickerRequest
                {
                    Title = "Load backup",
                    SuggestedStartLocation = settings.BackupsPath,
                    AllowMultiple = false,
                    Filters = [CommonFilePickerFilters.BeatmapBackups]
                });
            if (selected.Count == 0)
            {
                return;
            }

            string destination = workspace.SelectedPaths.Single();
            try
            {
                await RestoreAsync(selected[0], destination, false);
            }
            catch (BeatmapBackupIncompatibleException exception)
            {
                bool restore = await dialogs.ShowMessageAsync(
                    new MessageDialogRequest<bool>(
                        "Load backup",
                        "The backup belongs to a different beatmap. Load it anyway?",
                        [
                            new DialogChoice<bool>("Load anyway", true, IsDefault: true),
                            new DialogChoice<bool>("Cancel", false, IsCancel: true)
                        ],
                        dismissResult: false,
                        details: $"Backup: {exception.BackupFileName}{Environment.NewLine}Current: {exception.DestinationFileName}"));
                if (!restore)
                {
                    return;
                }

                await RestoreAsync(selected[0], destination, true);
            }

            await PublishAsync(
                UserNotificationSeverity.Success,
                "Backup loaded",
                "The selected backup replaced the current beatmap successfully.");
        }, "Load backup");

    [RelayCommand]
    private Task QuickUndoAsync() =>
        RunUserOperationAsync(
            () => quickUndoService.ExecuteAsync(),
            "QuickUndo");

    [RelayCommand]
    private Task OpenBackupsFolderAsync() =>
        RevealAsync(settings.BackupsPath, "backups folder");

    [RelayCommand]
    private Task OpenApplicationFolderAsync() =>
        RevealAsync(applicationDirectories.ApplicationData, "Mapping Tools folder");

    private Task RestoreAsync(
        string backupPath,
        string destinationPath,
        bool allowDifferentFilename) =>
        backupService.RestoreAsync(
            backupPath,
            destinationPath,
            allowDifferentFilename,
            reloadEditor: settings.AutoReload);

    private async Task RevealAsync(string path, string description)
    {
        await RunUserOperationAsync(async () =>
        {
            bool accepted = await fileRevealService.RevealAsync(path);
            if (!accepted)
            {
                await PublishAsync(
                    UserNotificationSeverity.Warning,
                    "Could not open folder",
                    $"The operating system did not open the {description}.");
            }
        }, $"Open {description}");
    }

    private async Task RunUserOperationAsync(Func<Task> operation, string title)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            // Native picker and dialog cancellation is an ordinary no-op.
        }
        catch (Exception exception)
        {
            await PublishAsync(
                UserNotificationSeverity.Error,
                title,
                exception.Message,
                exception);
        }
    }

    private void OnSelectionChanged(
        object? sender,
        BeatmapSelectionChangedEventArgs eventArgs) =>
        dispatcher.Post(() => RefreshSelection(eventArgs.Paths));

    private void RefreshSelection(IReadOnlyList<string> paths)
    {
        SelectedMapNames = string.Join("|", paths.Select(Path.GetFileName));
        SelectedMapToolTip = string.Join(Environment.NewLine, paths);
        int count = paths.Count;
        SelectedMapCountText = count == 1
            ? "(1) map total"
            : $"({count}) maps total";
        HasSelection = count > 0;
        HasSingleSelection = count == 1;
    }

    private Task PublishAsync(
        UserNotificationSeverity severity,
        string title,
        string message,
        Exception? exception = null) =>
        notifications.PublishAsync(
            new UserNotification(severity, title, message, exception));
}
