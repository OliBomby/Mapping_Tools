using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.HitsoundCopier;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.HitsoundCopier;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Owns Hitsound Copier state, picker actions, persistence, and execution.</summary>
public sealed partial class HitsoundCopierViewModel : SingleRunToolViewModel,
    IShellProjectFeature,
    IQuickRun
{

    private readonly IHitsoundCopierService copier;
    private readonly ICurrentBeatmapLocator currentBeatmap;

    private readonly ProjectDefinition<HitsoundCopierProject> definition = new(
        "hitsoundcopierproject.json",
        "Hitsound Copier Projects",
        () => new HitsoundCopierProject(),
        "hitsound-copier-project.json");

    private readonly IFilePicker filePicker;
    private readonly IUserNotificationService notifications;
    private readonly ApplicationSettings settings;

    /// <summary>Creates the Hitsound Copier presentation model.</summary>
    public HitsoundCopierViewModel(
        IHitsoundCopierService copier,
        IToolExecutionService execution,
        IFilePicker filePicker,
        ICurrentBeatmapLocator currentBeatmap,
        IUserNotificationService notifications,
        ApplicationSettings settings)
        : base(execution, MappingToolDefinitions.HitsoundCopier)
    {
        this.copier = copier ?? throw new ArgumentNullException(nameof(copier));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>Gets or sets the optional source beatmap path.</summary>
    [ObservableProperty]
    public partial string PathFrom { get; set; } = string.Empty;

    /// <summary>Gets or sets vertical-bar-separated target beatmap paths.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExportMapCountText))]
    public partial string PathTo { get; set; } = string.Empty;

    /// <summary>Gets or sets zero for overwrite-all or one for defined-only mode.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, 1)]
    [NotifyPropertyChangedFor(nameof(SmartCopyModeSelected))]
    public partial int CopyMode { get; set; }

    /// <summary>Gets whether defined-only mode is selected.</summary>
    public bool SmartCopyModeSelected => CopyMode == 1;

    /// <summary>Gets or sets the rounded millisecond matching leniency.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, double.MaxValue)]
    public partial double TemporalLeniency { get; set; } = 5;

    /// <summary>Gets or sets the source object selection mode.</summary>
    [ObservableProperty]
    public partial HitsoundCopierSelectionMode SourceSelectionMode { get; set; } =
        HitsoundCopierSelectionMode.Everything;

    /// <summary>Gets or sets the legacy time-code query for Time mode.</summary>
    [ObservableProperty]
    public partial string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the millisecond source timing offset.</summary>
    [ObservableProperty]
    public partial double TimingOffset { get; set; }

    /// <summary>Gets or sets whether object and edge hitsounds are copied.</summary>
    [ObservableProperty]
    public partial bool CopyHitsounds { get; set; } = true;

    /// <summary>Gets or sets whether slider-body hitsounds are copied.</summary>
    [ObservableProperty]
    public partial bool CopyBodyHitsounds { get; set; } = true;

    /// <summary>Gets or sets whether sample sets and custom indices are copied.</summary>
    [ObservableProperty]
    public partial bool CopySampleSets { get; set; } = true;

    /// <summary>Gets or sets whether timing-point volumes are copied.</summary>
    [ObservableProperty]
    public partial bool CopyVolumes { get; set; } = true;

    /// <summary>Gets or sets whether target five-percent volumes are protected.</summary>
    [ObservableProperty]
    public partial bool AlwaysPreserve5Volume { get; set; } = true;

    /// <summary>Gets or sets whether storyboard samples are copied.</summary>
    [ObservableProperty]
    public partial bool CopyStoryboardedSamples { get; set; }

    /// <summary>Gets or sets whether hitsound-satisfied storyboard samples are skipped.</summary>
    [ObservableProperty]
    public partial bool IgnoreHitsoundSatisfiedSamples { get; set; } = true;

    /// <summary>Gets or sets whether any target hitsound suppresses a storyboard sample.</summary>
    [ObservableProperty]
    public partial bool IgnoreWheneverHitsound { get; set; }

    /// <summary>Gets or sets whether unmatched hitsounds target slider ticks.</summary>
    [ObservableProperty]
    public partial bool CopyToSliderTicks { get; set; }

    /// <summary>Gets or sets whether unmatched hitsounds target slider slides.</summary>
    [ObservableProperty]
    public partial bool CopyToSliderSlides { get; set; }

    /// <summary>Gets whether the custom sample index field is relevant.</summary>
    public bool StartIndexBoxVisible => CopyToSliderTicks || CopyToSliderSlides;

    /// <summary>Gets or sets the first custom sample index.</summary>
    [ObservableProperty]
    public partial int StartIndex { get; set; } = 100;

    /// <summary>Gets or sets whether eligible slider ends are muted.</summary>
    [ObservableProperty]
    public partial bool MuteSliderends { get; set; }

    /// <summary>Gets or sets all accepted beat divisors for the muting filter.</summary>
    [ObservableProperty]
    public partial IBeatDivisor[] BeatDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors();

    /// <summary>Gets or sets muted beat divisors for the muting filter.</summary>
    [ObservableProperty]
    public partial IBeatDivisor[] MutedDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors().Skip(1).ToArray();

    /// <summary>Gets or sets the minimum eligible slider duration in beats.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, double.MaxValue)]
    public partial double MinLength { get; set; } = 0.5;

    /// <summary>Gets or sets the optional muted custom index.</summary>
    [ObservableProperty]
    public partial int MutedIndex { get; set; } = -1;

    /// <summary>Gets or sets the muted sample family.</summary>
    [ObservableProperty]
    public partial SampleSet MutedSampleSet { get; set; } = SampleSet.None;

    /// <summary>Gets the displayable source selection choices.</summary>
    public IReadOnlyList<HitsoundCopierSelectionMode> SourceSelectionModes { get; } =
        Enum.GetValues<HitsoundCopierSelectionMode>();

    /// <summary>Gets the displayable copy-mode labels.</summary>
    public IReadOnlyList<string> CopyModes { get; } = ["Overwrite everything", "Overwrite only defined"];

    /// <summary>Gets every sample family accepted by the legacy form.</summary>
    public IReadOnlyList<SampleSet> MutedSampleSets { get; } = Enum.GetValues<SampleSet>();

    /// <summary>Gets the latest ordinary-run summary.</summary>
    [ObservableProperty]
    public partial string ResultSummary { get; private set; } = string.Empty;

    /// <summary>Gets the legacy singular/plural target count label.</summary>
    public string ExportMapCountText
    {
        get
        {
            int count = PathTo.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length;
            return count == 1 ? "(1) map total" : $"({count}) maps total";
        }
    }

    /// <inheritdoc />
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(path))
        {
            ResultSummary = "Open a target beatmap in osu! before using QuickRun.";
            return;
        }

        HitsoundCopierOptions options = Snapshot();
        options.PathTo = path;
        await RunWithStateAsync(() => RunOptionsAsync(options, true));
    }

    IProjectDefinition IShellProjectFeature.ProjectDefinition => definition;

    object IShellProjectFeature.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature.Install(object project)
    {
        if (project is not HitsoundCopierProject typed) throw new InvalidDataException("Hitsound Copier project is incomplete.");
        Install(typed);
    }

    partial void OnCopyToSliderTicksChanged(bool value)
    {
        OnPropertyChanged(nameof(StartIndexBoxVisible));
    }

    partial void OnCopyToSliderSlidesChanged(bool value)
    {
        OnPropertyChanged(nameof(StartIndexBoxVisible));
    }

    /// <summary>Fetches the current osu! map into the source field.</summary>
    [RelayCommand]
    private async Task ImportLoadAsync()
    {
        await SetCurrentPathAsync(path => PathFrom = path, "source");
    }

    /// <summary>Fetches the current osu! map into the target field.</summary>
    [RelayCommand]
    private async Task ExportLoadAsync()
    {
        await SetCurrentPathAsync(path => PathTo = path, "target");
    }

    /// <summary>Opens a single-map source picker.</summary>
    [RelayCommand]
    private async Task ImportBrowseAsync()
    {
        await PickAsync(
            "Copy hitsounds from", PathFrom, false, paths => PathFrom = paths[0]);
    }

    /// <summary>Opens a multi-map target picker.</summary>
    [RelayCommand]
    private async Task ExportBrowseAsync()
    {
        await PickAsync(
            "Copy hitsounds to", FirstTargetDirectory(), true, paths => PathTo = string.Join('|', paths));
    }

    /// <inheritdoc />
    protected override bool PrepareRun()
    {
        ValidateAllProperties();
        if (HasErrors || string.IsNullOrWhiteSpace(PathTo)) return false;
        if (SourceSelectionMode == HitsoundCopierSelectionMode.Time && string.IsNullOrWhiteSpace(TimeCode))
        {
            ResultSummary = "Enter a time code before using Time mode.";
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        await RunOptionsAsync(Snapshot(), false);
    }

    private HitsoundCopierProject Snapshot()
    {
        return new HitsoundCopierProject
        {
            PathFrom = PathFrom,
            PathTo = PathTo,
            CopyMode = CopyMode,
            TemporalLeniency = TemporalLeniency,
            SourceSelectionMode = SourceSelectionMode,
            TimeCode = TimeCode,
            TimingOffset = TimingOffset,
            CopyHitsounds = CopyHitsounds,
            CopyBodyHitsounds = CopyBodyHitsounds,
            CopySampleSets = CopySampleSets,
            CopyVolumes = CopyVolumes,
            AlwaysPreserve5Volume = AlwaysPreserve5Volume,
            CopyStoryboardedSamples = CopyStoryboardedSamples,
            IgnoreHitsoundSatisfiedSamples = IgnoreHitsoundSatisfiedSamples,
            IgnoreWheneverHitsound = IgnoreWheneverHitsound,
            CopyToSliderTicks = CopyToSliderTicks,
            CopyToSliderSlides = CopyToSliderSlides,
            StartIndex = StartIndex,
            MuteSliderends = MuteSliderends,
            BeatDivisors = BeatDivisors.ToArray(),
            MutedDivisors = MutedDivisors.ToArray(),
            MinLength = MinLength,
            MutedIndex = MutedIndex,
            MutedSampleSet = MutedSampleSet,
        };
    }

    private void Install(HitsoundCopierProject project)
    {
        if (!Enum.IsDefined(project.SourceSelectionMode)
            || project.BeatDivisors is null
            || project.BeatDivisors.Length == 0
            || project.MutedDivisors is null
            || project.MutedDivisors.Length == 0
            || project.CopyMode is not 0 and not 1)
            throw new InvalidDataException("Hitsound Copier project is incomplete.");
        PathFrom = project.PathFrom ?? string.Empty;
        PathTo = project.PathTo ?? string.Empty;
        CopyMode = project.CopyMode;
        TemporalLeniency = project.TemporalLeniency;
        SourceSelectionMode = project.SourceSelectionMode;
        TimeCode = project.TimeCode ?? string.Empty;
        TimingOffset = project.TimingOffset;
        CopyHitsounds = project.CopyHitsounds;
        CopyBodyHitsounds = project.CopyBodyHitsounds;
        CopySampleSets = project.CopySampleSets;
        CopyVolumes = project.CopyVolumes;
        AlwaysPreserve5Volume = project.AlwaysPreserve5Volume;
        CopyStoryboardedSamples = project.CopyStoryboardedSamples;
        IgnoreHitsoundSatisfiedSamples = project.IgnoreHitsoundSatisfiedSamples;
        IgnoreWheneverHitsound = project.IgnoreWheneverHitsound;
        CopyToSliderTicks = project.CopyToSliderTicks;
        CopyToSliderSlides = project.CopyToSliderSlides;
        StartIndex = project.StartIndex;
        MuteSliderends = project.MuteSliderends;
        BeatDivisors = project.BeatDivisors.ToArray();
        MutedDivisors = project.MutedDivisors.ToArray();
        MinLength = project.MinLength;
        MutedIndex = project.MutedIndex;
        MutedSampleSet = project.MutedSampleSet;
    }

    private async Task RunOptionsAsync(HitsoundCopierOptions options, bool quick)
    {
        await Execution.ExecuteAsync(
            new ToolExecutionRequest<HitsoundCopierResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    var result = await copier.CopyAsync(
                        options,
                        new Progress<double>(value => context.ReportProgress(value, "Copying hitsounds")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<HitsoundCopierResult>(
                        result,
                        quick ? null : $"Successfully copied hitsounds to {result.ProcessedCount} " + $"{(result.ProcessedCount == 1 ? "beatmap" : "beatmaps")}!",
                        quick);
                }),
            CreateProgress());
    }

    private async Task SetCurrentPathAsync(Action<string> setter, string label)
    {
        try
        {
            string? path = await currentBeatmap.FindCurrentBeatmapAsync();
            if (!string.IsNullOrWhiteSpace(path)) setter(path);
        }
        catch (Exception exception)
        {
            await PublishFailureAsync($"Could not fetch the {label} beatmap", exception);
        }
    }

    private async Task PickAsync(
        string title,
        string? startLocation,
        bool allowMultiple,
        Action<IReadOnlyList<string>> apply)
    {
        try
        {
            var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
            {
                Title = title,
                SuggestedStartLocation = startLocation,
                AllowMultiple = allowMultiple,
                Filters = [CommonFilePickerFilters.BeatmapsAndStoryboards],
            });
            if (paths.Count > 0) apply(paths);
        }
        catch (Exception exception)
        {
            await PublishFailureAsync("Could not select beatmaps", exception);
        }
    }

    private string? FirstTargetDirectory()
    {
        string? target = PathTo.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(target) ? settings.SongsPath : Path.GetDirectoryName(target);
    }

    private Task PublishFailureAsync(string title, Exception exception)
    {
        return notifications.PublishAsync(
            new UserNotification(UserNotificationSeverity.Error, title,
                "The beatmap path could not be obtained.", exception));
    }
}
