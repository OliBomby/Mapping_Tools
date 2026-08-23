using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Updates;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.Updates;

/// <summary>
///     Holds the updater decision and package-preparation state for one window.
/// </summary>
internal sealed partial class UpdaterViewModel : ObservableObject, IDisposable
{
    private readonly IDialogService dialogs;
    private readonly IUiDispatcher dispatcher;
    private readonly IUserNotificationService notifications;
    private readonly IUpdateService updates;
    private bool disposed;

    [ObservableProperty] private double downloadProgress;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(InstallNowCommand))]
    private bool isBusy;

    [ObservableProperty] private bool isDownloadPanelVisible;

    [ObservableProperty] private bool isReadyPanelVisible;

    [ObservableProperty] private string releaseBody = string.Empty;

    [ObservableProperty] private string releaseTitle = string.Empty;

    internal UpdaterViewModel(
        IUpdateService updates,
        UpdateCheckResult check,
        IUserNotificationService notifications,
        IDialogService dialogs,
        IUiDispatcher dispatcher,
        bool downloadImmediately = false)
    {
        this.updates = updates ?? throw new ArgumentNullException(nameof(updates));
        Check = check ?? throw new ArgumentNullException(nameof(check));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        ReleaseTitle = string.IsNullOrWhiteSpace(check.ReleaseTitle)
            ? "Loading release notes..."
            : check.ReleaseTitle;
        ReleaseBody = check.ReleaseBody ?? string.Empty;
        IsReadyPanelVisible = !downloadImmediately;
        IsDownloadPanelVisible = downloadImmediately;
        this.updates.ProgressChanged += OnProgressChanged;
    }

    internal UpdateCheckResult Check { get; }

    internal bool UpdateAfterClose { get; private set; }

    internal Task? DownloadTask { get; private set; }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        updates.ProgressChanged -= OnProgressChanged;
    }

    internal event EventHandler? CloseRequested;

    internal event EventHandler? ApplicationCloseRequested;

    [RelayCommand(CanExecute = nameof(CanInstallNow))]
    private async Task InstallNowAsync()
    {
        IsDownloadPanelVisible = true;
        IsReadyPanelVisible = false;
        IsBusy = true;
        try
        {
            await BeginDownloadAsync();
            updates.StartUpdateProcess(true);
            UpdateAfterClose = false;
            CloseRequested?.Invoke(this, EventArgs.Empty);
            ApplicationCloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is a lifecycle outcome, not an updater error.
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void InstallAfterClosing()
    {
        IsDownloadPanelVisible = true;
        IsReadyPanelVisible = false;
        UpdateAfterClose = true;

        try
        {
            DownloadTask = updates.PrepareUpdateAsync();
            _ = ObserveDownloadFailureAsync(DownloadTask);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            UpdateAfterClose = false;
            _ = ReportFailureAsync(exception);
        }
    }

    [RelayCommand]
    private void SkipVersion()
    {
        try
        {
            updates.SkipCurrentVersion();
            UpdateAfterClose = false;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            _ = ReportFailureAsync(exception);
        }
    }

    internal void SetDownloadProgress(double progress)
    {
        if (!disposed) DownloadProgress = progress;
    }

    internal void ClearWaitAfterClose()
    {
        UpdateAfterClose = false;
    }

    private bool CanInstallNow()
    {
        return !IsBusy;
    }

    private async Task BeginDownloadAsync()
    {
        DownloadTask = updates.PrepareUpdateAsync();
        await DownloadTask;
    }

    private async Task ObserveDownloadFailureAsync(Task downloadTask)
    {
        try
        {
            await downloadTask;
        }
        catch (OperationCanceledException)
        {
            // The owning lifecycle decides whether a canceled download should
            // be retried during shutdown.
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(exception);
        }
    }

    private async Task ReportFailureAsync(Exception exception)
    {
        if (disposed) return;

        await dialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
            "Updater error",
            "UPDATER_EXCEPTION: " + exception.Message,
            [new DialogChoice<bool>("OK", true, true, true)],
            true));
        await notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            "Error fetching update",
            exception.Message,
            exception));
    }

    private void OnProgressChanged(
        object? sender,
        UpdateProgressChangedEventArgs eventArgs)
    {
        if (disposed) return;

        dispatcher.Post(() => SetDownloadProgress(eventArgs.Progress));
    }
}
