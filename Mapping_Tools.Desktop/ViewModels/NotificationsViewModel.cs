using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using Mapping_Tools.Application.Types;
using ReactiveUI;
using Material.Icons;
using Avalonia.Media;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services;

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

        private string _relativeTime = string.Empty;
        public string RelativeTime
        {
            get => _relativeTime;
            private set => this.RaiseAndSetIfChanged(ref _relativeTime, value);
        }

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

            UpdateRelativeTime();
        }

        public void UpdateRelativeTime()
        {
            var now = DateTime.Now;
            var ts = now - CreatedAt;
            if (ts.TotalSeconds < 5) RelativeTime = "just now";
            else if (ts.TotalSeconds < 60) RelativeTime = $"{Math.Floor(ts.TotalSeconds)}s ago";
            else if (ts.TotalMinutes < 60) RelativeTime = $"{Math.Floor(ts.TotalMinutes)}m ago";
            else if (ts.TotalHours < 24) RelativeTime = $"{Math.Floor(ts.TotalHours)}h ago";
            else if (ts.TotalDays < 7) RelativeTime = $"{Math.Floor(ts.TotalDays)}d ago";
            else if (ts.TotalDays < 30) RelativeTime = $"{Math.Floor(ts.TotalDays / 7)}w ago";
            else if (ts.TotalDays < 365) RelativeTime = $"{Math.Floor(ts.TotalDays / 30)}mo ago";
            else RelativeTime = $"{Math.Floor(ts.TotalDays / 365)}y ago";
        }
    }

    public class NotificationsViewModel : ReactiveObject
    {
        private readonly INotificationService _notificationService;
        private readonly UserSettingsService _userSettingsService;
        private readonly DispatcherTimer _timer;

        public ObservableCollection<NotificationItemViewModel> Notifications { get; } = [];
        
        public UserSettings Settings => _userSettingsService.Settings;

        public ReactiveCommand<Unit, Unit> ClearAll { get; }
        public ReactiveCommand<Guid, Unit> Remove { get; }

        public NotificationsViewModel(INotificationService notificationService, UserSettingsService userSettingsService)
        {
            _notificationService = notificationService;
            _userSettingsService = userSettingsService;

            // load existing notifications (newest first)
            foreach (var n in _notificationService.GetNotifications().OrderByDescending(o => o.CreatedAt))
                Notifications.Add(new NotificationItemViewModel(n));

            Notifications.CollectionChanged += NotificationsOnCollectionChanged;

            _notificationService.NotificationAdded += (_, n) => {
                // newest at top — ensure update on UI thread
                Dispatcher.UIThread.Post(() => Notifications.Insert(0, new NotificationItemViewModel(n)));
            };

            _notificationService.NotificationRemoved += (_, n) => {
                Dispatcher.UIThread.Post(() => {
                    var item = Notifications.FirstOrDefault(o => o.Id == n.Id);
                    if (item != null) Notifications.Remove(item);
                });
            };

            ClearAll = ReactiveCommand.Create(() => _notificationService.ClearNotifications());
            Remove = ReactiveCommand.Create<Guid>(id => _notificationService.RemoveNotification(id));

            // Ticker to update RelativeTime display every second
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => {
                foreach (var item in Notifications)
                    item.UpdateRelativeTime();
            };
            _timer.Start();
        }

        private void NotificationsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Raise property changed for HasNoNotifications
            this.RaisePropertyChanged(nameof(HasNoNotifications));
        }

        public bool HasNoNotifications => Notifications.Count == 0;
    }
}
