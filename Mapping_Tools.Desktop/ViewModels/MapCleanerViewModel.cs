using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
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
    private readonly IDialogService _dialogs;
    private readonly IUserNotificationService _notifications;
    private readonly IPlatformLauncher _launcher;
    private readonly ProjectDefinition<MapCleanerProject> _definition = new(
        "mapcleanerproject.json",
        "Map Cleaner Projects",
        () => new MapCleanerProject());
    private bool _installing;
    private bool _loadedAutosave;

    [ObservableProperty] private bool _volumeSliders = true;
    [ObservableProperty] private bool _sampleSetSliders = true;
    [ObservableProperty] private bool _volumeSpinners = true;
    [ObservableProperty] private bool _resnapObjects = true;
    [ObservableProperty] private bool _resnapBookmarks;
    [ObservableProperty] private bool _analyzeSamples = true;
    [ObservableProperty] private bool _removeUnusedSamples;
    [ObservableProperty] private bool _removeHitsounds;
    [ObservableProperty] private bool _removeMuting;
    [ObservableProperty] private bool _removeUnclickableHitsounds;
    [ObservableProperty] private string _beatDivisorsText = "1/16, 1/12";
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string _resultSummary = "Run Map Cleaner to rebuild useful greenlines.";
    [ObservableProperty] private IReadOnlyList<TimelineMarker> _markers = [];
    [ObservableProperty] private double _endTime = 20;

    public MapCleanerViewModel(
        IMapCleanerService cleaner,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace,
        ICurrentBeatmapLocator currentBeatmap,
        ApplicationSettings settings,
        IQuickRunCommandRegistry quickRunRegistry,
        IProjectService projects,
        IDialogService dialogs,
        IUserNotificationService notifications,
        IPlatformLauncher launcher)
    {
        _cleaner = cleaner;
        _execution = execution;
        _workspace = workspace;
        _currentBeatmap = currentBeatmap;
        _settings = settings;
        _quickRunRegistry = quickRunRegistry;
        _projects = projects;
        _dialogs = dialogs;
        _notifications = notifications;
        _launcher = launcher;
    }

    public void Activate()
    {
        _quickRunRegistry.SelectCurrent(OperationId);
        if (!_loadedAutosave)
        {
            _loadedAutosave = true;
            _ = LoadAutosaveAsync();
        }
    }

    public void Deactivate()
    {
        if (_quickRunRegistry.CurrentCommandId == OperationId) _quickRunRegistry.SelectCurrent(null);
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
        await RunPathsAsync(_workspace.SelectedPaths, quick: false, CancellationToken.None);
    }

    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await _currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        await RunPathsAsync(string.IsNullOrWhiteSpace(path) ? [] : [path], true, cancellationToken);
    }

    [RelayCommand] private void Cancel() => _execution.Cancel(OperationId);
    [RelayCommand] private Task NavigateAsync(double time) =>
        _launcher.OpenUriAsync(new Uri($"osu://edit/{Math.Round(time)}"));
    private bool CanRun() => !IsRunning;

    public async Task SaveProjectAsync(CancellationToken cancellationToken = default)
    {
        if (await _projects.SaveAsAsync(_definition, Snapshot(), "map-cleaner-project.json", cancellationToken) is not null)
            IsDirty = false;
    }

    public async Task OpenProjectAsync(CancellationToken cancellationToken = default)
    {
        if (!await ConfirmDiscardAsync(cancellationToken)) return;
        ProjectOpenResult<MapCleanerProject>? opened = await _projects.OpenAsync(_definition, cancellationToken);
        if (opened is not null) Install(opened.Project);
    }

    public async Task NewProjectAsync(CancellationToken cancellationToken = default)
    {
        if (await ConfirmDiscardAsync(cancellationToken)) Install(_projects.CreateNew(_definition));
    }

    private async Task RunPathsAsync(IReadOnlyList<string> paths, bool quick, CancellationToken cancellationToken)
    {
        if (paths.Count == 0)
        {
            ResultSummary = "Select at least one beatmap or open one in osu! before running Map Cleaner.";
            return;
        }
        MapCleanerOptions options;
        try { options = Snapshot().MapCleanerArgs; }
        catch (FormatException exception) { ResultSummary = exception.Message; return; }
        IsRunning = true;
        Progress = 0;
        try
        {
            ToolExecutionResult<MapCleanerResult> execution = await _execution.ExecuteAsync(
                new ToolExecutionRequest<MapCleanerResult>(OperationId, "Map Cleaner", async context =>
                {
                    MapCleanerResult result = await _cleaner.CleanAsync(
                        paths, options,
                        new Progress<double>(value => context.ReportProgress(value, "Cleaning beatmaps")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<MapCleanerResult>(
                        result,
                        quick ? null : Summarize(result, options),
                        reloadEditor: true);
                }),
                new Progress<ToolExecutionProgress>(value => Progress = value.Percent),
                cancellationToken);
            if (execution.Status == ToolExecutionStatus.Succeeded && execution.Value is { } result)
            {
                ResultSummary = Summarize(result, options);
                EndTime = result.TimelineEndTime;
                Markers = paths.Count == 1 ? CreateMarkers(result) : [];
            }
        }
        finally { IsRunning = false; }
    }

    private MapCleanerProject Snapshot() => new() { MapCleanerArgs = new MapCleanerOptions
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
        BeatDivisors = ParseDivisors(BeatDivisorsText)
    }};

    private void Install(MapCleanerProject project)
    {
        MapCleanerOptions options = project?.MapCleanerArgs ?? throw new InvalidDataException("Map Cleaner project is incomplete.");
        _installing = true;
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
        BeatDivisorsText = string.Join(", ", options.BeatDivisors.Select(value => value.ToString()));
        _installing = false;
        IsDirty = false;
    }

    private async Task<bool> ConfirmDiscardAsync(CancellationToken cancellationToken) => !IsDirty ||
        await _dialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
            "Confirm new project", "All unsaved Map Cleaner changes will be lost. Continue?",
            [new DialogChoice<bool>("Continue", true, IsDefault: true), new DialogChoice<bool>("Cancel", false, IsCancel: true)], false), cancellationToken);

    private async Task LoadAutosaveAsync()
    {
        try { if (!IsDirty) Install(await _projects.LoadAsync<MapCleanerProject>(_projects.GetAutoSavePath(_definition))); }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        catch (Exception exception) { await PublishFailureAsync("Project could not be loaded", exception); }
    }

    private async Task AutoSaveSafelyAsync()
    {
        try { await _projects.AutoSaveAsync(_definition, Snapshot()); }
        catch (Exception exception) { await PublishFailureAsync("Project could not be saved", exception); }
    }

    private Task PublishFailureAsync(string message, Exception exception) =>
        _notifications.PublishAsync(new UserNotification(UserNotificationSeverity.Error, "Map Cleaner", message, exception));

    private static IBeatDivisor[] ParseDivisors(string text)
    {
        string[] parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) throw new FormatException("Enter at least one beat divisor.");
        return parts.Select(part =>
        {
            string[] fraction = part.Split('/', StringSplitOptions.TrimEntries);
            if (fraction.Length != 2 || !int.TryParse(fraction[0], out int numerator) ||
                !int.TryParse(fraction[1], out int denominator) || numerator <= 0 || denominator <= 0)
                throw new FormatException($"Beat divisor '{part}' must use positive numerator/denominator notation.");
            return (IBeatDivisor)new RationalBeatDivisor(numerator, denominator);
        }).ToArray();
    }

    private static IReadOnlyList<TimelineMarker> CreateMarkers(MapCleanerResult result) =>
        result.TimingPointsAdded.Select(time => new TimelineMarker(time, TimelineMarkerKind.Added, "Greenline added"))
            .Concat(result.TimingPointsChanged.Select(time => new TimelineMarker(time, TimelineMarkerKind.Changed, "Greenline changed")))
            .Concat(result.TimingPointsRemovedAt.Select(time => new TimelineMarker(time, TimelineMarkerKind.Removed, "Greenline removed")))
            .OrderBy(marker => marker.Time).ToArray();

    private static string Summarize(MapCleanerResult result, MapCleanerOptions options) =>
        $"Successfully {(result.TimingPointsRemoved < 0 ? "added" : "removed")} {Math.Abs(result.TimingPointsRemoved)} {(Math.Abs(result.TimingPointsRemoved) == 1 ? "greenline" : "greenlines")}" +
        (options.ResnapObjects ? $" and resnapped {result.ObjectsResnapped} {(result.ObjectsResnapped == 1 ? "object" : "objects")}" : "") +
        (options.RemoveUnusedSamples ? $" and removed {result.SamplesRemoved} unused {(result.SamplesRemoved == 1 ? "sample" : "samples")}" : "") + "!";

    partial void OnVolumeSlidersChanged(bool value) => MarkDirty();
    partial void OnSampleSetSlidersChanged(bool value) => MarkDirty();
    partial void OnVolumeSpinnersChanged(bool value) => MarkDirty();
    partial void OnResnapObjectsChanged(bool value) => MarkDirty();
    partial void OnResnapBookmarksChanged(bool value) => MarkDirty();
    partial void OnAnalyzeSamplesChanged(bool value) => MarkDirty();
    partial void OnRemoveUnusedSamplesChanged(bool value) => MarkDirty();
    partial void OnRemoveHitsoundsChanged(bool value) => MarkDirty();
    partial void OnRemoveMutingChanged(bool value) => MarkDirty();
    partial void OnRemoveUnclickableHitsoundsChanged(bool value) => MarkDirty();
    partial void OnBeatDivisorsTextChanged(string value) => MarkDirty();
    private void MarkDirty() { if (!_installing) IsDirty = true; }
}
