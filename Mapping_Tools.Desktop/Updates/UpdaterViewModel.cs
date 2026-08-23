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
    private readonly IDialogService _dialogs;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUserNotificationService _notifications;
    private readonly IUpdateService _updates;
    private bool _disposed;

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
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
        Check = check ?? throw new ArgumentNullException(nameof(check));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        ReleaseTitle = string.IsNullOrWhiteSpace(check.ReleaseTitle)
            ? "Loading release notes..."
            : check.ReleaseTitle;
        ReleaseBody = check.ReleaseBody ?? string.Empty;
        IsReadyPanelVisible = !downloadImmediately;
        IsDownloadPanelVisible = downloadImmediately;
        _updates.ProgressChanged += OnProgressChanged;
    }

    internal UpdateCheckResult Check { get; }

    internal bool UpdateAfterClose { get; private set; }

    internal Task? DownloadTask { get; private set; }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _updates.ProgressChanged -= OnProgressChanged;
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
            _updates.StartUpdateProcess(true);
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
            DownloadTask = _updates.PrepareUpdateAsync();
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
            _updates.SkipCurrentVersion();
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
        if (!_disposed) DownloadProgress = progress;
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
        DownloadTask = _updates.PrepareUpdateAsync();
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
        if (_disposed) return;

        await _dialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
            "Updater error",
            "UPDATER_EXCEPTION: " + exception.Message,
            [new DialogChoice<bool>("OK", true, true, true)],
            true));
        await _notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            "Error fetching update",
            exception.Message,
            exception));
    }

    private void OnProgressChanged(
        object? sender,
        UpdateProgressChangedEventArgs eventArgs)
    {
        if (_disposed) return;

        _dispatcher.Post(() => SetDownloadProgress(eventArgs.Progress));
    }
}
