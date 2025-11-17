using Mapping_Tools.Application.Types;

namespace Mapping_Tools.Application.Services;

public record Notification(Guid Id, DateTime CreatedAt, string Title, string Message, NotificationType Type) : INotification;

public class NotificationService : INotificationService
{
    private readonly List<INotification> _notifications = [];

    public void AddNotification(string title, string message, NotificationType type)
    {
        var notification = new Notification(
            Guid.NewGuid(),
            DateTime.Now,
            title,
            message,
            type
        );
        _notifications.Add(notification);
        NotificationAdded?.Invoke(this, notification);
    }

    public void RemoveNotification(Guid notificationId)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification == null)
            return;

        _notifications.Remove(notification);
        NotificationRemoved?.Invoke(this, notification);
    }

    public IEnumerable<INotification> GetNotifications()
    {
        return _notifications;
    }

    public void ClearNotifications()
    {
        foreach (var notification in _notifications.ToList())
            RemoveNotification(notification.Id);
    }

    public event EventHandler<INotification>? NotificationAdded;
    public event EventHandler<INotification>? NotificationRemoved;
}