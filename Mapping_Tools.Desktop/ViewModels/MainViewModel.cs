using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Coordinates explicit feature discovery, navigation, favorites, activation,
/// and the shell notification queue.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IShellFeatureRegistry _registry;
    private readonly ApplicationSettings _settings;
    private readonly IUserNotificationService _notifications;
    private readonly IUiDispatcher _dispatcher;
    private readonly Dictionary<string, ObservableObject> _featureViewModels =
        new(StringComparer.OrdinalIgnoreCase);
    private string _searchText = string.Empty;
    private bool _disposed;

    /// <summary>
    /// Creates the desktop shell and activates the first explicit registration.
    /// </summary>
    public MainViewModel(
        IShellFeatureRegistry registry,
        ApplicationSettings settings,
        IUserNotificationService notifications,
        IUiDispatcher dispatcher)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        FeatureItems = registry.Features
            .Select((registration, order) => new ShellFeatureItemViewModel(
                registration,
                order,
                settings.FavoriteTools.Contains(registration.Id, StringComparer.OrdinalIgnoreCase),
                item => SelectedFeature = item,
                ToggleFavorite))
            .ToArray();
        VisibleFeatures = [];
        NotificationQueue = [];
        _notifications.Published += OnNotificationPublished;
        RefreshVisibleFeatures();
        SelectedFeature = FeatureItems[0];
    }

    /// <summary>Gets every registered navigation item in declaration order.</summary>
    public IReadOnlyList<ShellFeatureItemViewModel> FeatureItems { get; }

    /// <summary>Gets the feature items matching the current query.</summary>
    public ObservableCollection<ShellFeatureItemViewModel> VisibleFeatures { get; }

    /// <summary>
    /// Gets or sets the selected navigation item and activates its registered feature.
    /// </summary>
    [ObservableProperty]
    public partial ShellFeatureItemViewModel? SelectedFeature { get; set; }

    /// <summary>Gets queued notifications in publication order.</summary>
    public ObservableCollection<ShellNotificationViewModel> NotificationQueue { get; }

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

            if (SetProperty(ref _searchText, normalized))
            {
                RefreshVisibleFeatures();
            }
        }
    }

    /// <summary>Gets the currently activated feature presentation model.</summary>
    [ObservableProperty]
    public partial ObservableObject? CurrentFeature { get; private set; }

    /// <summary>Gets the title of the currently activated feature.</summary>
    [ObservableProperty]
    public partial string Header { get; private set; } = "Mapping Tools";

    /// <summary>Gets or sets whether the full navigation pane is visible.</summary>
    [ObservableProperty]
    public partial bool IsNavigationOpen { get; set; } = true;

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

        if (!_featureViewModels.TryGetValue(item.Id, out ObservableObject? viewModel))
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

    partial void OnSelectedFeatureChanged(ShellFeatureItemViewModel? value)
    {
        if (value is not null)
        {
            Activate(value);
        }
    }

    [RelayCommand]
    private void ToggleNavigation() =>
        IsNavigationOpen = !IsNavigationOpen;

    private void ToggleFavorite(ShellFeatureItemViewModel item)
    {
        item.IsFavorite = !item.IsFavorite;
        _settings.FavoriteTools.RemoveAll(
            id => id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
        if (item.IsFavorite)
        {
            _settings.FavoriteTools.Add(item.Id);
        }

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
