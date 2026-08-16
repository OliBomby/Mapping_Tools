using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.AutoFail;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Timeline;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.AutoFail;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Coordinates Auto-fail Detector options, execution, fixes, and timeline output.</summary>
public sealed partial class AutoFailDetectorViewModel : SingleRunToolViewModel, IShellFeatureActivation
{
    internal const string OperationId = "auto-fail-detector";
    private readonly IAutoFailService _autoFail;
    private readonly IBeatmapWorkspace _workspace;
    private readonly ICurrentBeatmapLocator _currentBeatmap;
    private readonly ApplicationSettings _settings;
    private readonly IDialogService _dialogs;
    private readonly IQuickRunCommandRegistry _quickRunRegistry;
    private readonly IPlatformLauncher _launcher;

    /// <summary>Gets or sets whether confirmed unloading objects appear on the timeline.</summary>
    [ObservableProperty]
    public partial bool ShowUnloadingObjects { get; set; } = true;

    /// <summary>Gets or sets whether possible unloading objects appear on the timeline.</summary>
    [ObservableProperty]
    public partial bool ShowPotentialUnloadingObjects { get; set; }

    /// <summary>Gets or sets whether disrupting objects appear on the timeline.</summary>
    [ObservableProperty]
    public partial bool ShowPotentialDisruptors { get; set; }

    /// <summary>Gets or sets the simulated approach rate, or -1 to use the map value.</summary>
    [ObservableProperty]
    public partial double ApproachRateOverride { get; set; } = -1;

    /// <summary>Gets or sets the simulated overall difficulty, or -1 to use the map value.</summary>
    [ObservableProperty]
    public partial double OverallDifficultyOverride { get; set; } = -1;

    /// <summary>Gets or sets the tolerated physics-update delay in milliseconds.</summary>
    [ObservableProperty]
    public partial int PhysicsUpdateLeniency { get; set; } = 9;

    /// <summary>Gets or sets whether analysis offers repair guidance.</summary>
    [ObservableProperty]
    public partial bool GetAutoFailFix { get; set; }

    /// <summary>Gets or sets whether an accepted repair may insert spinners automatically.</summary>
    [ObservableProperty]
    public partial bool AutoPlaceFix { get; set; }

    /// <summary>Gets the final timestamp displayed by the result timeline.</summary>
    [ObservableProperty]
    public partial double EndTime { get; private set; } = 20;

    /// <summary>Gets whether a successful analysis has produced timeline state.</summary>
    [ObservableProperty]
    public partial bool HasRun { get; private set; }

    /// <summary>Gets the filtered result markers displayed on the timeline.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<TimelineMarker> Markers { get; private set; } = [];

    /// <summary>Gets a textual summary of the latest analysis.</summary>
    [ObservableProperty]
    public partial string ResultSummary { get; private set; } =
        "Run the detector to inspect this beatmap.";

    /// <summary>Creates an Auto-fail Detector presentation model.</summary>
    /// <param name="autoFail">Analyzes beatmaps and applies repairs.</param>
    /// <param name="execution">Coordinates cancellation, backup, and notifications.</param>
    /// <param name="workspace">Supplies the shell's selected beatmap.</param>
    /// <param name="currentBeatmap">Finds the beatmap open in osu! for QuickRun.</param>
    /// <param name="settings">Supplies QuickRun behavior preferences.</param>
    /// <param name="dialogs">Presents repair choices.</param>
    /// <param name="quickRunRegistry">Tracks the active QuickRun-capable tool.</param>
    /// <param name="launcher">Navigates osu! to selected timeline markers.</param>
    public AutoFailDetectorViewModel(
        IAutoFailService autoFail,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace,
        ICurrentBeatmapLocator currentBeatmap,
        ApplicationSettings settings,
        IDialogService dialogs,
        IQuickRunCommandRegistry quickRunRegistry,
        IPlatformLauncher launcher)
        : base(execution, OperationId)
    {
        _autoFail = autoFail ?? throw new ArgumentNullException(nameof(autoFail));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _quickRunRegistry = quickRunRegistry ?? throw new ArgumentNullException(nameof(quickRunRegistry));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
    }

    /// <summary>Selects this feature as the current QuickRun target.</summary>
    public void Activate() => _quickRunRegistry.SelectCurrent(OperationId);

    /// <summary>Clears this feature as the current QuickRun target when it is active.</summary>
    public void Deactivate()
    {
        if (_quickRunRegistry.CurrentCommandId == OperationId)
        {
            _quickRunRegistry.SelectCurrent(null);
        }
    }

    /// <inheritdoc/>
    protected override async Task RunCoreAsync()
    {
        string? path = _settings.AlwaysQuickRun
            ? await _currentBeatmap.FindCurrentBeatmapAsync()
            : _workspace.SelectedPaths.FirstOrDefault();
        await RunPathAsync(path, CancellationToken.None);
    }

    /// <summary>Analyzes the beatmap currently open in osu! through the QuickRun path.</summary>
    /// <param name="cancellationToken">Cancels beatmap discovery or analysis.</param>
    /// <returns>A task that completes after QuickRun finishes.</returns>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await _currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        await RunWithStateAsync(() => RunPathAsync(path, cancellationToken));
    }

    [RelayCommand]
    private Task NavigateAsync(double time) => _launcher.OpenUriAsync(
        new Uri($"osu://edit/{Math.Round(time)}"));

    private async Task RunPathAsync(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            ResultSummary = "Select a beatmap or open one in osu! before running the detector.";
            return;
        }

        ToolExecutionResult<AutoFailRun> result = await Execution.ExecuteAsync(
            new ToolExecutionRequest<AutoFailRun>(
                OperationId,
                "Auto-fail Detector",
                async context =>
                {
                    context.ReportProgress(33, "Loading beatmap");
                    AutoFailRun run = await _autoFail.AnalyzeAsync(
                        new AutoFailOptions(
                            path,
                            ApproachRateOverride,
                            OverallDifficultyOverride,
                            PhysicsUpdateLeniency),
                        context.CancellationToken);
                    context.ReportProgress(67, "Planning fixes");
                    context.ReportProgress(100, "Analysis complete");
                    return new ToolExecutionOutput<AutoFailRun>(run, Summarize(run.Analysis));
                }),
            CreateProgress(),
            cancellationToken);
        if (result.Status != ToolExecutionStatus.Succeeded || result.Value is null)
        {
            return;
        }

        InstallResult(result.Value);
        if (GetAutoFailFix)
        {
            await OfferFixesAsync(result.Value, cancellationToken);
        }
    }

    private void InstallResult(AutoFailRun run)
    {
        HasRun = true;
        ResultSummary = Summarize(run.Analysis);
        EndTime = run.MapEndTime;
        List<TimelineMarker> markers = [];
        if (ShowPotentialUnloadingObjects)
        {
            markers.AddRange(run.Analysis.PotentialUnloadingObjects.Select(time =>
                new TimelineMarker(time, TimelineMarkerKind.Added, "Potential unloading object")));
        }
        if (ShowPotentialDisruptors)
        {
            markers.AddRange(run.Analysis.Disruptors.Select(time =>
                new TimelineMarker(time, TimelineMarkerKind.Accent, "Potential disruptor")));
        }
        if (ShowUnloadingObjects)
        {
            markers.AddRange(run.Analysis.UnloadingObjects.Select(time =>
                new TimelineMarker(time, TimelineMarkerKind.Removed, "Unloading object")));
        }
        Markers = markers.OrderBy(marker => marker.Time).ToArray();
    }

    private async Task OfferFixesAsync(AutoFailRun run, CancellationToken cancellationToken)
    {
        foreach (AutoFailFixPlan plan in _autoFail.GetFixPlans(run, cancellationToken))
        {
            FixChoice choice = await _dialogs.ShowMessageAsync(
                new MessageDialogRequest<FixChoice>(
                    "Auto-fail fix",
                    plan.Guide,
                    AutoPlaceFix
                        ? [
                            new DialogChoice<FixChoice>("Apply", FixChoice.Apply, IsDefault: true),
                            new DialogChoice<FixChoice>("Next solution", FixChoice.Next),
                            new DialogChoice<FixChoice>("Cancel", FixChoice.Cancel, IsCancel: true)]
                        : [
                            new DialogChoice<FixChoice>("Done", FixChoice.Done, IsDefault: true),
                            new DialogChoice<FixChoice>("Next solution", FixChoice.Next),
                            new DialogChoice<FixChoice>("Cancel", FixChoice.Cancel, IsCancel: true)],
                    FixChoice.Cancel),
                cancellationToken);
            if (choice == FixChoice.Next)
            {
                continue;
            }
            if (choice == FixChoice.Apply)
            {
                ToolExecutionResult<bool> applied = await Execution.ExecuteAsync(
                    new ToolExecutionRequest<bool>(
                        OperationId + "-fix",
                        "Auto-fail Fix",
                        async context =>
                        {
                            await _autoFail.ApplyFixAsync(run, plan, context.CancellationToken);
                            return new ToolExecutionOutput<bool>(true, "Applied the auto-fail fix.", reloadEditor: true);
                        }),
                    cancellationToken: cancellationToken);
                if (applied.Status == ToolExecutionStatus.Succeeded)
                {
                    ResultSummary += " Fix applied.";
                }
            }
            return;
        }
    }

    private static string Summarize(AutoFailAnalysis analysis) => analysis.HasAutoFail
        ? $"{analysis.UnloadingObjects.Count} unloading objects detected and {analysis.PotentialUnloadingObjects.Count} potential unloading objects detected!"
        : analysis.PotentialUnloadingObjects.Count > 0
            ? $"No auto-fail, but {analysis.PotentialUnloadingObjects.Count} potential unloading objects detected."
            : "No auto-fail detected.";

    private enum FixChoice { Apply, Next, Done, Cancel }
}
