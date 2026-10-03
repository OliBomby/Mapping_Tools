using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Tools.MapCleaner.Models;
using Mapping_Tools.Desktop.Controls.Timeline;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tools.MapCleaner.Models;
using Mapping_Tools.Desktop.ViewModels;

using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Desktop.Tools.MapCleaner.ViewModels;

/// <summary>Coordinates Map Cleaner options, projects, QuickRun, and timeline results.</summary>
public sealed partial class MapCleanerViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature<MapCleanerProject>
{
    /// <inheritdoc />
    public IProjectUndoHistory? UndoHistory { get; set; }

    private readonly IMapCleanerService cleaner;

    private readonly IPlatformLauncher launcher;
    private readonly IUserNotificationService notifications;
    private readonly DesktopApplicationSettings settings;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>Creates a Map Cleaner presentation model.</summary>
    /// <param name="cleaner">Runs framework-independent cleanup operations.</param>
    /// <param name="execution">Coordinates cancellation, backup, and notifications.</param>
    /// <param name="workspace">Supplies selected beatmaps for ordinary runs.</param>
    /// <param name="settings">Supplies shared execution preferences.</param>
    /// <param name="launcher">Navigates osu! to selected timeline markers.</param>
    /// <param name="notifications">Publishes user-facing validation messages.</param>
    public MapCleanerViewModel(
        IMapCleanerService cleaner,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace,
        DesktopApplicationSettings settings,
        IPlatformLauncher launcher,
        IUserNotificationService notifications)
        : base(execution, MapCleanerToolDefinition.Definition)
    {
        this.cleaner = cleaner ?? throw new ArgumentNullException(nameof(cleaner));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <summary>Gets or sets whether slider volume changes are preserved.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool VolumeSliders { get; set; } = true;

    /// <summary>Gets or sets whether slider sample-set changes are preserved.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool SampleSetSliders { get; set; } = true;

    /// <summary>Gets or sets whether spinner volume changes are preserved.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool VolumeSpinners { get; set; } = true;

    /// <summary>Gets or sets whether hit objects and slider ends are resnapped.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool ResnapObjects { get; set; } = true;

    /// <summary>Gets or sets whether editor bookmarks are resnapped.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool ResnapBookmarks { get; set; }

    /// <summary>Gets or sets whether mapset samples are inspected.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool AnalyzeSamples { get; set; } = true;

    /// <summary>Gets or sets whether unused samples are moved to recovery.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool RemoveUnusedSamples { get; set; }

    /// <summary>Gets or sets whether object hitsounds are removed.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool RemoveHitsounds { get; set; }

    /// <summary>Gets or sets whether muting values are removed from object ends.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool RemoveMuting { get; set; }

    /// <summary>Gets or sets whether unclickable slider and spinner ends are muted.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool RemoveUnclickableHitsounds { get; set; }

    /// <summary>Gets or sets the typed beat divisors used for resnapping.</summary>
    [ObservableProperty]
    [Undoable]
    public partial IBeatDivisor[] BeatDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors();

    /// <summary>Gets the latest single-map cleanup markers.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<TimelineMarker> Markers { get; private set; } = [];

    /// <summary>Gets the final timestamp displayed by the cleanup timeline.</summary>
    [ObservableProperty]
    public partial double EndTime { get; private set; } = 20;

    /// <summary>Gets whether a successful cleanup has produced timeline state.</summary>
    [ObservableProperty]
    public partial bool HasRun { get; private set; }

    /// <summary>Cleans the current editor beatmap, falling back to the shell selection.</summary>
    /// <param name="cancellationToken">Cancels beatmap discovery or cleanup.</param>
    /// <returns>A task that completes after QuickRun finishes.</returns>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string path = await workspace.ResolveQuickRunBeatmapAsync(
            cancellationToken: cancellationToken);

        await RunWithStateAsync(() => RunPathsAsync(
            string.IsNullOrWhiteSpace(path) ? [] : [path],
            true,
            cancellationToken));
    }

    ProjectDefinition<MapCleanerProject> IShellProjectFeature<MapCleanerProject>.ProjectDefinition => new(
        "mapcleanerproject.json",
        "Map Cleaner Projects",
        () => new MapCleanerProject(),
        "map-cleaner-project.json",
        ToolConfigSchema.ForTool(MapCleanerToolDefinition.Definition.Id));

    MapCleanerProject IShellProjectFeature<MapCleanerProject>.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature<MapCleanerProject>.Install(MapCleanerProject project)
    {
        Install(project);
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        if (settings.AlwaysQuickRun)
        {
            string path = await workspace.ResolveQuickRunBeatmapAsync();
            await RunPathsAsync(
                string.IsNullOrWhiteSpace(path) ? [] : [path],
                true,
                CancellationToken.None);
            return;
        }

        await RunPathsAsync(
            workspace.SelectedPaths,
            false,
            CancellationToken.None);
    }

    [RelayCommand]
    private Task NavigateAsync(double time)
    {
        return launcher.OpenUriAsync(new Uri($"osu://edit/{Math.Round(time)}"));
    }

    private async Task RunPathsAsync(IReadOnlyList<string> paths, bool quick, CancellationToken cancellationToken)
    {
        if (paths.Count == 0)
        {
            await notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Warning,
                Tool.DisplayName,
                DesktopStrings.MapCleaner_EmptySelection));
            return;
        }

        var options = Snapshot().MapCleanerArgs;

        var execution = await Execution.ExecuteAsync(
            new ToolExecutionRequest<MapCleanerResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    Progress<double> progress = new(value =>
                        context.ReportProgress(value, DesktopStrings.MapCleaner_CleaningProgress));
                    var result = await cleaner.CleanAsync(
                        paths,
                        options,
                        quick,
                        progress,
                        context.CancellationToken);
                    return new ToolExecutionOutput<MapCleanerResult>(
                        result,
                        Summarize(result, options));
                }),
            CreateProgress(),
            cancellationToken);
        if (execution is { Status: ToolExecutionStatus.Succeeded, Value: { } result2 })
        {
            EndTime = result2.TimelineEndTime;
            Markers = paths.Count == 1 ? CreateMarkers(result2) : [];
            HasRun = paths.Count == 1;
        }
    }

    private MapCleanerProject Snapshot()
    {
        return new MapCleanerProject
        {
            MapCleanerArgs = new MapCleanerServiceOptions.MapCleanerCleanupOptions
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
                BeatDivisors = BeatDivisors.ToArray(),
            },
        };
    }

    private void Install(MapCleanerProject project)
    {
        var options = project.MapCleanerArgs ?? throw new InvalidDataException("Map Cleaner project is incomplete.");
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

    private static IReadOnlyList<TimelineMarker> CreateMarkers(MapCleanerResult result)
    {
        return result.TimingPointsAdded
            .Select(time => new TimelineMarker(
                time,
                TimelineMarkerKind.Added))
            .Concat(result.TimingPointsChanged.Select(time => new TimelineMarker(
                time,
                TimelineMarkerKind.Changed)))
            .Concat(result.TimingPointsRemovedAt.Select(time => new TimelineMarker(
                time,
                TimelineMarkerKind.Removed)))
            .OrderBy(marker => marker.Time)
            .ToArray();
    }

    private static string Summarize(MapCleanerResult result, MapCleanerServiceOptions.MapCleanerCleanupOptions options)
    {
        int greenlines = Math.Abs(result.TimingPointsRemoved);
        string summary = result.TimingPointsRemoved < 0
            ? greenlines == 1
                ? DesktopStrings.MapCleaner_SummaryAddedOne
                : ApplicationText.Format(DesktopStrings.MapCleaner_SummaryAddedMany, greenlines)
            : greenlines == 1
                ? DesktopStrings.MapCleaner_SummaryRemovedOne
                : ApplicationText.Format(DesktopStrings.MapCleaner_SummaryRemovedMany, greenlines);

        if (options.ResnapObjects)
        {
            summary += " " + (result.ObjectsResnapped == 1
                ? DesktopStrings.MapCleaner_SummaryResnappedOne
                : ApplicationText.Format(DesktopStrings.MapCleaner_SummaryResnappedMany, result.ObjectsResnapped));
        }

        if (options.RemoveUnusedSamples)
        {
            summary += " " + (result.SamplesRemoved == 1
                ? DesktopStrings.MapCleaner_SummaryRemovedSampleOne
                : ApplicationText.Format(DesktopStrings.MapCleaner_SummaryRemovedSampleMany, result.SamplesRemoved));
        }

        return summary + "!";
    }
}
