using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Updates;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Views;

namespace Mapping_Tools.Desktop.Updates;

/// <summary>
/// Bridges the Application updater lifecycle to owner-modal and modeless
/// Avalonia windows without exposing an Avalonia type to Application.
/// </summary>
public interface IUpdaterInteractionService : IDisposable
{
    /// <summary>
    /// Gets whether the shell must finish a wait-after-close update before it exits.
    /// </summary>
    bool ShouldUpdateOnClose { get; }

    /// <summary>
    /// Checks the release channel and shows the legacy decision window for an available update.
    /// </summary>
    /// <param name="allowSkippedVersion">Suppresses the persisted skipped version for startup checks when true.</param>
    /// <param name="notifyUser">Shows no-update and skipped-version messages for a manual check.</param>
    /// <param name="cancellationToken">Cancels network and release-metadata work.</param>
    Task CheckForUpdatesAsync(
        bool allowSkippedVersion,
        bool notifyUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a wait-after-close download, showing the legacy progress dialog
    /// when preparation is not already finished, then launches the updater.
    /// </summary>
    /// <param name="cancellationToken">Cancels the shutdown wait and package preparation.</param>
    /// <returns><see langword="true"/> when the owner may close; otherwise the update remains pending.</returns>
    Task<bool> CompleteUpdateOnCloseAsync(CancellationToken cancellationToken = default);
}

internal sealed class AvaloniaUpdaterInteractionService : IUpdaterInteractionService
{
    private readonly Func<MainWindow> _owner;
    private readonly IUpdateService _updates;
    private readonly IUserNotificationService _notifications;
    private readonly Func<IDialogService> _dialogs;
    private readonly IUiDispatcher _dispatcher;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private UpdaterWindow? _window;
    private UpdaterViewModel? _viewModel;
    private Task? _checkTask;
    private bool _waitAfterClose;
    private bool _disposed;

    internal AvaloniaUpdaterInteractionService(
        Func<MainWindow> owner,
        IUpdateService updates,
        IUserNotificationService notifications,
        Func<IDialogService> dialogs,
        IUiDispatcher dispatcher)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
        _notifications = notifications ??
            throw new ArgumentNullException(nameof(notifications));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <inheritdoc/>
    public bool ShouldUpdateOnClose => _waitAfterClose;

    /// <inheritdoc/>
    public Task CheckForUpdatesAsync(
        bool allowSkippedVersion,
        bool notifyUser,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_window is not null)
        {
            return Task.CompletedTask;
        }

        if (_checkTask is { IsCompleted: false })
        {
            return _checkTask;
        }

        CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetimeCancellation.Token);
        _checkTask = CheckCoreAsync(
            allowSkippedVersion,
            notifyUser,
            linkedCancellation);
        return ObserveCheckAsync(_checkTask);
    }

    /// <inheritdoc/>
    public async Task<bool> CompleteUpdateOnCloseAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_waitAfterClose)
        {
            return true;
        }

        CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetimeCancellation.Token);
        try
        {
            Task? downloadTask = _updates.ActiveDownloadTask;
            if (downloadTask is null || downloadTask.IsFaulted || downloadTask.IsCanceled)
            {
                downloadTask = _updates.PrepareUpdateAsync(linkedCancellation.Token);
            }

            if (!downloadTask.IsCompletedSuccessfully)
            {
                await ShowShutdownDownloadAsync(downloadTask, linkedCancellation.Token);
            }

            await downloadTask.ConfigureAwait(true);
            linkedCancellation.Token.ThrowIfCancellationRequested();
            _updates.StartUpdateProcess(restartAfterUpdate: false);
            _waitAfterClose = false;
            return true;
        }
        catch (OperationCanceledException) when (linkedCancellation.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(exception, notifyUser: true);
            return false;
        }
        finally
        {
            linkedCancellation.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetimeCancellation.Cancel();
        if (_window is not null)
        {
            _window.Close();
        }

        _viewModel?.Dispose();
        _viewModel = null;
        _lifetimeCancellation.Dispose();
    }

    private async Task CheckCoreAsync(
        bool allowSkippedVersion,
        bool notifyUser,
        CancellationTokenSource linkedCancellation)
    {
        try
        {
            CancellationToken cancellationToken = linkedCancellation.Token;
            UpdateCheckResult result = await _updates
                .CheckForUpdatesAsync(allowSkippedVersion, cancellationToken)
                .ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();

            switch (result.Availability)
            {
                case UpdateAvailability.None when notifyUser:
                    await PublishAsync(
                        UserNotificationSeverity.Information,
                        "Update check",
                        "No new versions available.");
                    break;

                case UpdateAvailability.Skipped when notifyUser:
                    await PublishAsync(
                        UserNotificationSeverity.Information,
                        "Update check",
                        $"Version {result.LatestVersion} skipped because of user config.");
                    break;

                case UpdateAvailability.Available:
                    ShowDecisionWindow(result);
                    break;
            }
        }
        catch (OperationCanceledException) when (linkedCancellation.IsCancellationRequested)
        {
            // Closing the application or canceling an explicit check is not an
            // updater failure and must not show a late dialog.
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(exception, notifyUser);
        }
        finally
        {
            linkedCancellation.Dispose();
        }
    }

    private void ShowDecisionWindow(UpdateCheckResult result)
    {
        if (_disposed || _window is not null)
        {
            return;
        }

        UpdaterViewModel viewModel = new(
            _updates,
            result,
            _notifications,
            _dialogs(),
            _dispatcher);
        UpdaterWindow window = new()
        {
            DataContext = viewModel,
            ShowActivated = true
        };
        viewModel.CloseRequested += (_, _) => CloseDecisionWindow(window);
        viewModel.ApplicationCloseRequested += (_, _) =>
        {
            CloseDecisionWindow(window);
            _owner().Close();
        };
        window.Closed += (_, _) => DecisionWindowClosed(window, viewModel);
        _viewModel = viewModel;
        _window = window;
        window.Show(_owner());
    }

    private void CloseDecisionWindow(UpdaterWindow window)
    {
        if (window.IsVisible)
        {
            window.Close();
        }
    }

    private void DecisionWindowClosed(
        UpdaterWindow window,
        UpdaterViewModel viewModel)
    {
        if (!ReferenceEquals(_window, window))
        {
            return;
        }

        _waitAfterClose = viewModel.UpdateAfterClose;
        if (!_waitAfterClose)
        {
            _updates.AbandonUpdate();
        }

        _window = null;
        _viewModel = null;
        viewModel.Dispose();
    }

    private async Task ShowShutdownDownloadAsync(
        Task downloadTask,
        CancellationToken cancellationToken)
    {
        UpdateCheckResult check = _updates.LastCheck
            ?? throw new InvalidOperationException(
                "The updater lost its release check before shutdown.");
        UpdaterViewModel viewModel = new(
            _updates,
            check,
            _notifications,
            _dialogs(),
            _dispatcher,
            downloadImmediately: true);
        UpdaterWindow window = new()
        {
            DataContext = viewModel,
            ShowActivated = true
        };

        _ = CloseAfterDownloadAsync(downloadTask, window, cancellationToken);
        try
        {
            await window.ShowDialog<object?>(_owner()).ConfigureAwait(true);
        }
        finally
        {
            viewModel.Dispose();
        }
    }

    private async Task CloseAfterDownloadAsync(
        Task downloadTask,
        UpdaterWindow window,
        CancellationToken cancellationToken)
    {
        try
        {
            await downloadTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // The caller awaits the same task and reports the original failure.
        }

        _dispatcher.Post(() =>
        {
            if (window.IsVisible)
            {
                window.Close();
            }
        });
    }

    private async Task ObserveCheckAsync(Task checkTask)
    {
        try
        {
            await checkTask.ConfigureAwait(true);
        }
        finally
        {
            if (ReferenceEquals(_checkTask, checkTask))
            {
                _checkTask = null;
            }
        }
    }

    private async Task ReportFailureAsync(Exception exception, bool notifyUser)
    {
        if (_disposed)
        {
            return;
        }

        await _dialogs().ShowMessageAsync(new MessageDialogRequest<bool>(
            "Updater error",
            "UPDATER_EXCEPTION: " + exception.Message,
            [new DialogChoice<bool>("OK", true, IsDefault: true, IsCancel: true)],
            true)).ConfigureAwait(true);

        if (notifyUser)
        {
            await PublishAsync(
                UserNotificationSeverity.Error,
                "Error fetching update",
                exception.Message,
                exception);
        }
    }

    private Task PublishAsync(
        UserNotificationSeverity severity,
        string title,
        string message,
        Exception? exception = null) =>
        _notifications.PublishAsync(new UserNotification(
            severity,
            title,
            message,
            exception));
}
