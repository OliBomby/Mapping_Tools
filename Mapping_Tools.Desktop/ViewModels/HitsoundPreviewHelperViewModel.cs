using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.HitsoundPreviewHelper;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Owns Hitsound Preview Helper zones, object-selection inputs, projects,
/// ordinary execution, QuickRun, and the shared Rhythm Guide interaction.
/// </summary>
public sealed partial class HitsoundPreviewHelperViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature
{
    internal const string OperationId = "hitsound-preview-helper";

    private readonly IHitsoundPreviewHelperService _previewService;
    private readonly IBeatmapWorkspace _workspace;
    private readonly ICurrentBeatmapLocator _currentBeatmap;
    private readonly ApplicationSettings _settings;
    private readonly IUserNotificationService _notifications;
    private readonly IRhythmGuideWindowService _rhythmGuideWindow;
    private readonly RhythmGuideViewModel _rhythmGuideViewModel;
    private readonly ProjectDefinition<HitsoundPreviewHelperProject> _definition;

    /// <summary>Gets or sets the zones edited by the tool.</summary>
    [ObservableProperty]
    public partial ObservableCollection<HitsoundZone> Items { get; set; } = [];

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
        get => _isAllItemsSelected;
        set
        {
            if (_isAllItemsSelected == value)
            {
                return;
            }

            _isAllItemsSelected = value;
            if (value.HasValue)
            {
                foreach (HitsoundZone item in Items)
                {
                    item.IsSelected = value.Value;
                }
            }

            OnPropertyChanged();
        }
    }

    private bool? _isAllItemsSelected;

    /// <summary>
    /// Creates the Hitsound Preview Helper presentation model.
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
        : base(execution, OperationId)
    {
        _previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _rhythmGuideWindow = rhythmGuideWindow ?? throw new ArgumentNullException(nameof(rhythmGuideWindow));
        _rhythmGuideViewModel = rhythmGuideViewModel ?? throw new ArgumentNullException(nameof(rhythmGuideViewModel));
        ArgumentNullException.ThrowIfNull(directories);

        _definition = new ProjectDefinition<HitsoundPreviewHelperProject>(
            "hspreviewproject.json",
            "Hitsound Preview Projects",
            static () => new HitsoundPreviewHelperProject(),
            "hitsound-preview-project.json");
    }

    /// <inheritdoc/>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await _currentBeatmap
            .FindCurrentBeatmapAsync(cancellationToken)
            .ConfigureAwait(false);
        await RunWithStateAsync(() => RunPathsAsync(
            string.IsNullOrWhiteSpace(path) ? [] : [path],
            quick: true,
            cancellationToken));
    }

    /// <inheritdoc/>
    protected override async Task RunCoreAsync()
    {
        string? currentPath = null;
        if (ImportModeSetting == HitsoundPreviewHelperImportMode.Selected)
        {
            currentPath = await _currentBeatmap.FindCurrentBeatmapAsync();
        }

        IReadOnlyList<string> paths = ImportModeSetting == HitsoundPreviewHelperImportMode.Selected
            ? string.IsNullOrWhiteSpace(currentPath) ? [] : [currentPath]
            : _workspace.SelectedPaths;
        if (_settings.AlwaysQuickRun)
        {
            string? quickPath = await _currentBeatmap.FindCurrentBeatmapAsync();
            paths = string.IsNullOrWhiteSpace(quickPath) ? [] : [quickPath];
        }

        await RunPathsAsync(paths, _settings.AlwaysQuickRun, CancellationToken.None);
    }

    /// <inheritdoc/>
    protected override bool PrepareRun()
    {
        if (Items.Count == 0)
        {
            ResultSummary = "There are no zones!";
            return false;
        }

        if (ImportModeSetting == HitsoundPreviewHelperImportMode.Time &&
            string.IsNullOrWhiteSpace(TimeCode))
        {
            ResultSummary = "Enter a time code before using Time mode.";
            return false;
        }

        return true;
    }

    /// <summary>Adds a new wildcard zone.</summary>
    [RelayCommand]
    private void Add() => Items.Add(new HitsoundZone());

    /// <summary>Adds one zone for each distinct selected editor position.</summary>
    [RelayCommand]
    private async Task AddFromSelectionAsync()
    {
        string? path = await _currentBeatmap.FindCurrentBeatmapAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            await PublishSelectionWarningAsync();
            return;
        }

        try
        {
            IReadOnlyList<Mapping_Tools.Core.Classes.MathUtil.Vector2> positions =
                await _previewService.GetSelectedZonePositionsAsync(path);
            if (positions.Count == 0)
            {
                await PublishSelectionWarningAsync();
                return;
            }

            foreach (Mapping_Tools.Core.Classes.MathUtil.Vector2 position in positions)
            {
                Items.Add(new HitsoundZone { XPos = position.X, YPos = position.Y });
            }
        }
        catch (Exception exception)
        {
            await _notifications.PublishAsync(new UserNotification(
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
        {
            if (Items[index].IsSelected)
            {
                Items.Add(Items[index].Copy());
            }
        }
    }

    /// <summary>Removes every selected zone.</summary>
    [RelayCommand]
    private void Remove() =>
        Items = new ObservableCollection<HitsoundZone>(Items.Where(item => !item.IsSelected));

    /// <summary>Opens the shared modeless Rhythm Guide auxiliary surface.</summary>
    [RelayCommand]
    private void OpenRhythmGuide() => _rhythmGuideWindow.Show(_rhythmGuideViewModel);

    string IQuickRun.OperationId => OperationId;

    IProjectDefinition IShellProjectFeature.ProjectDefinition => _definition;

    object IShellProjectFeature.Snapshot() => new HitsoundPreviewHelperProject
    {
        ImportModeSetting = ImportModeSetting,
        TimeCode = TimeCode,
        Items = Items.Select(item => item.Copy()).ToList()
    };

    void IShellProjectFeature.Install(object project)
    {
        if (project is not HitsoundPreviewHelperProject typed ||
            typed.Items is null ||
            !Enum.IsDefined(typed.ImportModeSetting))
        {
            throw new InvalidDataException("Hitsound Preview Helper project is incomplete.");
        }

        ImportModeSetting = typed.ImportModeSetting;
        TimeCode = typed.TimeCode ?? string.Empty;
        Items = new ObservableCollection<HitsoundZone>(typed.Items.Select(item => item.Copy()));
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
            Items = Items.Select(item => item.Copy()).ToList()
        };
        ToolExecutionResult<HitsoundPreviewHelperResult> result = await Execution.ExecuteAsync(
            new ToolExecutionRequest<HitsoundPreviewHelperResult>(
                OperationId,
                "Hitsound Preview Helper",
                async context =>
                {
                    HitsoundPreviewHelperResult applied = await _previewService.ApplyAsync(
                        paths,
                        options,
                        new Progress<double>(value => context.ReportProgress(
                            value,
                            "Placing preview hitsounds")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<HitsoundPreviewHelperResult>(
                        applied,
                        quick ? null : "Done!",
                        reloadEditor: quick);
                }),
            CreateProgress(),
            cancellationToken);

        if (result.Status == ToolExecutionStatus.Succeeded && result.Value is { } value)
        {
            ResultSummary = quick
                ? $"Placed {value.UpdatedEventCount} preview hitsounds."
                : "Done!";
        }
    }

    private Task PublishSelectionWarningAsync() => _notifications.PublishAsync(new UserNotification(
        UserNotificationSeverity.Warning,
        "No selected hit objects",
        "Please select a hit object to fetch the coordinates."));
}
