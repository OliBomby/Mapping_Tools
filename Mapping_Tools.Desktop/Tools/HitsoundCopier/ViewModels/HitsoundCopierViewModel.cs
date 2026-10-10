using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tools.HitsoundCopier.Models;
using Mapping_Tools.Desktop.ViewModels;

using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Desktop.Tools.HitsoundCopier.ViewModels;

/// <summary>Owns Hitsound Copier state, picker actions, persistence, and execution.</summary>
public sealed partial class HitsoundCopierViewModel : SingleRunToolViewModel,
    IShellProjectFeature<HitsoundCopierProject>,
    IQuickRun
{
    /// <inheritdoc />
    public IProjectUndoHistory? UndoHistory { get; set; }

    private readonly IHitsoundCopierService copier;
    private readonly ICurrentBeatmapDialogService currentBeatmapService;

    private readonly IFilePicker filePicker;
    private readonly IUserNotificationService notifications;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>Creates the Hitsound Copier presentation model.</summary>
    /// <param name="copier">Supplies the Hitsound Copier service.</param>
    /// <param name="execution">Supplies the tool execution service.</param>
    /// <param name="filePicker">Supplies the file picker service.</param>
    /// <param name="currentBeatmapService">Fetches the current beatmap and presents lookup feedback.</param>
    /// <param name="workspace">Supplies the shell's selected beatmap for QuickRun fallback.</param>
    /// <param name="notifications">Supplies the user notification service.</param>
    public HitsoundCopierViewModel(
        IHitsoundCopierService copier,
        IToolExecutionService execution,
        IFilePicker filePicker,
        ICurrentBeatmapDialogService currentBeatmapService,
        IBeatmapWorkspace workspace,
        IUserNotificationService notifications)
        : base(execution, HitsoundCopierToolDefinition.Definition)
    {
        this.copier = copier ?? throw new ArgumentNullException(nameof(copier));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.currentBeatmapService = currentBeatmapService
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapService));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <summary>Gets or sets the optional source beatmap path.</summary>
    [ObservableProperty]
    [Undoable]
    public partial string PathFrom { get; set; } = string.Empty;

    /// <summary>Gets or sets vertical-bar-separated target beatmap paths.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyPropertyChangedFor(nameof(ExportMapCountText))]
    public partial string PathTo { get; set; } = string.Empty;

    /// <summary>Gets or sets zero for overwrite-all or one for defined-only mode.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyPropertyChangedFor(nameof(SmartCopyModeSelected))]
    public partial HitsoundCopierCopyMode CopyMode { get; set; }

    /// <summary>Gets whether defined-only mode is selected.</summary>
    public bool SmartCopyModeSelected => CopyMode == HitsoundCopierCopyMode.OverwriteOnlyDefined;

    /// <summary>Gets or sets the rounded millisecond matching leniency.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyDataErrorInfo]
    [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.HitsoundCopier_NonnegativeValue))]
    public partial double TemporalLeniency { get; set; } = 5;

    /// <summary>Gets or sets whether object and edge hitsounds are copied.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool CopyHitsounds { get; set; } = true;

    /// <summary>Gets or sets whether slider-body hitsounds are copied.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool CopyBodyHitsounds { get; set; } = true;

    /// <summary>Gets or sets whether sample sets and custom indices are copied.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool CopySampleSets { get; set; } = true;

    /// <summary>Gets or sets whether timing-point volumes are copied.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool CopyVolumes { get; set; } = true;

    /// <summary>Gets or sets whether target five-percent volumes are protected.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool AlwaysPreserve5Volume { get; set; } = true;

    /// <summary>Gets or sets whether storyboard samples are copied.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool CopyStoryboardedSamples { get; set; }

    /// <summary>Gets or sets whether hitsound-satisfied storyboard samples are skipped.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool IgnoreHitsoundSatisfiedSamples { get; set; } = true;

    /// <summary>Gets or sets whether any target hitsound suppresses a storyboard sample.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool IgnoreWheneverHitsound { get; set; }

    /// <summary>Gets or sets whether unmatched hitsounds target slider ticks.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyPropertyChangedFor(nameof(StartIndexBoxVisible))]
    public partial bool CopyToSliderTicks { get; set; }

    /// <summary>Gets or sets whether unmatched hitsounds target slider slides.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyPropertyChangedFor(nameof(StartIndexBoxVisible))]
    public partial bool CopyToSliderSlides { get; set; }

    /// <summary>Gets whether the custom sample index field is relevant.</summary>
    public bool StartIndexBoxVisible => CopyToSliderTicks || CopyToSliderSlides;

    /// <summary>Gets or sets the first custom sample index.</summary>
    [ObservableProperty]
    [Undoable]
    public partial int StartIndex { get; set; } = 100;

    /// <summary>Gets or sets whether eligible slider ends are muted.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool MuteSliderends { get; set; }

    /// <summary>Gets or sets all accepted beat divisors for the muting filter.</summary>
    [ObservableProperty]
    [Undoable]
    public partial IBeatDivisor[] BeatDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors();

    /// <summary>Gets or sets muted beat divisors for the muting filter.</summary>
    [ObservableProperty]
    [Undoable]
    public partial IBeatDivisor[] MutedDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors().Skip(1).ToArray();

    /// <summary>Gets or sets the minimum eligible slider duration in beats.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyDataErrorInfo]
    [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.HitsoundCopier_NonnegativeValue))]
    public partial double MinLength { get; set; } = 0.5;

    /// <summary>Gets or sets the optional muted custom index.</summary>
    [ObservableProperty]
    [Undoable]
    public partial int MutedIndex { get; set; } = -1;

    /// <summary>Gets or sets the muted sample family.</summary>
    [ObservableProperty]
    [Undoable]
    public partial SampleSet MutedSampleSet { get; set; } = SampleSet.None;

    /// <summary>Gets the copy modes in display order.</summary>
    public IReadOnlyList<HitsoundCopierCopyMode> CopyModes { get; } =
        Enum.GetValues<HitsoundCopierCopyMode>();

    /// <summary>Gets every sample family accepted by the legacy form.</summary>
    public IReadOnlyList<SampleSet> MutedSampleSets { get; } = Enum.GetValues<SampleSet>();

    /// <summary>Gets the legacy singular/plural target count label.</summary>
    public string ExportMapCountText
    {
        get
        {
            int count = PathTo.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length;
            return count == 1
                ? DesktopStrings.HitsoundCopier_MapCountOne
                : ApplicationText.Format(DesktopStrings.HitsoundCopier_MapCountMany, count);
        }
    }

    /// <inheritdoc />
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string path = await workspace.ResolveQuickRunBeatmapAsync(
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(path))
        {
            PublishWarning(DesktopStrings.HitsoundCopier_OpenTarget);
            return;
        }

        var options = Snapshot();
        options.PathTo = path;
        await RunWithStateAsync(() => RunOptionsAsync(options, true));
    }

    ProjectDefinition<HitsoundCopierProject> IShellProjectFeature<HitsoundCopierProject>.ProjectDefinition { get; } = new(
        "hitsoundcopierproject.json",
        "Hitsound Copier Projects",
        () => new HitsoundCopierProject(),
        "hitsound-copier-project.json",
        ToolConfigSchema.ForTool(HitsoundCopierToolDefinition.Definition.Id));

    HitsoundCopierProject IShellProjectFeature<HitsoundCopierProject>.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature<HitsoundCopierProject>.Install(HitsoundCopierProject project)
    {
        Install(project);
    }

    /// <summary>Fetches the current osu! map into the source field.</summary>
    [RelayCommand]
    private async Task ImportLoadAsync()
    {
        await SetCurrentPathAsync(path => PathFrom = path);
    }

    /// <summary>Fetches the current osu! map into the target field.</summary>
    [RelayCommand]
    private async Task ExportLoadAsync()
    {
        await SetCurrentPathAsync(path => PathTo = path);
    }

    /// <summary>Opens a single-map source picker.</summary>
    [RelayCommand]
    private async Task ImportBrowseAsync()
    {
        await PickAsync(
            DesktopStrings.HitsoundCopier_BrowseSource,
            workspace.GetBeatmapPickerStartLocation(Path.GetDirectoryName(PathFrom)),
            false,
            paths => PathFrom = paths[0]);
    }

    /// <summary>Opens a multi-map target picker.</summary>
    [RelayCommand]
    private async Task ExportBrowseAsync()
    {
        await PickAsync(
            DesktopStrings.HitsoundCopier_BrowseTarget,
            GetExportPickerStartLocation(),
            true,
            paths => PathTo = string.Join('|', paths));
    }

    /// <inheritdoc />
    protected override bool PrepareRun()
    {
        if (!base.PrepareRun())
        {
            PublishWarning(DesktopStrings.HitsoundCopier_InvalidSettings);
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
        PathFrom = project.PathFrom;
        PathTo = project.PathTo;
        CopyMode = project.CopyMode;
        TemporalLeniency = project.TemporalLeniency;
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

    private async Task RunOptionsAsync(HitsoundCopierProject options, bool quick)
    {
        await Execution.ExecuteAsync(
            new ToolExecutionRequest<HitsoundCopierResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    var result = await copier.CopyAsync(
                        options,
                        quick,
                        new Progress<double>(value => context.ReportProgress(value, DesktopStrings.HitsoundCopier_Progress)),
                        context.CancellationToken);
                    return new ToolExecutionOutput<HitsoundCopierResult>(
                        result,
                        result.ProcessedCount == 1
                            ? ApplicationText.Format(DesktopStrings.HitsoundCopier_ResultOne, result.ProcessedCount)
                            : ApplicationText.Format(DesktopStrings.HitsoundCopier_ResultMany, result.ProcessedCount));
                }),
            CreateProgress());
    }

    private async Task SetCurrentPathAsync(Action<string> setter)
    {
        string? path = await currentBeatmapService.FetchAsync();
        if (path is not null) setter(path);
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
            await PublishFailureAsync(DesktopStrings.HitsoundCopier_PickerFailure, exception);
        }
    }

    private string? GetExportPickerStartLocation()
    {
        string? sourceDirectory = Path.GetDirectoryName(PathFrom);
        return string.IsNullOrWhiteSpace(sourceDirectory)
            ? workspace.GetBeatmapPickerStartLocation(GetFirstPathDirectory(PathTo))
            : sourceDirectory;
    }

    private static string? GetFirstPathDirectory(string paths)
    {
        string? path = paths
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return Path.GetDirectoryName(path);
    }

    private Task PublishFailureAsync(string title, Exception exception)
    {
        return notifications.PublishAsync(
            new UserNotification(UserNotificationSeverity.Error, title,
                DesktopStrings.HitsoundCopier_MissingBeatmapPath, exception));
    }

    private void PublishWarning(string message)
    {
        notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Warning,
            Tool.DisplayName,
            message)).GetAwaiter().GetResult();
    }
}
