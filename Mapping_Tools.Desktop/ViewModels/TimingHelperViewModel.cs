using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions.Validation;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.TimingHelper;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Tools.TimingHelper;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Owns Timing Helper form state, project persistence, ordinary execution, and QuickRun.
/// </summary>
public sealed partial class TimingHelperViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature
{
    internal const string OPERATION_ID = "timing-helper";
    private readonly ICurrentBeatmapLocator currentBeatmap;

    private readonly ProjectDefinition<TimingHelperProject> definition = new(
        "timinghelperproject.json",
        "Timing Helper Projects",
        static () => new TimingHelperProject(),
        "timing-helper-project.json");

    private readonly ApplicationSettings settings;

    private readonly ITimingHelperService timingHelper;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>
    ///     Creates a Timing Helper presentation model.
    /// </summary>
    /// <param name="timingHelper">Runs the framework-independent timing transformation.</param>
    /// <param name="execution">Coordinates background execution, cancellation, and notifications.</param>
    /// <param name="currentBeatmap">Finds the beatmap currently open in osu!.</param>
    /// <param name="workspace">Supplies the shell's selected beatmap paths.</param>
    /// <param name="settings">Supplies QuickRun and automatic-reload preferences.</param>
    public TimingHelperViewModel(
        ITimingHelperService timingHelper,
        IToolExecutionService execution,
        ICurrentBeatmapLocator currentBeatmap,
        IBeatmapWorkspace workspace,
        ApplicationSettings settings)
        : base(execution, OPERATION_ID)
    {
        this.timingHelper = timingHelper ?? throw new ArgumentNullException(nameof(timingHelper));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>Gets or sets whether hit objects are counted as timing markers.</summary>
    [ObservableProperty]
    public partial bool Objects { get; set; } = true;

    /// <summary>Gets or sets whether bookmarks are counted as timing markers.</summary>
    [ObservableProperty]
    public partial bool Bookmarks { get; set; } = true;

    /// <summary>Gets or sets whether greenlines are counted as timing markers.</summary>
    [ObservableProperty]
    public partial bool Greenlines { get; set; } = true;

    /// <summary>Gets or sets whether redlines are counted as timing markers and retained.</summary>
    [ObservableProperty]
    public partial bool Redlines { get; set; } = true;

    /// <summary>Gets or sets whether inserted redlines omit their first barline.</summary>
    [ObservableProperty]
    public partial bool OmitBarline { get; set; }

    /// <summary>Gets or sets the tolerated marker error in milliseconds.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [InclusiveRange<double>(0, double.MaxValue, ErrorMessage = "Leniency must be non-negative and finite.")]
    public partial double Leniency { get; set; } = 3;

    /// <summary>
    ///     Gets or sets the number of beats requested between markers, or <c>-1</c>
    ///     to infer the spacing.
    /// </summary>
    [ObservableProperty]
    public partial double BeatsBetween { get; set; } = -1;

    /// <summary>Gets or sets the beat divisors used to resnap marker times.</summary>
    [ObservableProperty]
    public partial IBeatDivisor[] BeatDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors();

    /// <summary>Runs Timing Helper against the beatmap currently open in osu!.</summary>
    /// <param name="cancellationToken">Cancels beatmap discovery or timing adjustment.</param>
    /// <returns>A task that completes after QuickRun reaches a terminal state.</returns>
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

    string IQuickRun.OperationId => OPERATION_ID;

    IProjectDefinition IShellProjectFeature.ProjectDefinition => definition;

    object IShellProjectFeature.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature.Install(object project)
    {
        Install((TimingHelperProject)project);
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        if (settings.AlwaysQuickRun)
        {
            string? path = await currentBeatmap.FindCurrentBeatmapAsync();
            await RunPathsAsync(
                string.IsNullOrWhiteSpace(path) ? [] : [path],
                true,
                CancellationToken.None);
            return;
        }

        await RunPathsAsync(workspace.SelectedPaths, false, CancellationToken.None);
    }

    /// <inheritdoc />
    protected override bool PrepareRun()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    private async Task RunPathsAsync(
        IReadOnlyList<string> paths,
        bool quick,
        CancellationToken cancellationToken)
    {
        if (paths.Count == 0) return;

        TimingHelperOptions options = Snapshot();
        await Execution.ExecuteAsync(
                new ToolExecutionRequest<TimingHelperResult>(
                    OPERATION_ID,
                    "Timing Helper",
                    async context =>
                    {
                        Progress<double> progress = new(value =>
                            context.ReportProgress(value, "Adjusting timing"));
                        var result = await timingHelper.AdjustAsync(
                            paths,
                            options,
                            progress,
                            context.CancellationToken);
                        return new ToolExecutionOutput<TimingHelperResult>(
                            result,
                            quick ? null : $"Successfully added {result.RedlinesAdded} redlines!",
                            quick);
                    }),
                CreateProgress(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private TimingHelperProject Snapshot()
    {
        return new TimingHelperProject
        {
            Objects = Objects,
            Bookmarks = Bookmarks,
            Greenlines = Greenlines,
            Redlines = Redlines,
            OmitBarline = OmitBarline,
            Leniency = Leniency,
            BeatsBetween = BeatsBetween,
            BeatDivisors = BeatDivisors.ToArray(),
        };
    }

    private void Install(TimingHelperProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!double.IsFinite(project.Leniency)
            || project.Leniency < 0
            || project.BeatDivisors is null
            || project.BeatDivisors.Length == 0
            || project.BeatDivisors.Any(divisor => divisor is null))
            throw new InvalidDataException("Timing Helper project is incomplete.");

        Objects = project.Objects;
        Bookmarks = project.Bookmarks;
        Greenlines = project.Greenlines;
        Redlines = project.Redlines;
        OmitBarline = project.OmitBarline;
        Leniency = project.Leniency;
        BeatsBetween = project.BeatsBetween;
        BeatDivisors = project.BeatDivisors.ToArray();
    }
}
