using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Updates;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Views;

namespace Mapping_Tools.Desktop.Updates;

/// <summary>
///     Bridges the Application updater lifecycle to owner-modal and modeless
///     Avalonia windows without exposing an Avalonia type to Application.
/// </summary>
public interface IUpdaterInteractionService : IDisposable
{
    /// <summary>
    ///     Gets whether the shell must finish a wait-after-close update before it exits.
    /// </summary>
    bool ShouldUpdateOnClose { get; }

    /// <summary>
    ///     Checks the release channel and shows the legacy decision window for an available update.
    /// </summary>
    /// <param name="allowSkippedVersion">Suppresses the persisted skipped version for startup checks when true.</param>
    /// <param name="notifyUser">Shows no-update and skipped-version messages for a manual check.</param>
    /// <param name="cancellationToken">Cancels network and release-metadata work.</param>
    Task CheckForUpdatesAsync(
        bool allowSkippedVersion,
        bool notifyUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Completes a wait-after-close download, showing the legacy progress dialog
    ///     when preparation is not already finished, then launches the updater.
    /// </summary>
    /// <param name="cancellationToken">Cancels the shutdown wait and package preparation.</param>
    /// <returns><see langword="true" /> when the owner may close; otherwise the update remains pending.</returns>
    Task<bool> CompleteUpdateOnCloseAsync(CancellationToken cancellationToken = default);
}

internal sealed class AvaloniaUpdaterInteractionService : IUpdaterInteractionService
{
    private readonly Func<IDialogService> dialogs;
    private readonly IUiDispatcher dispatcher;
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private readonly IUserNotificationService notifications;
    private readonly Func<MainWindow> owner;
    private readonly IUpdateService updates;
    private Task? checkTask;
    private bool disposed;
    private UpdaterViewModel? viewModel;
    private UpdaterWindow? window;

    internal AvaloniaUpdaterInteractionService(
        Func<MainWindow> owner,
        IUpdateService updates,
        IUserNotificationService notifications,
        Func<IDialogService> dialogs,
        IUiDispatcher dispatcher)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.updates = updates ?? throw new ArgumentNullException(nameof(updates));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <inheritdoc />
    public bool ShouldUpdateOnClose { get; private set; }

    /// <inheritdoc />
    public Task CheckForUpdatesAsync(
        bool allowSkippedVersion,
        bool notifyUser,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (window is not null) return Task.CompletedTask;

        if (checkTask is { IsCompleted: false }) return checkTask;

        var linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                lifetimeCancellation.Token);
        checkTask = CheckCoreAsync(
            allowSkippedVersion,
            notifyUser,
            linkedCancellation);
        return ObserveCheckAsync(checkTask);
    }

    /// <inheritdoc />
    public async Task<bool> CompleteUpdateOnCloseAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!ShouldUpdateOnClose) return true;

        var linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                lifetimeCancellation.Token);
        try
        {
            var downloadTask = updates.ActiveDownloadTask;
            if (downloadTask is null || downloadTask.IsFaulted || downloadTask.IsCanceled) downloadTask = updates.PrepareUpdateAsync(linkedCancellation.Token);

            if (!downloadTask.IsCompletedSuccessfully) await ShowShutdownDownloadAsync(downloadTask, linkedCancellation.Token);

            await downloadTask.ConfigureAwait(true);
            linkedCancellation.Token.ThrowIfCancellationRequested();
            updates.StartUpdateProcess(false);
            ShouldUpdateOnClose = false;
            return true;
        }
        catch (OperationCanceledException) when (linkedCancellation.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(exception, true);
            return false;
        }
        finally
        {
            linkedCancellation.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        lifetimeCancellation.Cancel();
        if (window is not null) window.Close();

        viewModel?.Dispose();
        viewModel = null;
        lifetimeCancellation.Dispose();
    }

    private async Task CheckCoreAsync(
        bool allowSkippedVersion,
        bool notifyUser,
        CancellationTokenSource linkedCancellation)
    {
        try
        {
            var cancellationToken = linkedCancellation.Token;
            var result = await updates
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
        if (disposed || this.window is not null) return;

        UpdaterViewModel viewModel = new(
            updates,
            result,
            notifications,
            dialogs(),
            dispatcher);
        UpdaterWindow window = new()
        {
            DataContext = viewModel,
            ShowActivated = true,
        };
        viewModel.CloseRequested += (_, _) => CloseDecisionWindow(window);
        viewModel.ApplicationCloseRequested += (_, _) =>
        {
            CloseDecisionWindow(window);
            owner().Close();
        };
        window.Closed += (_, _) => DecisionWindowClosed(window, viewModel);
        this.viewModel = viewModel;
        this.window = window;
        window.Show(owner());
    }

    private void CloseDecisionWindow(UpdaterWindow window)
    {
        if (window.IsVisible) window.Close();
    }

    private void DecisionWindowClosed(
        UpdaterWindow window,
        UpdaterViewModel viewModel)
    {
        if (!ReferenceEquals(this.window, window)) return;

        ShouldUpdateOnClose = viewModel.UpdateAfterClose;
        if (!ShouldUpdateOnClose) updates.AbandonUpdate();

        this.window = null;
        this.viewModel = null;
        viewModel.Dispose();
    }

    private async Task ShowShutdownDownloadAsync(
        Task downloadTask,
        CancellationToken cancellationToken)
    {
        var check = updates.LastCheck
                    ?? throw new InvalidOperationException(
                        "The updater lost its release check before shutdown.");
        UpdaterViewModel viewModel = new(
            updates,
            check,
            notifications,
            dialogs(),
            dispatcher,
            true);
        UpdaterWindow window = new()
        {
            DataContext = viewModel,
            ShowActivated = true,
        };

        _ = CloseAfterDownloadAsync(downloadTask, window, cancellationToken);
        try
        {
            await window.ShowDialog<object?>(owner()).ConfigureAwait(true);
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

        dispatcher.Post(() =>
        {
            if (window.IsVisible) window.Close();
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
            if (ReferenceEquals(this.checkTask, checkTask)) this.checkTask = null;
        }
    }

    private async Task ReportFailureAsync(Exception exception, bool notifyUser)
    {
        if (disposed) return;

        await dialogs().ShowMessageAsync(new MessageDialogRequest<bool>(
            "Updater error",
            "UPDATER_EXCEPTION: " + exception.Message,
            [new DialogChoice<bool>("OK", true, true, true)],
            true)).ConfigureAwait(true);

        if (notifyUser)
            await PublishAsync(
                UserNotificationSeverity.Error,
                "Error fetching update",
                exception.Message,
                exception);
    }

    private Task PublishAsync(
        UserNotificationSeverity severity,
        string title,
        string message,
        Exception? exception = null)
    {
        return notifications.PublishAsync(new UserNotification(
            severity,
            title,
            message,
            exception));
    }
}
