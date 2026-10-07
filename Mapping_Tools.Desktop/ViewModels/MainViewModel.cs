using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Application.Localization;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Globalization;
using System.Text;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Migration.Contracts;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Services.Updates;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Shell;
using Material.Icons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Coordinates explicit feature discovery, navigation, favorites, activation,
///     and shell-level commands.
/// </summary>
public sealed partial class MainViewModel : LocalizedObservableObject, IDisposable, IAsyncDisposable
{
    private static readonly Uri websiteUri = new("https://mappingtools.github.io");
    private static readonly Uri gitHubUri = new("https://github.com/OliBomby/Mapping_Tools");
    private static readonly Uri issuesUri = new("https://github.com/OliBomby/Mapping_Tools/issues");
    private static readonly Uri donateUri = new("https://ko-fi.com/olibomby");
    private readonly IBetterSaveService betterSave;
    private readonly ILogger<MainViewModel> logger;
    private readonly IDialogService dialogs;
    private readonly IUiDispatcher dispatcher;

    private readonly Dictionary<string, ObservableObject> featureViewModels =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly IPlatformLauncher launcher;
    private readonly IApplicationDataMigrationService? migrationService;
    private readonly IUserNotificationService notifications;
    private readonly ProjectAutosaveCoordinator projectCoordinator;
    private readonly IQuickRunCommandRegistry quickRunRegistry;
    private readonly IShellFeatureRegistry registry;
    private readonly DesktopApplicationSettings settings;
    private readonly IUpdaterInteractionService? updaterInteraction;
    private CancellationTokenSource? featureActivationCancellation;
    private bool featureActivationReady;
    private bool featureActivationStarted;
    private long featureActivationVersion;
    private Task? shutdownTask;

    /// <summary>
    ///     Creates the desktop shell and prepares the first explicit registration for activation.
    /// </summary>
    /// <param name="registry">Supplies explicitly registered features in navigation order.</param>
    /// <param name="quickRunRegistry">Tracks the command for the active QuickRun-capable feature.</param>
    /// <param name="settings">Owns persisted favorites and other shared preferences.</param>
    /// <param name="notifications">Supplies the process-lifetime user notification stream.</param>
    /// <param name="launcher">Opens support links through the operating system.</param>
    /// <param name="workspace">Presents current-map and safety-copy actions in shell chrome.</param>
    /// <param name="betterSave">Saves the current live editor state through the shared safety gateway.</param>
    /// <param name="dialogs">Presents shell-owned information dialogs.</param>
    /// <param name="projectCoordinator">Owns project menus and feature autosave lifecycle.</param>
    /// <param name="dispatcher">Schedules deferred UI work after the shell can render its loading state.</param>
    /// <param name="updaterInteraction">
    ///     Shows update decisions and owns update shutdown interaction when supplied by runtime
    ///     composition.
    /// </param>
    /// <param name="migrationService">Provides the result of migration completed during settings loading.</param>
    /// <param name="logger">Records shell actions, run settings, and activation failures.</param>
    public MainViewModel(
        IShellFeatureRegistry registry,
        IQuickRunCommandRegistry quickRunRegistry,
        DesktopApplicationSettings settings,
        IUserNotificationService notifications,
        IPlatformLauncher launcher,
        BeatmapWorkspaceViewModel workspace,
        IBetterSaveService betterSave,
        IDialogService dialogs,
        ProjectAutosaveCoordinator projectCoordinator,
        IUiDispatcher dispatcher,
        IUpdaterInteractionService? updaterInteraction = null,
        IApplicationDataMigrationService? migrationService = null,
        ILogger<MainViewModel>? logger = null)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.quickRunRegistry = quickRunRegistry ?? throw new ArgumentNullException(nameof(quickRunRegistry));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.betterSave = betterSave ?? throw new ArgumentNullException(nameof(betterSave));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.projectCoordinator = projectCoordinator ?? throw new ArgumentNullException(nameof(projectCoordinator));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.updaterInteraction = updaterInteraction;
        this.migrationService = migrationService;
        this.logger = logger ?? NullLogger<MainViewModel>.Instance;

        FeatureItems = registry.Features
            .Select(registration => new ShellFeatureItemViewModel(
                registration,
                settings.FavoriteTools.Contains(registration.Id, StringComparer.OrdinalIgnoreCase),
                item => SelectedFeature = item,
                ToggleFavorite))
            .ToArray();
        VisibleFeatures = [];
        NavigationEntries = [];
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

    /// <summary>Gets current-map and backup actions shared by every shell feature.</summary>
    public BeatmapWorkspaceViewModel Workspace { get; }

    /// <summary>Gets the active feature's standard and additional project-menu commands.</summary>
    public IReadOnlyList<ShellProjectMenuItem> ProjectMenuItems { get; private set; } = [];

    /// <summary>Gets or sets the case-insensitive feature search query.</summary>
    public string SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                logger.LogInformation("User searched features: {Query}", value);
                RefreshVisibleFeatures();
            }
        }
    } = string.Empty;

    /// <summary>Gets the currently activated feature presentation model.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    [NotifyCanExecuteChangedFor(nameof(RedoCommand))]
    [NotifyPropertyChangedFor(nameof(ProjectHistory))]
    public partial ObservableObject? CurrentFeature { get; private set; }

    /// <summary>Gets the active feature's edit history, or null when the feature has no history.</summary>
    public IProjectUndoHistory? ProjectHistory => (CurrentFeature as IShellUndoFeature)?.UndoHistory;

    /// <summary>Gets whether the selected feature is currently being prepared.</summary>
    [ObservableProperty]
    public partial bool IsFeatureLoading { get; private set; } = true;

    /// <summary>Gets the selected feature loading error, if preparation failed.</summary>
    [ObservableProperty]
    public partial string? FeatureLoadError { get; private set; }

    /// <summary>Gets the original diagnostic details for a failed feature activation.</summary>
    [ObservableProperty]
    public partial string? FeatureLoadDetails { get; private set; }

    private Exception? featureLoadException;

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

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (shutdownTask is null) shutdownTask = DisposeCoreAsync();

        return new ValueTask(shutdownTask);
    }

    /// <summary>
    ///     Saves recovery snapshots for all instantiated project features and
    ///     deactivates the current feature during application shutdown.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private async Task DisposeCoreAsync()
    {
        logger.LogInformation("Shell shutdown started; {FeatureCount} features were instantiated", featureViewModels.Count);
        if (featureActivationCancellation is not null)
            await featureActivationCancellation.CancelAsync();

        var saveTasks = featureViewModels.Values
            .OfType<IShellProjectFeature>()
            .Select(projectCoordinator.SaveOnShutdown)
            .ToArray();

        try
        {
            await Task.WhenAll(saveTasks);
        }
        finally
        {
            if (ProjectHistory is { } history)
                history.Changed -= OnUndoHistoryChanged;
            if (CurrentFeature is IQuickRun) DeactivateQuickRun();
            if (CurrentFeature is IShellFeatureActivation activation) activation.Deactivate();
            ProjectMenuItems = [];
            OnPropertyChanged(nameof(ProjectMenuItems));
            logger.LogInformation("Shell shutdown completed");
        }
    }

    /// <summary>Prevents project recovery snapshots from being written during the current shutdown.</summary>
    public void SuppressProjectAutosave()
    {
        projectCoordinator.SuppressSave();
    }

    internal async Task InitializeAsync()
    {
        if (featureActivationStarted) return;

        featureActivationStarted = true;
        logger.LogInformation("Shell initialization started with {FeatureCount} registered features", FeatureItems.Count);
        await ReportLegacyDataMigrationAsync();
        featureActivationReady = true;
        if (SelectedFeature is not null) await ActivateAsync(SelectedFeature);
    }

    private async Task ReportLegacyDataMigrationAsync()
    {
        if (migrationService?.LastMigrationResult is not { } result) return;

        logger.LogInformation("Migrated legacy data: {Autosaves} autosaves, {Projects} projects", result.AutosavesCopied, result.ProjectFilesCopied);
        await notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Success,
            DesktopStrings.Shell_LegacyDataMigrated,
            ApplicationText.Format(DesktopStrings.Shell_MigrationSummary, result.AutosavesCopied, result.ProjectFilesCopied)));
    }

    private async Task ActivateAsync(ShellFeatureItemViewModel item)
    {
        logger.LogInformation("Feature activation requested: {FeatureId}", item.Id);
        if (featureActivationCancellation is not null)
            await featureActivationCancellation.CancelAsync();

        CancellationTokenSource cancellation = new();
        featureActivationCancellation = cancellation;
        long activationVersion = ++featureActivationVersion;

        HighlightedFeature = item;

        foreach (var featureItem in FeatureItems) featureItem.IsActive = ReferenceEquals(featureItem, item);

        if (CurrentFeature is IShellFeatureActivation previous) previous.Deactivate();
        if (CurrentFeature is IQuickRun) DeactivateQuickRun();

        var registration = registry.Find(item.Id)
                           ?? throw new InvalidOperationException($"Feature '{item.Id}' is not registered.");
        CurrentFeature = null;
        ContentHorizontalScrollBarVisibility = registration.HorizontalScrollBarVisibility;
        ContentVerticalScrollBarVisibility = registration.VerticalScrollBarVisibility;
        Header = item.Id == "get-started"
            ? "Mapping Tools"
            : $"Mapping Tools - {item.DisplayName}";
        FeatureLoadError = null;
        featureLoadException = null;
        FeatureLoadDetails = null;
        IsFeatureLoading = true;

        try
        {
            if (!featureViewModels.TryGetValue(item.Id, out var viewModel))
            {
                // Let the shell paint its loading state before running synchronous constructors.
                TaskCompletionSource<bool> renderCompletion = new();
                dispatcher.PostBackground(() => renderCompletion.TrySetResult(true));
                await renderCompletion.Task;
                cancellation.Token.ThrowIfCancellationRequested();

                viewModel = registration.CreateViewModel();
                featureViewModels.Add(item.Id, viewModel);
                if (viewModel is SingleRunToolViewModel runTool)
                    runTool.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(SingleRunToolViewModel.IsRunning)
                            && runTool.IsRunning
                            && ReferenceEquals(CurrentFeature, runTool))
                            LogFeatureSettings(item.Id, runTool);
                    };
            }

            if (activationVersion != featureActivationVersion || !ReferenceEquals(SelectedFeature, item)) return;

            if (viewModel is IShellProjectFeature projectFeature)
                await projectCoordinator.ActivateAsync(projectFeature);

            cancellation.Token.ThrowIfCancellationRequested();
            if (activationVersion != featureActivationVersion || !ReferenceEquals(SelectedFeature, item)) return;

            CurrentFeature = viewModel;
            HasProjectMenu = viewModel is IShellProjectFeature;
            ProjectMenuItems = CreateProjectMenuItems(viewModel);
            OnPropertyChanged(nameof(ProjectMenuItems));
            if (viewModel is IShellFeatureActivation current) current.Activate();
            if (viewModel is IQuickRun) quickRunRegistry.SelectCurrent(registration.Id);
            logger.LogInformation("Feature activated: {FeatureId}, view model {ViewModelType}", item.Id, viewModel.GetType().Name);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Feature activation cancelled: {FeatureId}", item.Id);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Feature activation failed: {FeatureId}", item.Id);
            if (activationVersion == featureActivationVersion && ReferenceEquals(SelectedFeature, item))
            {
                featureLoadException = exception;
                FeatureLoadDetails = exception.ToString();
                FeatureLoadError = ApplicationExceptionText.GetSummary(exception);
                ClearProjectMenu();
            }
        }
        finally
        {
            if (activationVersion == featureActivationVersion) IsFeatureLoading = false;
            if (ReferenceEquals(featureActivationCancellation, cancellation)) featureActivationCancellation = null;
            cancellation.Dispose();
        }
    }

    private IReadOnlyList<ShellProjectMenuItem> CreateProjectMenuItems(ObservableObject viewModel)
    {
        if (viewModel is not IShellProjectFeature) return [];

        List<ShellProjectMenuItem> items =
        [
            new(DesktopStrings.Shell_MenuUndo, DesktopStrings.Shell_UndoTheLastEditCtrlZ, UndoCommand, MaterialIconKind.Undo),
            new(DesktopStrings.Shell_MenuRedo, DesktopStrings.Shell_RedoTheLastUndoneEditCtrlY, RedoCommand, MaterialIconKind.Redo),
            new(DesktopStrings.Shell_MenuSaveProject, DesktopStrings.Shell_SaveToolSettingsToFile, SaveProjectCommand, MaterialIconKind.ContentSave),
            new(DesktopStrings.Shell_MenuOpenProject, DesktopStrings.Shell_LoadToolSettingsFromFile, OpenProjectCommand, MaterialIconKind.Folder),
            new(DesktopStrings.Shell_MenuNewProject, DesktopStrings.Shell_LoadTheDefaultToolSettings, NewProjectCommand, MaterialIconKind.RocketLaunch),
        ];
        if (viewModel is IShellExtraProjectMenuFeature extra) items.AddRange(extra.ExtraProjectMenuItems);

        return items;
    }

    /// <inheritdoc />
    protected override void RefreshLocalizedProperties()
    {
        base.RefreshLocalizedProperties();
        if (featureLoadException is { } exception) FeatureLoadError = ApplicationExceptionText.GetSummary(exception);
        RefreshVisibleFeatures();
        if (SelectedFeature is { } feature)
            Header = feature.Id == "get-started" ? "Mapping Tools" : $"Mapping Tools - {feature.DisplayName}";
        if (CurrentFeature is { } viewModel)
        {
            ProjectMenuItems = CreateProjectMenuItems(viewModel);
            OnPropertyChanged(nameof(ProjectMenuItems));
        }
    }

    private void ClearProjectMenu()
    {
        HasProjectMenu = false;
        ProjectMenuItems = [];
        OnPropertyChanged(nameof(ProjectMenuItems));
    }

    partial void OnSelectedFeatureChanged(ShellFeatureItemViewModel? value)
    {
        if (value is not null) logger.LogInformation("User selected feature {FeatureId}", value.Id);
        if (value is not null && featureActivationReady) _ = ActivateAsync(value);
    }

    partial void OnIsNavigationOpenChanged(bool value)
    {
        logger.LogInformation("Navigation pane open: {IsOpen}", value);
    }

    [RelayCommand]
    private Task OpenWebsiteAsync()
    {
        return OpenUriAsync(websiteUri, DesktopStrings.Shell_Website);
    }

    [RelayCommand]
    private Task CheckForUpdatesAsync()
    {
        logger.LogInformation("User requested update check");
        return updaterInteraction?.CheckForUpdatesAsync(
                   false,
                   true)
               ?? Task.CompletedTask;
    }

    internal Task CheckForUpdatesOnStartupAsync()
    {
        return updaterInteraction?.CheckForUpdatesAsync(
                   true,
                   false)
               ?? Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenGitHubAsync()
    {
        return OpenUriAsync(gitHubUri, DesktopStrings.Shell_SourceRepository);
    }

    [RelayCommand]
    private Task OpenIssuesAsync()
    {
        return OpenUriAsync(issuesUri, DesktopStrings.Shell_IssueTracker);
    }

    [RelayCommand]
    private Task OpenDonateAsync()
    {
        return OpenUriAsync(donateUri, DesktopStrings.Shell_DonationPage);
    }

    [RelayCommand]
    private async Task OpenAboutAsync()
    {
        logger.LogInformation("User opened About dialog");
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        StringBuilder message = new();
        message.AppendLine($"Mapping Tools {version}");
        message.AppendLine();
        message.AppendLine(DesktopStrings.Shell_MadeBy);
        message.AppendLine("OliBomby");
        message.AppendLine();
        message.AppendLine(DesktopStrings.Shell_Supporters);
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
        message.AppendLine("Omekyu");
        message.AppendLine("downpour");
        message.AppendLine("ZyMaa");
        message.AppendLine("phazzi");
        message.AppendLine("JeffFuchsional");
        message.AppendLine("onlyatroller");
        message.AppendLine("GreatMCGamer");
        message.AppendLine();
        message.AppendLine(DesktopStrings.Shell_Contributors);
        message.AppendLine("Potoofu");
        message.AppendLine("Karoo13");
        message.AppendLine("Coppertine");
        message.Append("JPK314");

        await dialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
            DesktopStrings.Shell_Info,
            message.ToString(),
            [new DialogChoice<bool>(DesktopStrings.Shell_UpperOk, true, true, true)],
            true));
    }

    [RelayCommand]
    private Task BetterSaveAsync()
    {
        logger.LogInformation("User requested Better Save");
        return betterSave.ExecuteAsync();
    }

    private bool CanUseProjectActions()
    {
        return CurrentFeature is IShellProjectFeature;
    }

    private bool CanUndo()
    {
        return ProjectHistory?.CanUndo == true;
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task UndoAsync()
    {
        logger.LogInformation("User requested Undo for {FeatureId}", SelectedFeature?.Id);
        try
        {
            ProjectHistory?.Undo();
        }
        catch (Exception exception)
        {
            await notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Error, DesktopStrings.Shell_CouldNotUndo, ApplicationExceptionText.GetSummary(exception), exception));
        }
    }

    private bool CanRedo()
    {
        return ProjectHistory?.CanRedo == true;
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private async Task RedoAsync()
    {
        logger.LogInformation("User requested Redo for {FeatureId}", SelectedFeature?.Id);
        try
        {
            ProjectHistory?.Redo();
        }
        catch (Exception exception)
        {
            await notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Error, DesktopStrings.Shell_CouldNotRedo, ApplicationExceptionText.GetSummary(exception), exception));
        }
    }

    partial void OnCurrentFeatureChanged(ObservableObject? oldValue, ObservableObject? newValue)
    {
        if (oldValue is IShellUndoFeature { UndoHistory: { } previous })
            previous.Changed -= OnUndoHistoryChanged;

        if (newValue is IShellUndoFeature { UndoHistory: { } current })
            current.Changed += OnUndoHistoryChanged;
    }

    private void OnUndoHistoryChanged(object? sender, EventArgs eventArgs)
    {
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void DeactivateQuickRun()
    {
        if (quickRunRegistry.CurrentCommandId is not null) quickRunRegistry.SelectCurrent(null);
    }

    [RelayCommand(CanExecute = nameof(CanUseProjectActions))]
    private Task SaveProjectAsync()
    {
        logger.LogInformation("User requested Save project for {FeatureId}", SelectedFeature?.Id);
        return CurrentFeature is IShellProjectFeature feature
            ? projectCoordinator.SaveAsync(feature)
            : Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanUseProjectActions))]
    private Task OpenProjectAsync()
    {
        logger.LogInformation("User requested Open project for {FeatureId}", SelectedFeature?.Id);
        return CurrentFeature is IShellProjectFeature feature
            ? projectCoordinator.OpenAsync(feature)
            : Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanUseProjectActions))]
    private Task NewProjectAsync()
    {
        logger.LogInformation("User requested New project for {FeatureId}", SelectedFeature?.Id);
        return CurrentFeature is IShellProjectFeature feature
            ? projectCoordinator.NewAsync(feature)
            : Task.CompletedTask;
    }

    private void ToggleFavorite(ShellFeatureItemViewModel item)
    {
        item.IsFavorite = !item.IsFavorite;
        settings.FavoriteTools.RemoveAll(id => id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
        if (item.IsFavorite) settings.FavoriteTools.Add(item.Id);
        logger.LogInformation("User set favorite for {FeatureId} to {IsFavorite}", item.Id, item.IsFavorite);

        RefreshVisibleFeatures();
    }

    private void RefreshVisibleFeatures()
    {
        var matches = FeatureItems
            .Where(MatchesSearch)
            .ToArray();
        var foundational = matches
            .Where(item => !item.Category.Equals("Tools", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var favorites = matches
            .Where(item =>
                item.Category.Equals("Tools", StringComparison.OrdinalIgnoreCase) && item.IsFavorite)
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        var tools = matches
            .Where(item =>
                item.Category.Equals("Tools", StringComparison.OrdinalIgnoreCase) && !item.IsFavorite)
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
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

    private void LogFeatureSettings(string featureId, ObservableObject viewModel)
    {
        foreach (var property in viewModel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0
                || property.Name.EndsWith("Progress", StringComparison.Ordinal)
                || property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Secret", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase)) continue;

            Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type != typeof(string) && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal)) continue;

            try
            {
                string value = Convert.ToString(property.GetValue(viewModel), CultureInfo.InvariantCulture) ?? "<null>";
                logger.LogInformation("Run setting {FeatureId}.{Property}={Value}", featureId, property.Name,
                    value.Length <= 500 ? value : value[..500] + "…");
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Could not inspect run setting {FeatureId}.{Property}", featureId, property.Name);
            }
        }
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

    private async Task OpenUriAsync(Uri uri, string destination)
    {
        logger.LogInformation("User opened {Destination}: {Uri}", destination, uri);
        bool accepted = await launcher.OpenUriAsync(uri).ConfigureAwait(false);
        logger.LogInformation("Open {Destination} accepted by OS: {Accepted}", destination, accepted);
        if (!accepted)
            await notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Warning,
                DesktopStrings.Shell_CouldNotOpenLink,
                ApplicationText.Format(DesktopStrings.Shell_LinkOpenFailed, destination))).ConfigureAwait(false);
    }
}
