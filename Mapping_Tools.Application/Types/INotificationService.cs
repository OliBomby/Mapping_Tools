namespace Mapping_Tools.Application.Types;

public enum NotificationType
{
    Info,
    Warning,
    Error
}

public interface INotification
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
    string Title { get; }
    string Message { get; }
    NotificationType Type { get; }
}

public interface INotificationService
{
    void AddNotification(string title, string message, NotificationType type);
    
    void RemoveNotification(Guid notificationId);
    
    IEnumerable<INotification> GetNotifications();
    
    void ClearNotifications();
    
    event EventHandler<INotification> NotificationAdded;
    event EventHandler<INotification> NotificationRemoved;
}