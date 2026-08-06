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

public sealed partial class AutoFailDetectorViewModel : ObservableObject, IShellFeatureActivation
{
    internal const string OperationId = "auto-fail-detector";
    private readonly IAutoFailService _autoFail;
    private readonly IToolExecutionService _execution;
    private readonly IBeatmapWorkspace _workspace;
    private readonly ICurrentBeatmapLocator _currentBeatmap;
    private readonly ApplicationSettings _settings;
    private readonly IDialogService _dialogs;
    private readonly IQuickRunCommandRegistry _quickRunRegistry;
    private readonly IPlatformLauncher _launcher;

    [ObservableProperty] private bool _showUnloadingObjects = true;
    [ObservableProperty] private bool _showPotentialUnloadingObjects;
    [ObservableProperty] private bool _showPotentialDisruptors;
    [ObservableProperty] private double _approachRateOverride = -1;
    [ObservableProperty] private double _overallDifficultyOverride = -1;
    [ObservableProperty] private int _physicsUpdateLeniency = 9;
    [ObservableProperty] private bool _getAutoFailFix;
    [ObservableProperty] private bool _autoPlaceFix;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private double _endTime = 20;
    [ObservableProperty] private IReadOnlyList<TimelineMarker> _markers = [];
    [ObservableProperty] private string _resultSummary = "Run the detector to inspect this beatmap.";

    public AutoFailDetectorViewModel(
        IAutoFailService autoFail,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace,
        ICurrentBeatmapLocator currentBeatmap,
        ApplicationSettings settings,
        IDialogService dialogs,
        IQuickRunCommandRegistry quickRunRegistry,
        IPlatformLauncher launcher)
    {
        _autoFail = autoFail ?? throw new ArgumentNullException(nameof(autoFail));
        _execution = execution ?? throw new ArgumentNullException(nameof(execution));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _quickRunRegistry = quickRunRegistry ?? throw new ArgumentNullException(nameof(quickRunRegistry));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
    }

    public void Activate() => _quickRunRegistry.SelectCurrent(OperationId);

    public void Deactivate()
    {
        if (_quickRunRegistry.CurrentCommandId == OperationId)
        {
            _quickRunRegistry.SelectCurrent(null);
        }
    }

    private bool CanRun() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        string? path = _settings.AlwaysQuickRun
            ? await _currentBeatmap.FindCurrentBeatmapAsync()
            : _workspace.SelectedPaths.FirstOrDefault();
        await RunPathAsync(path, CancellationToken.None);
    }

    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await _currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        await RunPathAsync(path, cancellationToken);
    }

    [RelayCommand]
    private void Cancel() => _execution.Cancel(OperationId);

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

        IsRunning = true;
        Progress = 0;
        try
        {
            ToolExecutionResult<AutoFailRun> result = await _execution.ExecuteAsync(
                new ToolExecutionRequest<AutoFailRun>(
                    OperationId,
                    "Auto-fail Detector",
                    async context =>
                    {
                        context.ReportProgress(10, "Loading beatmap");
                        AutoFailRun run = await _autoFail.AnalyzeAsync(
                            new AutoFailOptions(
                                path,
                                ApproachRateOverride,
                                OverallDifficultyOverride,
                                PhysicsUpdateLeniency),
                            context.CancellationToken);
                        context.ReportProgress(100, "Analysis complete");
                        return new ToolExecutionOutput<AutoFailRun>(run, Summarize(run.Analysis));
                    }),
                new Progress<ToolExecutionProgress>(value => Progress = value.Percent),
                cancellationToken);
            if (result.Status != ToolExecutionStatus.Succeeded || result.Value is null)
            {
                return;
            }

            InstallResult(result.Value);
            if (GetAutoFailFix && result.Value.Analysis.PotentialUnloadingObjects.Count > 0)
            {
                await OfferFixesAsync(result.Value, cancellationToken);
            }
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void InstallResult(AutoFailRun run)
    {
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
                ToolExecutionResult<bool> applied = await _execution.ExecuteAsync(
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
