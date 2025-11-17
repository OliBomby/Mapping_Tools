using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Collections.Specialized;
using Mapping_Tools.Application.Types;
using ReactiveUI;
using Material.Icons;
using Avalonia.Media;

namespace Mapping_Tools.Desktop.ViewModels
{
    public class NotificationItemViewModel : ReactiveObject
    {
        public Guid Id { get; }
        public DateTime CreatedAt { get; }
        public string Title { get; }
        public string Message { get; }
        public NotificationType Type { get; }

        public MaterialIconKind IconKind { get; }
        public IBrush IconBrush { get; }

        public NotificationItemViewModel(INotification n)
        {
            Id = n.Id;
            CreatedAt = n.CreatedAt;
            Title = n.Title;
            Message = n.Message;
            Type = n.Type;

            // Map icon and color based on notification type
            switch (Type)
            {
                case NotificationType.Info:
                    IconKind = MaterialIconKind.Information;
                    IconBrush = Brushes.DodgerBlue;
                    break;
                case NotificationType.Warning:
                    IconKind = MaterialIconKind.Alert;
                    IconBrush = Brushes.Orange;
                    break;
                case NotificationType.Error:
                    IconKind = MaterialIconKind.AlertCircle;
                    IconBrush = Brushes.Red;
                    break;
                default:
                    IconKind = MaterialIconKind.Information;
                    IconBrush = Brushes.Gray;
                    break;
            }
        }
    }

    public class NotificationsViewModel : ViewModelBase
    {
        private readonly INotificationService _notificationService;

        public ObservableCollection<NotificationItemViewModel> Notifications { get; } = new ObservableCollection<NotificationItemViewModel>();

        public ReactiveCommand<Unit, Unit> ClearAll { get; }
        public ReactiveCommand<Guid, Unit> Remove { get; }

        public NotificationsViewModel(INotificationService notificationService)
        {
            _notificationService = notificationService;

            // load existing notifications
            foreach (var n in _notificationService.GetNotifications().OrderByDescending(o => o.CreatedAt))
                Notifications.Add(new NotificationItemViewModel(n));

            Notifications.CollectionChanged += NotificationsOnCollectionChanged;

            _notificationService.NotificationAdded += (_, n) => {
                // newest at top
                Notifications.Insert(0, new NotificationItemViewModel(n));
            };

            _notificationService.NotificationRemoved += (_, n) => {
                var item = Notifications.FirstOrDefault(o => o.Id == n.Id);
                if (item != null) Notifications.Remove(item);
            };

            ClearAll = ReactiveCommand.Create(() => _notificationService.ClearNotifications());
            Remove = ReactiveCommand.Create<Guid>(id => _notificationService.RemoveNotification(id));
        }

        private void NotificationsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Raise property changed for HasNoNotifications
            this.RaisePropertyChanged(nameof(HasNoNotifications));
        }

        public bool HasNoNotifications => Notifications.Count == 0;
    }
}
