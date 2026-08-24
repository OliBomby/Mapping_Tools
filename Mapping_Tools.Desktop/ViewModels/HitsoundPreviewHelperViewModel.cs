using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper.Models;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels.Adapters;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Owns Hitsound Preview Helper zones, object-selection inputs, projects,
///     ordinary execution, QuickRun, and the shared Rhythm Guide interaction.
/// </summary>
public sealed partial class HitsoundPreviewHelperViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature
{
    private readonly ICurrentBeatmapLocator currentBeatmap;
    private readonly ProjectDefinition<HitsoundPreviewHelperProject> definition;
    private readonly IUserNotificationService notifications;

    private readonly IHitsoundPreviewHelperService previewService;
    private readonly RhythmGuideViewModel rhythmGuideViewModel;
    private readonly IRhythmGuideWindowService rhythmGuideWindow;
    private readonly ApplicationSettings settings;
    private readonly IBeatmapWorkspace workspace;

    private bool? isAllItemsSelected;

    /// <summary>
    ///     Creates the Hitsound Preview Helper presentation model.
    /// </summary>
    /// <param name="previewService">Runs the framework-independent preview transformation.</param>
    /// <param name="execution">Coordinates cancellation, backup, notifications, and reload.</param>
    /// <param name="workspace">Supplies selected beatmap paths for ordinary runs.</param>
    /// <param name="currentBeatmap">Finds the beatmap currently open in osu!.</param>
    /// <param name="settings">Supplies QuickRun preferences.</param>
    /// <param name="notifications">Publishes recoverable input and selection messages.</param>
    /// <param name="rhythmGuideWindow">Opens the shared Rhythm Guide auxiliary surface.</param>
    /// <param name="rhythmGuideViewModel">Provides the shared Rhythm Guide project state.</param>
    /// <param name="directories">Supplies the application project location.</param>
    public HitsoundPreviewHelperViewModel(
        IHitsoundPreviewHelperService previewService,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace,
        ICurrentBeatmapLocator currentBeatmap,
        ApplicationSettings settings,
        IUserNotificationService notifications,
        IRhythmGuideWindowService rhythmGuideWindow,
        RhythmGuideViewModel rhythmGuideViewModel,
        IApplicationDirectories directories)
        : base(execution, MappingToolDefinitions.HitsoundPreviewHelper)
    {
        this.previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.rhythmGuideWindow = rhythmGuideWindow ?? throw new ArgumentNullException(nameof(rhythmGuideWindow));
        this.rhythmGuideViewModel = rhythmGuideViewModel ?? throw new ArgumentNullException(nameof(rhythmGuideViewModel));
        ArgumentNullException.ThrowIfNull(directories);

        definition = new ProjectDefinition<HitsoundPreviewHelperProject>(
            "hspreviewproject.json",
            "Hitsound Preview Projects",
            static () => new HitsoundPreviewHelperProject(),
            "hitsound-preview-project.json");
    }

    /// <summary>Gets or sets the zones edited by the tool.</summary>
    [ObservableProperty]
    public partial ObservableCollection<ObservableHitsoundZone> Items { get; set; } = [];

    /// <summary>Gets or sets which beatmap objects receive preview hitsounds.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeCodeVisible))]
    public partial HitsoundPreviewHelperImportMode ImportModeSetting { get; set; } =
        HitsoundPreviewHelperImportMode.Everything;

    /// <summary>Gets or sets the legacy osu! time-code query used by Time mode.</summary>
    [ObservableProperty]
    public partial string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets a concise result or validation message for the latest action.</summary>
    [ObservableProperty]
    public partial string ResultSummary { get; private set; } =
        "Add hitsound zones, then run the helper.";

    /// <summary>Gets every supported object-selection mode.</summary>
    public IReadOnlyList<HitsoundPreviewHelperImportMode> ImportModes { get; } =
        Enum.GetValues<HitsoundPreviewHelperImportMode>();

    /// <summary>Gets every supported hitsound layer.</summary>
    public IReadOnlyList<Hitsound> Hitsounds { get; } = Enum.GetValues<Hitsound>();

    /// <summary>Gets every supported sample family.</summary>
    public IReadOnlyList<SampleSet> SampleSets { get; } = Enum.GetValues<SampleSet>();

    /// <summary>Gets whether the time-code field applies to the current mode.</summary>
    public bool TimeCodeVisible => ImportModeSetting == HitsoundPreviewHelperImportMode.Time;

    /// <summary>Gets or sets the tri-state select-all value used by the zone list.</summary>
    public bool? IsAllItemsSelected
    {
        get => isAllItemsSelected;
        set
        {
            if (isAllItemsSelected == value) return;

            isAllItemsSelected = value;
            if (value.HasValue)
                foreach (var item in Items)
                    item.IsSelected = value.Value;

            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await currentBeatmap
            .FindCurrentBeatmapAsync(cancellationToken)
            .ConfigureAwait(false);
        await RunWithStateAsync(() => RunPathsAsync(
            string.IsNullOrWhiteSpace(path) ? [] : [path],
            true,
            cancellationToken));
    }

    IProjectDefinition IShellProjectFeature.ProjectDefinition => definition;

    object IShellProjectFeature.Snapshot()
    {
        return new HitsoundPreviewHelperProject
        {
            ImportModeSetting = ImportModeSetting,
            TimeCode = TimeCode,
            Items = Items.Select(item => item.Snapshot()).ToList(),
        };
    }

    void IShellProjectFeature.Install(object project)
    {
        if (project is not HitsoundPreviewHelperProject typed || typed.Items is null || !Enum.IsDefined(typed.ImportModeSetting))
            throw new InvalidDataException("Hitsound Preview Helper project is incomplete.");

        ImportModeSetting = typed.ImportModeSetting;
        TimeCode = typed.TimeCode ?? string.Empty;
        Items = new ObservableCollection<ObservableHitsoundZone>(
            typed.Items.Select(item => new ObservableHitsoundZone(item.Copy())));
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        string? currentPath = null;
        if (ImportModeSetting == HitsoundPreviewHelperImportMode.Selected) currentPath = await currentBeatmap.FindCurrentBeatmapAsync();

        var paths = ImportModeSetting == HitsoundPreviewHelperImportMode.Selected
            ? string.IsNullOrWhiteSpace(currentPath) ? [] : [currentPath]
            : workspace.SelectedPaths;
        if (settings.AlwaysQuickRun)
        {
            string? quickPath = await currentBeatmap.FindCurrentBeatmapAsync();
            paths = string.IsNullOrWhiteSpace(quickPath) ? [] : [quickPath];
        }

        await RunPathsAsync(paths, settings.AlwaysQuickRun, CancellationToken.None);
    }

    /// <inheritdoc />
    protected override bool PrepareRun()
    {
        if (Items.Count == 0)
        {
            ResultSummary = "There are no zones!";
            return false;
        }

        if (ImportModeSetting == HitsoundPreviewHelperImportMode.Time && string.IsNullOrWhiteSpace(TimeCode))
        {
            ResultSummary = "Enter a time code before using Time mode.";
            return false;
        }

        return true;
    }

    /// <summary>Adds a new wildcard zone.</summary>
    [RelayCommand]
    private void Add()
    {
        Items.Add(new ObservableHitsoundZone());
    }

    /// <summary>Adds one zone for each distinct selected editor position.</summary>
    [RelayCommand]
    private async Task AddFromSelectionAsync()
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            await PublishSelectionWarningAsync();
            return;
        }

        try
        {
            var positions =
                await previewService.GetSelectedZonePositionsAsync(path);
            if (positions.Count == 0)
            {
                await PublishSelectionWarningAsync();
                return;
            }

            foreach (var position in positions)
                Items.Add(new ObservableHitsoundZone(new HitsoundZone
                {
                    XPos = position.X,
                    YPos = position.Y,
                }));
        }
        catch (Exception exception)
        {
            await notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Error,
                "Could not read selection",
                "The selected editor coordinates could not be read.",
                exception));
        }
    }

    /// <summary>Copies every selected zone once.</summary>
    [RelayCommand]
    private void Copy()
    {
        int initialCount = Items.Count;
        for (int index = 0; index < initialCount; index++)
            if (Items[index].IsSelected)
                Items.Add(new ObservableHitsoundZone(Items[index].Snapshot()));
    }

    /// <summary>Removes every selected zone.</summary>
    [RelayCommand]
    private void Remove()
    {
        Items = new ObservableCollection<ObservableHitsoundZone>(Items.Where(item => !item.IsSelected));
    }

    /// <summary>Opens the shared modeless Rhythm Guide auxiliary surface.</summary>
    [RelayCommand]
    private void OpenRhythmGuide()
    {
        rhythmGuideWindow.Show(rhythmGuideViewModel);
    }

    private async Task RunPathsAsync(
        IReadOnlyList<string> paths,
        bool quick,
        CancellationToken cancellationToken)
    {
        if (paths.Count == 0)
        {
            ResultSummary = "Select at least one beatmap or open one in osu! before running Hitsound Preview Helper.";
            return;
        }

        HitsoundPreviewHelperOptions options = new()
        {
            ImportModeSetting = ImportModeSetting,
            TimeCode = TimeCode,
            Items = Items.Select(item => item.Snapshot()).ToList(),
        };
        var result = await Execution.ExecuteAsync(
            new ToolExecutionRequest<HitsoundPreviewHelperResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    var applied = await previewService.ApplyAsync(
                        paths,
                        options,
                        new Progress<double>(value => context.ReportProgress(
                            value,
                            "Placing preview hitsounds")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<HitsoundPreviewHelperResult>(
                        applied,
                        quick ? null : "Done!",
                        quick);
                }),
            CreateProgress(),
            cancellationToken);

        if (result.Status == ToolExecutionStatus.Succeeded && result.Value is { } value)
            ResultSummary = quick
                ? $"Placed {value.UpdatedEventCount} preview hitsounds."
                : "Done!";
    }

    private Task PublishSelectionWarningAsync()
    {
        return notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Warning,
            "No selected hit objects",
            "Please select a hit object to fetch the coordinates."));
    }
}
