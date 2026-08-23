using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Updates;
using Material.Icons;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Coordinates explicit feature discovery, navigation, favorites, activation,
///     and the shell notification queue.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private static readonly Uri WebsiteUri = new("https://mappingtools.github.io");
    private static readonly Uri GitHubUri = new("https://github.com/OliBomby/Mapping_Tools");
    private static readonly Uri DonateUri = new("https://ko-fi.com/olibomby");
    private readonly IBetterSaveService _betterSave;
    private readonly IDialogService _dialogs;
    private readonly IUiDispatcher _dispatcher;

    private readonly Dictionary<string, ObservableObject> _featureViewModels =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly IPlatformLauncher _launcher;
    private readonly IUserNotificationService _notifications;
    private readonly ProjectAutosaveCoordinator _projectCoordinator;
    private readonly IQuickRunCommandRegistry _quickRunRegistry;
    private readonly IShellFeatureRegistry _registry;
    private readonly ApplicationSettings _settings;
    private readonly IUpdaterInteractionService? _updaterInteraction;
    private bool _disposed;
    private string _searchText = string.Empty;

    /// <summary>
    ///     Creates the desktop shell and activates the first explicit registration.
    /// </summary>
    /// <param name="registry">Supplies explicitly registered features in navigation order.</param>
    /// <param name="quickRunRegistry">Tracks the command for the active QuickRun-capable feature.</param>
    /// <param name="settings">Owns persisted favorites and other shared preferences.</param>
    /// <param name="notifications">Supplies the process-lifetime user notification stream.</param>
    /// <param name="launcher">Opens support links through the operating system.</param>
    /// <param name="dispatcher">Marshals notification changes to the UI thread.</param>
    /// <param name="workspace">Presents current-map and safety-copy actions in shell chrome.</param>
    /// <param name="betterSave">Saves the current live editor state through the shared safety gateway.</param>
    /// <param name="dialogs">Presents shell-owned information dialogs.</param>
    /// <param name="projectCoordinator">Owns project menus and feature autosave lifecycle.</param>
    /// <param name="updaterInteraction">
    ///     Shows update decisions and owns update shutdown interaction when supplied by runtime
    ///     composition.
    /// </param>
    public MainViewModel(
        IShellFeatureRegistry registry,
        IQuickRunCommandRegistry quickRunRegistry,
        ApplicationSettings settings,
        IUserNotificationService notifications,
        IPlatformLauncher launcher,
        IUiDispatcher dispatcher,
        BeatmapWorkspaceViewModel workspace,
        IBetterSaveService betterSave,
        IDialogService dialogs,
        ProjectAutosaveCoordinator projectCoordinator,
        IUpdaterInteractionService? updaterInteraction = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _quickRunRegistry = quickRunRegistry ?? throw new ArgumentNullException(nameof(quickRunRegistry));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _betterSave = betterSave ?? throw new ArgumentNullException(nameof(betterSave));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _projectCoordinator = projectCoordinator ?? throw new ArgumentNullException(nameof(projectCoordinator));
        _updaterInteraction = updaterInteraction;

        FeatureItems = registry.Features
            .Select((registration, order) => new ShellFeatureItemViewModel(
                registration,
                order,
                settings.FavoriteTools.Contains(registration.Id, StringComparer.OrdinalIgnoreCase),
                item => SelectedFeature = item,
                ToggleFavorite))
            .ToArray();
        VisibleFeatures = [];
        NavigationEntries = [];
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
    ///     Gets the visible feature rows interleaved with inert section-divider markers.
    /// </summary>
    public ObservableCollection<object> NavigationEntries { get; }

    /// <summary>
    ///     Gets or sets the selected navigation item and activates its registered feature.
    /// </summary>
    [ObservableProperty]
    public partial ShellFeatureItemViewModel? SelectedFeature { get; set; }

    /// <summary>Gets or sets the navigation item currently targeted by keyboard input.</summary>
    [ObservableProperty]
    public partial ShellFeatureItemViewModel? HighlightedFeature { get; set; }

    /// <summary>Gets queued notifications in publication order.</summary>
    public ObservableCollection<ShellNotificationViewModel> NotificationQueue { get; }

    /// <summary>Gets current-map and backup actions shared by every shell feature.</summary>
    public BeatmapWorkspaceViewModel Workspace { get; }

    /// <summary>Gets the active feature's standard and additional project-menu commands.</summary>
    public IReadOnlyList<ShellProjectMenuItem> ProjectMenuItems { get; private set; } = [];

    /// <summary>Gets or sets the case-insensitive feature search query.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            string normalized = value ?? string.Empty;
            if (_searchText == normalized) return;

            if (SetProperty(ref _searchText, normalized)) RefreshVisibleFeatures();
        }
    }

    /// <summary>Gets the currently activated feature presentation model.</summary>
    [ObservableProperty]
    public partial ObservableObject? CurrentFeature { get; private set; }

    /// <summary>Gets the title of the currently activated feature.</summary>
    [ObservableProperty]
    public partial string Header { get; private set; } = "Mapping Tools";

    /// <summary>Gets the active feature's shell-owned horizontal scrolling behavior.</summary>
    [ObservableProperty]
    public partial ScrollBarVisibility ContentHorizontalScrollBarVisibility { get; private set; }

    /// <summary>Gets the active feature's shell-owned vertical scrolling behavior.</summary>
    [ObservableProperty]
    public partial ScrollBarVisibility ContentVerticalScrollBarVisibility { get; private set; }

    /// <summary>Gets whether the active feature exposes typed project operations.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(NewProjectCommand))]
    public partial bool HasProjectMenu { get; private set; }

    /// <summary>Gets or sets whether the full navigation pane is visible.</summary>
    [ObservableProperty]
    public partial bool IsNavigationOpen { get; set; } = true;

    /// <summary>Unsubscribes the process-lifetime notification stream.</summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _notifications.Published -= OnNotificationPublished;
        if (CurrentFeature is IShellProjectFeature projectFeature) _projectCoordinator.Deactivate(projectFeature);
        if (CurrentFeature is IQuickRun quickRun) DeactivateQuickRun(quickRun);
        if (CurrentFeature is IShellFeatureActivation activation) activation.Deactivate();
        ProjectMenuItems = [];
        OnPropertyChanged(nameof(ProjectMenuItems));
    }

    private void Activate(ShellFeatureItemViewModel item)
    {
        HighlightedFeature = item;

        foreach (var featureItem in FeatureItems) featureItem.IsActive = ReferenceEquals(featureItem, item);

        if (CurrentFeature is IShellFeatureActivation previous) previous.Deactivate();
        if (CurrentFeature is IShellProjectFeature previousProject) _projectCoordinator.Deactivate(previousProject);
        if (CurrentFeature is IQuickRun previousQuickRun) DeactivateQuickRun(previousQuickRun);

        var registration = _registry.Find(item.Id)
                           ?? throw new InvalidOperationException($"Feature '{item.Id}' is not registered.");
        if (!_featureViewModels.TryGetValue(item.Id, out var viewModel))
        {
            viewModel = registration.CreateViewModel();
            _featureViewModels.Add(item.Id, viewModel);
        }

        CurrentFeature = viewModel;
        ContentHorizontalScrollBarVisibility = registration.HorizontalScrollBarVisibility;
        ContentVerticalScrollBarVisibility = registration.VerticalScrollBarVisibility;
        HasProjectMenu = viewModel is IShellProjectFeature;
        ProjectMenuItems = CreateProjectMenuItems(viewModel);
        OnPropertyChanged(nameof(ProjectMenuItems));
        Header = item.DisplayName == "Get started"
            ? "Mapping Tools"
            : $"Mapping Tools - {item.DisplayName}";
        if (viewModel is IShellFeatureActivation current) current.Activate();
        if (viewModel is IShellProjectFeature projectFeature) _projectCoordinator.Activate(projectFeature);
        if (viewModel is IQuickRun quickRun) _quickRunRegistry.SelectCurrent(quickRun.OperationId);
    }

    private IReadOnlyList<ShellProjectMenuItem> CreateProjectMenuItems(ObservableObject viewModel)
    {
        if (viewModel is not IShellProjectFeature) return [];

        List<ShellProjectMenuItem> items =
        [
            new("_Save project", "Save tool settings to file.", SaveProjectCommand, MaterialIconKind.ContentSave),
            new("_Open project", "Load tool settings from file.", OpenProjectCommand, MaterialIconKind.Folder),
            new("_New project", "Load the default tool settings.", NewProjectCommand, MaterialIconKind.RocketLaunch),
        ];
        if (viewModel is IShellExtraProjectMenuFeature extra) items.AddRange(extra.ExtraProjectMenuItems);

        return items;
    }

    partial void OnSelectedFeatureChanged(ShellFeatureItemViewModel? value)
    {
        if (value is not null) Activate(value);
    }

    [RelayCommand]
    private Task OpenWebsiteAsync()
    {
        return OpenUriAsync(WebsiteUri, "website");
    }

    [RelayCommand]
    private Task CheckForUpdatesAsync()
    {
        return _updaterInteraction?.CheckForUpdatesAsync(
                   false,
                   true)
               ?? Task.CompletedTask;
    }

    internal Task CheckForUpdatesOnStartupAsync()
    {
        return _updaterInteraction?.CheckForUpdatesAsync(
                   true,
                   false)
               ?? Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenGitHubAsync()
    {
        return OpenUriAsync(GitHubUri, "source repository");
    }

    [RelayCommand]
    private Task OpenDonateAsync()
    {
        return OpenUriAsync(DonateUri, "donation page");
    }

    [RelayCommand]
    private async Task OpenAboutAsync()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        StringBuilder message = new();
        message.AppendLine($"Mapping Tools {version}");
        message.AppendLine();
        message.AppendLine("Made by:");
        message.AppendLine("OliBomby");
        message.AppendLine();
        message.AppendLine("Supporters:");
        message.AppendLine("Mercury");
        message.AppendLine("Ryuusei Aika");
        message.AppendLine("Pon -");
        message.AppendLine("Spoppyboi");
        message.AppendLine("fanzhen0019");
        message.AppendLine("spon");
        message.AppendLine("Joshua Saku");
        message.AppendLine("Julaaaan");
        message.AppendLine("pizzafanboy");
        message.AppendLine("ZEduards");
        message.AppendLine("Dcs");
        message.AppendLine();
        message.AppendLine("Contributors:");
        message.AppendLine("Potoofu");
        message.AppendLine("Karoo13");
        message.AppendLine("Coppertine");
        message.Append("JPK314");

        await _dialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
            "Info",
            message.ToString(),
            [new DialogChoice<bool>("OK", true, true, true)],
            true));
    }

    [RelayCommand]
    private Task BetterSaveAsync()
    {
        return _betterSave.ExecuteAsync();
    }

    private bool CanUseProjectActions()
    {
        return CurrentFeature is IShellProjectFeature;
    }

    private void DeactivateQuickRun(IQuickRun quickRun)
    {
        if (_quickRunRegistry.CurrentCommandId == quickRun.OperationId) _quickRunRegistry.SelectCurrent(null);
    }

    [RelayCommand(CanExecute = nameof(CanUseProjectActions))]
    private Task SaveProjectAsync()
    {
        return CurrentFeature is IShellProjectFeature feature
            ? _projectCoordinator.SaveAsync(feature)
            : Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanUseProjectActions))]
    private Task OpenProjectAsync()
    {
        return CurrentFeature is IShellProjectFeature feature
            ? _projectCoordinator.OpenAsync(feature)
            : Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanUseProjectActions))]
    private Task NewProjectAsync()
    {
        return CurrentFeature is IShellProjectFeature feature
            ? _projectCoordinator.NewAsync(feature)
            : Task.CompletedTask;
    }

    private void ToggleFavorite(ShellFeatureItemViewModel item)
    {
        item.IsFavorite = !item.IsFavorite;
        _settings.FavoriteTools.RemoveAll(id => id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
        if (item.IsFavorite) _settings.FavoriteTools.Add(item.Id);

        RefreshVisibleFeatures();
    }

    private void RefreshVisibleFeatures()
    {
        var matches = FeatureItems
            .Where(MatchesSearch)
            .ToArray();
        var foundational = matches
            .Where(item => !item.Category.Equals("Tools", StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Order)
            .ToArray();
        var favorites = matches
            .Where(item =>
                item.Category.Equals("Tools", StringComparison.OrdinalIgnoreCase) && item.IsFavorite)
            .OrderBy(item => item.Order)
            .ToArray();
        var tools = matches
            .Where(item =>
                item.Category.Equals("Tools", StringComparison.OrdinalIgnoreCase) && !item.IsFavorite)
            .OrderBy(item => item.Order)
            .ToArray();

        VisibleFeatures.Clear();
        foreach (var item in foundational.Concat(favorites).Concat(tools)) VisibleFeatures.Add(item);

        NavigationEntries.Clear();
        AddNavigationSection(foundational, false);
        AddNavigationSection(favorites, foundational.Length > 0);
        AddNavigationSection(tools, foundational.Length + favorites.Length > 0);

        if (HighlightedFeature is null || !VisibleFeatures.Contains(HighlightedFeature)) HighlightedFeature = VisibleFeatures.FirstOrDefault();
    }

    private bool MatchesSearch(ShellFeatureItemViewModel item)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        return item.SearchableText.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase);
    }

    internal void MoveHighlightedFeature(int offset)
    {
        if (VisibleFeatures.Count == 0) return;

        int currentIndex = HighlightedFeature is null
            ? -1
            : VisibleFeatures.IndexOf(HighlightedFeature);
        int nextIndex = Math.Clamp(currentIndex + offset, 0, VisibleFeatures.Count - 1);
        HighlightedFeature = VisibleFeatures[nextIndex];
    }

    [RelayCommand]
    internal void ActivateHighlightedFeature()
    {
        HighlightedFeature?.ActivateCommand.Execute(null);
    }

    [RelayCommand]
    private void SelectPreviousFeature()
    {
        MoveHighlightedFeature(-1);
    }

    [RelayCommand]
    private void SelectNextFeature()
    {
        MoveHighlightedFeature(1);
    }

    private void AddNavigationSection(
        IEnumerable<ShellFeatureItemViewModel> section,
        bool includeDivider)
    {
        var items = section.ToArray();
        if (items.Length == 0) return;

        if (includeDivider) NavigationEntries.Add(new NavigationDividerViewModel());

        foreach (var item in items) NavigationEntries.Add(item);
    }

    private void OnNotificationPublished(
        object? sender,
        UserNotificationPublishedEventArgs eventArgs)
    {
        _dispatcher.Post(() =>
            NotificationQueue.Add(new ShellNotificationViewModel(
                eventArgs.Notification,
                RemoveNotification)));
    }

    private void RemoveNotification(ShellNotificationViewModel notification)
    {
        NotificationQueue.Remove(notification);
    }

    private async Task OpenUriAsync(Uri uri, string destination)
    {
        bool accepted = await _launcher.OpenUriAsync(uri).ConfigureAwait(false);
        if (!accepted)
            await _notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Warning,
                "Could not open link",
                $"The {destination} could not be opened by the operating system.")).ConfigureAwait(false);
    }
}
