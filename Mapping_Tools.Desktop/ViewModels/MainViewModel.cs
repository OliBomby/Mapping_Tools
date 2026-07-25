using System.Collections.ObjectModel;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.Desktop.Shell;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Coordinates explicit feature discovery, navigation, favorites, activation,
/// and the shell notification queue.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IShellFeatureRegistry _registry;
    private readonly ApplicationSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly IUserNotificationService _notifications;
    private readonly IUiDispatcher _dispatcher;
    private readonly Dictionary<string, ViewModelBase> _featureViewModels =
        new(StringComparer.OrdinalIgnoreCase);
    private string _searchText = string.Empty;
    private ViewModelBase? _currentFeature;
    private string _header = "Mapping Tools";
    private bool _isNavigationOpen = true;
    private bool _disposed;

    /// <summary>
    /// Creates the desktop shell and activates the first explicit registration.
    /// </summary>
    public MainViewModel(
        IShellFeatureRegistry registry,
        ApplicationSettings settings,
        ISettingsService settingsService,
        IUserNotificationService notifications,
        IUiDispatcher dispatcher)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        FeatureItems = registry.Features
            .Select((registration, order) => new ShellFeatureItemViewModel(
                registration,
                order,
                settings.FavoriteTools.Contains(registration.Id, StringComparer.OrdinalIgnoreCase),
                Activate,
                ToggleFavorite))
            .ToArray();
        VisibleFeatures = [];
        NotificationQueue = [];
        ToggleNavigationCommand = ReactiveCommand.Create(() =>
        {
            IsNavigationOpen = !IsNavigationOpen;
        });

        _notifications.Published += OnNotificationPublished;
        RefreshVisibleFeatures();
        Activate(FeatureItems[0]);
    }

    /// <summary>Gets every registered navigation item in declaration order.</summary>
    public IReadOnlyList<ShellFeatureItemViewModel> FeatureItems { get; }

    /// <summary>Gets the feature items matching the current query.</summary>
    public ObservableCollection<ShellFeatureItemViewModel> VisibleFeatures { get; }

    /// <summary>Gets queued notifications in publication order.</summary>
    public ObservableCollection<ShellNotificationViewModel> NotificationQueue { get; }

    /// <summary>Gets the command that expands or collapses the navigation pane.</summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ToggleNavigationCommand { get; }

    /// <summary>Gets or sets the case-insensitive feature search query.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            string normalized = value ?? string.Empty;
            if (_searchText == normalized)
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _searchText, normalized);
            RefreshVisibleFeatures();
        }
    }

    /// <summary>Gets the currently activated feature presentation model.</summary>
    public ViewModelBase? CurrentFeature
    {
        get => _currentFeature;
        private set => this.RaiseAndSetIfChanged(ref _currentFeature, value);
    }

    /// <summary>Gets the title of the currently activated feature.</summary>
    public string Header
    {
        get => _header;
        private set => this.RaiseAndSetIfChanged(ref _header, value);
    }

    /// <summary>Gets or sets whether the full navigation pane is visible.</summary>
    public bool IsNavigationOpen
    {
        get => _isNavigationOpen;
        set => this.RaiseAndSetIfChanged(ref _isNavigationOpen, value);
    }

    /// <summary>Unsubscribes the process-lifetime notification stream.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifications.Published -= OnNotificationPublished;
        if (CurrentFeature is IShellFeatureActivation activation)
        {
            activation.Deactivate();
        }
    }

    private void Activate(ShellFeatureItemViewModel item)
    {
        foreach (ShellFeatureItemViewModel featureItem in FeatureItems)
        {
            featureItem.IsActive = ReferenceEquals(featureItem, item);
        }

        if (CurrentFeature is IShellFeatureActivation previous)
        {
            previous.Deactivate();
        }

        if (!_featureViewModels.TryGetValue(item.Id, out ViewModelBase? viewModel))
        {
            ShellFeatureRegistration registration = _registry.Find(item.Id)
                ?? throw new InvalidOperationException($"Feature '{item.Id}' is not registered.");
            viewModel = registration.CreateViewModel();
            _featureViewModels.Add(item.Id, viewModel);
        }

        CurrentFeature = viewModel;
        Header = item.DisplayName == "Get started"
            ? "Mapping Tools"
            : $"Mapping Tools - {item.DisplayName}";
        if (viewModel is IShellFeatureActivation current)
        {
            current.Activate();
        }
    }

    private void ToggleFavorite(ShellFeatureItemViewModel item)
    {
        item.IsFavorite = !item.IsFavorite;
        _settings.FavoriteTools.RemoveAll(
            id => id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
        if (item.IsFavorite)
        {
            _settings.FavoriteTools.Add(item.Id);
        }

        _settingsService.Save(_settings);
        RefreshVisibleFeatures();
    }

    private void RefreshVisibleFeatures()
    {
        IEnumerable<ShellFeatureItemViewModel> matches = FeatureItems.Where(MatchesSearch);
        matches = matches
            .OrderByDescending(item => item.IsFavorite)
            .ThenBy(item => item.Order);

        VisibleFeatures.Clear();
        foreach (ShellFeatureItemViewModel item in matches)
        {
            VisibleFeatures.Add(item);
        }
    }

    private bool MatchesSearch(ShellFeatureItemViewModel item)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return item.SearchableText.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase);
    }

    private void OnNotificationPublished(
        object? sender,
        UserNotificationPublishedEventArgs eventArgs) =>
        _dispatcher.Post(() =>
            NotificationQueue.Add(new ShellNotificationViewModel(
                eventArgs.Notification,
                RemoveNotification)));

    private void RemoveNotification(ShellNotificationViewModel notification) =>
        NotificationQueue.Remove(notification);
}
