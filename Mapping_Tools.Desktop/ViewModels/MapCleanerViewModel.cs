using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Timeline;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Tools.MapCleaner;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Coordinates Map Cleaner options, projects, QuickRun, and timeline results.</summary>
public sealed partial class MapCleanerViewModel : ObservableObject,
    IShellFeatureActivation,
    IShellProjectFeature
{
    internal const string OperationId = "map-cleaner";
    private readonly IMapCleanerService _cleaner;
    private readonly IToolExecutionService _execution;
    private readonly IBeatmapWorkspace _workspace;
    private readonly ICurrentBeatmapLocator _currentBeatmap;
    private readonly ApplicationSettings _settings;
    private readonly IQuickRunCommandRegistry _quickRunRegistry;
    private readonly IProjectService _projects;
    private readonly IUserNotificationService _notifications;
    private readonly IPlatformLauncher _launcher;
    private readonly ProjectDefinition<MapCleanerProject> _definition = new(
        "mapcleanerproject.json",
        "Map Cleaner Projects",
        () => new MapCleanerProject());
    private bool _loadedAutosave;

    /// <summary>Gets or sets whether slider volume changes are preserved.</summary>
    [ObservableProperty]
    public partial bool VolumeSliders { get; set; } = true;

    /// <summary>Gets or sets whether slider sample-set changes are preserved.</summary>
    [ObservableProperty]
    public partial bool SampleSetSliders { get; set; } = true;

    /// <summary>Gets or sets whether spinner volume changes are preserved.</summary>
    [ObservableProperty]
    public partial bool VolumeSpinners { get; set; } = true;

    /// <summary>Gets or sets whether hit objects and slider ends are resnapped.</summary>
    [ObservableProperty]
    public partial bool ResnapObjects { get; set; } = true;

    /// <summary>Gets or sets whether editor bookmarks are resnapped.</summary>
    [ObservableProperty]
    public partial bool ResnapBookmarks { get; set; }

    /// <summary>Gets or sets whether mapset samples are inspected.</summary>
    [ObservableProperty]
    public partial bool AnalyzeSamples { get; set; } = true;

    /// <summary>Gets or sets whether unused samples are moved to recovery.</summary>
    [ObservableProperty]
    public partial bool RemoveUnusedSamples { get; set; }

    /// <summary>Gets or sets whether object hitsounds are removed.</summary>
    [ObservableProperty]
    public partial bool RemoveHitsounds { get; set; }

    /// <summary>Gets or sets whether muting values are removed from object ends.</summary>
    [ObservableProperty]
    public partial bool RemoveMuting { get; set; }

    /// <summary>Gets or sets whether unclickable slider and spinner ends are muted.</summary>
    [ObservableProperty]
    public partial bool RemoveUnclickableHitsounds { get; set; }

    /// <summary>Gets or sets the typed beat divisors used for resnapping.</summary>
    [ObservableProperty]
    public partial IBeatDivisor[] BeatDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors();

    /// <summary>Gets whether cleanup is currently running.</summary>
    [ObservableProperty]
    public partial bool IsRunning { get; private set; }

    /// <summary>Gets the current cleanup completion percentage.</summary>
    [ObservableProperty]
    public partial double Progress { get; private set; }

    /// <summary>Gets a textual summary of the latest cleanup.</summary>
    [ObservableProperty]
    public partial string ResultSummary { get; private set; } =
        "Run Map Cleaner to rebuild useful greenlines.";

    /// <summary>Gets the latest single-map cleanup markers.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<TimelineMarker> Markers { get; private set; } = [];

    /// <summary>Gets the final timestamp displayed by the cleanup timeline.</summary>
    [ObservableProperty]
    public partial double EndTime { get; private set; } = 20;

    /// <summary>Gets whether a successful cleanup has produced timeline state.</summary>
    [ObservableProperty]
    public partial bool HasRun { get; private set; }

    /// <summary>Creates a Map Cleaner presentation model.</summary>
    /// <param name="cleaner">Runs framework-independent cleanup operations.</param>
    /// <param name="execution">Coordinates cancellation, backup, and notifications.</param>
    /// <param name="workspace">Supplies selected beatmaps for ordinary runs.</param>
    /// <param name="currentBeatmap">Finds the beatmap open in osu! for QuickRun.</param>
    /// <param name="settings">Supplies shared execution preferences.</param>
    /// <param name="quickRunRegistry">Tracks the active QuickRun-capable tool.</param>
    /// <param name="projects">Loads, saves, and autosaves typed projects.</param>
    /// <param name="notifications">Publishes project persistence failures.</param>
    /// <param name="launcher">Navigates osu! to selected timeline markers.</param>
    public MapCleanerViewModel(
        IMapCleanerService cleaner,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace,
        ICurrentBeatmapLocator currentBeatmap,
        ApplicationSettings settings,
        IQuickRunCommandRegistry quickRunRegistry,
        IProjectService projects,
        IUserNotificationService notifications,
        IPlatformLauncher launcher)
    {
        _cleaner = cleaner ?? throw new ArgumentNullException(nameof(cleaner));
        _execution = execution ?? throw new ArgumentNullException(nameof(execution));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _quickRunRegistry = quickRunRegistry ??
            throw new ArgumentNullException(nameof(quickRunRegistry));
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
    }

    /// <summary>Selects this feature for QuickRun and restores its autosaved project once.</summary>
    public void Activate()
    {
        _quickRunRegistry.SelectCurrent(OperationId);
        if (!_loadedAutosave)
        {
            _loadedAutosave = true;
            _ = LoadAutosaveAsync();
        }
    }

    /// <summary>Clears QuickRun selection and schedules project autosave.</summary>
    public void Deactivate()
    {
        if (_quickRunRegistry.CurrentCommandId == OperationId)
        {
            _quickRunRegistry.SelectCurrent(null);
        }

        _ = AutoSaveSafelyAsync();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        if (_settings.AlwaysQuickRun)
        {
            await RunQuickAsync(CancellationToken.None);
            return;
        }
        await RunPathsAsync(
            _workspace.SelectedPaths,
            quick: false,
            CancellationToken.None);
    }

    /// <summary>Cleans the beatmap currently open in osu! through the QuickRun path.</summary>
    /// <param name="cancellationToken">Cancels beatmap discovery or cleanup.</param>
    /// <returns>A task that completes after QuickRun finishes.</returns>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await _currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        await RunPathsAsync(string.IsNullOrWhiteSpace(path) ? [] : [path], true, cancellationToken);
    }

    [RelayCommand]
    private void Cancel() => _execution.Cancel(OperationId);

    [RelayCommand]
    private Task NavigateAsync(double time) =>
        _launcher.OpenUriAsync(new Uri($"osu://edit/{Math.Round(time)}"));

    private bool CanRun() => !IsRunning;

    /// <summary>Prompts for a destination and saves the current Map Cleaner project.</summary>
    /// <param name="cancellationToken">Cancels the picker or save.</param>
    /// <returns>A task that completes after the save attempt.</returns>
    public async Task SaveProjectAsync(CancellationToken cancellationToken = default)
    {
        await _projects.SaveAsAsync(
            _definition,
            Snapshot(),
            "map-cleaner-project.json",
            cancellationToken);
    }

    /// <summary>Opens a Map Cleaner project selected by the user.</summary>
    /// <param name="cancellationToken">Cancels picking or loading.</param>
    /// <returns>A task that completes after the open attempt.</returns>
    public async Task OpenProjectAsync(CancellationToken cancellationToken = default)
    {
        ProjectOpenResult<MapCleanerProject>? opened = await _projects.OpenAsync(
            _definition,
            cancellationToken);
        if (opened is not null)
        {
            Install(opened.Project);
        }
    }

    /// <summary>Installs a new default Map Cleaner project.</summary>
    /// <param name="cancellationToken">Unused cancellation token retained by the project-feature contract.</param>
    /// <returns>A task that completes after the new-project attempt.</returns>
    public Task NewProjectAsync(CancellationToken cancellationToken = default)
    {
        Install(_projects.CreateNew(_definition));
        return Task.CompletedTask;
    }

    private async Task RunPathsAsync(IReadOnlyList<string> paths, bool quick, CancellationToken cancellationToken)
    {
        if (paths.Count == 0)
        {
            ResultSummary = "Select at least one beatmap or open one in osu! before running Map Cleaner.";
            return;
        }

        MapCleanerOptions options = Snapshot().MapCleanerArgs;

        IsRunning = true;
        Progress = 0;
        try
        {
            ToolExecutionResult<MapCleanerResult> execution = await _execution.ExecuteAsync(
                new ToolExecutionRequest<MapCleanerResult>(
                    OperationId,
                    "Map Cleaner",
                    async context =>
                    {
                        Progress<double> progress = new(value =>
                            context.ReportProgress(value, "Cleaning beatmaps"));
                        MapCleanerResult result = await _cleaner.CleanAsync(
                            paths,
                            options,
                            progress,
                            context.CancellationToken);
                        return new ToolExecutionOutput<MapCleanerResult>(
                            result,
                            quick ? null : Summarize(result, options),
                            reloadEditor: quick);
                    }),
                new Progress<ToolExecutionProgress>(value => Progress = value.Percent),
                cancellationToken);
            if (execution.Status == ToolExecutionStatus.Succeeded && execution.Value is { } result)
            {
                ResultSummary = Summarize(result, options);
                EndTime = result.TimelineEndTime;
                Markers = paths.Count == 1 ? CreateMarkers(result) : [];
                HasRun = true;
            }
        }
        finally
        {
            IsRunning = false;
        }
    }

    private MapCleanerProject Snapshot() => new()
    {
        MapCleanerArgs = new MapCleanerOptions
        {
            VolumeSliders = VolumeSliders,
            SampleSetSliders = SampleSetSliders,
            VolumeSpinners = VolumeSpinners,
            ResnapObjects = ResnapObjects,
            ResnapBookmarks = ResnapBookmarks,
            AnalyzeSamples = AnalyzeSamples,
            RemoveUnusedSamples = RemoveUnusedSamples,
            RemoveHitsounds = RemoveHitsounds,
            RemoveMuting = RemoveMuting,
            RemoveUnclickableHitsounds = RemoveUnclickableHitsounds,
            BeatDivisors = BeatDivisors.ToArray()
        }
    };

    private void Install(MapCleanerProject project)
    {
        MapCleanerOptions options = project?.MapCleanerArgs ??
            throw new InvalidDataException("Map Cleaner project is incomplete.");
        VolumeSliders = options.VolumeSliders;
        SampleSetSliders = options.SampleSetSliders;
        VolumeSpinners = options.VolumeSpinners;
        ResnapObjects = options.ResnapObjects;
        ResnapBookmarks = options.ResnapBookmarks;
        AnalyzeSamples = options.AnalyzeSamples;
        RemoveUnusedSamples = options.RemoveUnusedSamples;
        RemoveHitsounds = options.RemoveHitsounds;
        RemoveMuting = options.RemoveMuting;
        RemoveUnclickableHitsounds = options.RemoveUnclickableHitsounds;
        BeatDivisors = options.BeatDivisors.ToArray();
    }

    private async Task LoadAutosaveAsync()
    {
        try
        {
            MapCleanerProject project = await _projects.LoadAsync<MapCleanerProject>(
                _projects.GetAutoSavePath(_definition));
            Install(project);
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync("Project could not be loaded", exception);
        }
    }

    private async Task AutoSaveSafelyAsync()
    {
        try
        {
            await _projects.AutoSaveAsync(_definition, Snapshot());
        }
        catch (Exception exception)
        {
            await PublishFailureAsync("Project could not be saved", exception);
        }
    }

    private Task PublishFailureAsync(string message, Exception exception) =>
        _notifications.PublishAsync(new UserNotification(UserNotificationSeverity.Error, "Map Cleaner", message, exception));

    private static IReadOnlyList<TimelineMarker> CreateMarkers(MapCleanerResult result) =>
        result.TimingPointsAdded
            .Select(time => new TimelineMarker(
                time,
                TimelineMarkerKind.Added,
                "Greenline added"))
            .Concat(result.TimingPointsChanged.Select(time => new TimelineMarker(
                time,
                TimelineMarkerKind.Changed,
                "Greenline changed")))
            .Concat(result.TimingPointsRemovedAt.Select(time => new TimelineMarker(
                time,
                TimelineMarkerKind.Removed,
                "Greenline removed")))
            .OrderBy(marker => marker.Time)
            .ToArray();

    private static string Summarize(MapCleanerResult result, MapCleanerOptions options) =>
        $"Successfully {(result.TimingPointsRemoved < 0 ? "added" : "removed")} " +
        $"{Math.Abs(result.TimingPointsRemoved)} " +
        $"{(Math.Abs(result.TimingPointsRemoved) == 1 ? "greenline" : "greenlines")}" +
        (options.ResnapObjects
            ? $" and resnapped {result.ObjectsResnapped} " +
              $"{(result.ObjectsResnapped == 1 ? "object" : "objects")}"
            : string.Empty) +
        (options.RemoveUnusedSamples
            ? $" and removed {result.SamplesRemoved} unused " +
              $"{(result.SamplesRemoved == 1 ? "sample" : "samples")}"
            : string.Empty) +
        "!";

}
