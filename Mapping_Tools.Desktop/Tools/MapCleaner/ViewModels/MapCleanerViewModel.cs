using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Desktop.Controls.Timeline;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Tools.MapCleaner.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tools.MapCleaner.Models;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.MapCleaner.ViewModels;

/// <summary>Coordinates Map Cleaner options, projects, QuickRun, and timeline results.</summary>
public sealed partial class MapCleanerViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature<MapCleanerProject>
{
    private readonly IMapCleanerService cleaner;
    private readonly ICurrentBeatmapLocator currentBeatmap;

    private readonly ProjectDefinition<MapCleanerProject> definition = new(
        "mapcleanerproject.json",
        "Map Cleaner Projects",
        () => new MapCleanerProject(),
        "map-cleaner-project.json",
        ToolConfigSchema.ForTool(MapCleanerToolDefinition.Definition.Id));

    private readonly IPlatformLauncher launcher;
    private readonly DesktopApplicationSettings settings;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>Creates a Map Cleaner presentation model.</summary>
    /// <param name="cleaner">Runs framework-independent cleanup operations.</param>
    /// <param name="execution">Coordinates cancellation, backup, and notifications.</param>
    /// <param name="workspace">Supplies selected beatmaps for ordinary runs.</param>
    /// <param name="currentBeatmap">Finds the beatmap open in osu! for QuickRun.</param>
    /// <param name="settings">Supplies shared execution preferences.</param>
    /// <param name="launcher">Navigates osu! to selected timeline markers.</param>
    public MapCleanerViewModel(
        IMapCleanerService cleaner,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace,
        ICurrentBeatmapLocator currentBeatmap,
        DesktopApplicationSettings settings,
        IPlatformLauncher launcher)
        : base(execution, MapCleanerToolDefinition.Definition)
    {
        this.cleaner = cleaner ?? throw new ArgumentNullException(nameof(cleaner));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
    }

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

    /// <summary>Cleans the beatmap currently open in osu! through the QuickRun path.</summary>
    /// <param name="cancellationToken">Cancels beatmap discovery or cleanup.</param>
    /// <returns>A task that completes after QuickRun finishes.</returns>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string path = await currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);

        await RunWithStateAsync(() => RunPathsAsync(
            string.IsNullOrWhiteSpace(path) ? [] : [path],
            true,
            cancellationToken));
    }

    ProjectDefinition<MapCleanerProject> IShellProjectFeature<MapCleanerProject>.ProjectDefinition => definition;

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
            string path = await currentBeatmap.FindCurrentBeatmapAsync();
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
            ResultSummary = "Select at least one beatmap or open one in osu! before running Map Cleaner.";
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
                        context.ReportProgress(value, "Cleaning beatmaps"));
                    var result = await cleaner.CleanAsync(
                        paths,
                        options,
                        progress,
                        context.CancellationToken);
                    return new ToolExecutionOutput<MapCleanerResult>(
                        result,
                        quick ? null : Summarize(result, options),
                        quick);
                }),
            CreateProgress(),
            cancellationToken);
        if (execution.Status == ToolExecutionStatus.Succeeded && execution.Value is { } result)
        {
            ResultSummary = Summarize(result, options);
            EndTime = result.TimelineEndTime;
            Markers = paths.Count == 1 ? CreateMarkers(result) : [];
            HasRun = paths.Count == 1;
        }
    }

    private MapCleanerProject Snapshot()
    {
        return new MapCleanerProject
        {
            MapCleanerArgs = new MapCleanerProject.MapCleanerCleanupOptions
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
        var options = project?.MapCleanerArgs ?? throw new InvalidDataException("Map Cleaner project is incomplete.");
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

    private static string Summarize(MapCleanerResult result, MapCleanerProject.MapCleanerCleanupOptions options)
    {
        return $"Successfully {(result.TimingPointsRemoved < 0 ? "added" : "removed")} "
               + $"{Math.Abs(result.TimingPointsRemoved)} "
               + $"{(Math.Abs(result.TimingPointsRemoved) == 1 ? "greenline" : "greenlines")}"
               + (options.ResnapObjects
                   ? $" and resnapped {result.ObjectsResnapped} " + $"{(result.ObjectsResnapped == 1 ? "object" : "objects")}"
                   : string.Empty)
               + (options.RemoveUnusedSamples
                   ? $" and removed {result.SamplesRemoved} unused " + $"{(result.SamplesRemoved == 1 ? "sample" : "samples")}"
                   : string.Empty)
               + "!";
    }
}
