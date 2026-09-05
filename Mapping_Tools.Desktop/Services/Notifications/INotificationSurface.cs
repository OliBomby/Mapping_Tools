using Mapping_Tools.Application.Execution.UserNotification.Models;

namespace Mapping_Tools.Desktop.Services.Notifications;

/// <summary>
///     Provides the non-modal desktop surface used by the notification presenter.
/// </summary>
public interface INotificationSurface
{
    /// <summary>
    ///     Queues a notification on the desktop shell's snackbar surface.
    /// </summary>
    /// <param name="notification">The notification content to display.</param>
    void ShowSnackbar(UserNotification notification);
}
