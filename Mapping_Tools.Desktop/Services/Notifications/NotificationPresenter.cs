using System.Diagnostics;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Desktop.Services.Dialogs;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Services.Notifications;

internal sealed class NotificationPresenter : IHostedService
{
    private readonly SemaphoreSlim errorDialogGate = new(1, 1);
    private readonly IDialogService dialogs;
    private readonly IUserNotificationService notifications;
    private readonly INotificationSurface surface;
    private bool started;

    public NotificationPresenter(
        IUserNotificationService notifications,
        IDialogService dialogs,
        INotificationSurface surface)
    {
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.surface = surface ?? throw new ArgumentNullException(nameof(surface));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (started) return Task.CompletedTask;

        started = true;
        notifications.Published += OnNotificationPublished;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!started) return Task.CompletedTask;

        started = false;
        notifications.Published -= OnNotificationPublished;
        return Task.CompletedTask;
    }

    private void OnNotificationPublished(
        object? sender,
        UserNotificationPublishedEventArgs eventArgs)
    {
        UserNotification notification = eventArgs.Notification;
        if (notification.Severity == UserNotificationSeverity.Error)
        {
            _ = ShowErrorDialogAsync(notification);
            return;
        }

        surface.ShowSnackbar(notification);
    }

    private async Task ShowErrorDialogAsync(UserNotification notification)
    {
        await errorDialogGate.WaitAsync();
        try
        {
            await dialogs.ShowMessageAsync(
                new MessageDialogRequest<bool>(
                    $"Error: {notification.Title}",
                    notification.Message,
                    [new DialogChoice<bool>("OK", true, IsDefault: true, IsCancel: true)],
                    true,
                    FormatExceptionDetails(notification.Exception)));
        }
        catch (Exception exception)
        {
            Trace.TraceError("Could not show an error message dialog: {0}", exception);
        }
        finally
        {
            errorDialogGate.Release();
        }
    }

    private static string? FormatExceptionDetails(Exception? exception)
    {
        if (exception is null) return null;

        List<string> sections = [];
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            sections.Add(
                string.IsNullOrWhiteSpace(current.StackTrace)
                    ? current.Message
                    : $"{current.Message}{Environment.NewLine}{Environment.NewLine}{current.StackTrace}");
        }

        return string.Join(
            $"{Environment.NewLine}{Environment.NewLine}Inner exception:{Environment.NewLine}{Environment.NewLine}",
            sections);
    }
}
