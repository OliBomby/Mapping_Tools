using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.Tools.AutoFail.Models;
using Mapping_Tools.Desktop.Controls.Timeline;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tools.AutoFailDetector.Models;
using Mapping_Tools.Desktop.ViewModels;

using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Desktop.Tools.AutoFailDetector.ViewModels;

/// <summary>Coordinates Auto-fail Detector options, execution, fixes, and timeline output.</summary>
public sealed partial class AutoFailDetectorViewModel : SingleRunToolViewModel, IQuickRun,
    IShellProjectFeature<AutoFailDetectorProject>
{
    /// <inheritdoc />
    public IProjectUndoHistory? UndoHistory { get; set; }

    private readonly IAutoFailService autoFail;
    private readonly IDialogService dialogs;
    private readonly IPlatformLauncher launcher;
    private readonly IUserNotificationService notifications;
    private readonly DesktopApplicationSettings settings;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>Creates an Auto-fail Detector presentation model.</summary>
    /// <param name="autoFail">Analyzes beatmaps and applies repairs.</param>
    /// <param name="execution">Coordinates cancellation, backup, and notifications.</param>
    /// <param name="workspace">Supplies the shell's selected beatmap.</param>
    /// <param name="settings">Supplies QuickRun behavior preferences.</param>
    /// <param name="dialogs">Presents repair choices.</param>
    /// <param name="launcher">Navigates osu! to selected timeline markers.</param>
    /// <param name="notifications">Publishes user-facing validation and completion messages.</param>
    public AutoFailDetectorViewModel(
        IAutoFailService autoFail,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace,
        DesktopApplicationSettings settings,
        IDialogService dialogs,
        IPlatformLauncher launcher,
        IUserNotificationService notifications)
        : base(execution, AutoFailDetectorToolDefinition.Definition)
    {
        this.autoFail = autoFail ?? throw new ArgumentNullException(nameof(autoFail));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <summary>Gets or sets whether confirmed unloading objects appear on the timeline.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool ShowUnloadingObjects { get; set; } = true;

    /// <summary>Gets or sets whether possible unloading objects appear on the timeline.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool ShowPotentialUnloadingObjects { get; set; }

    /// <summary>Gets or sets whether disrupting objects appear on the timeline.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool ShowPotentialDisruptors { get; set; }

    /// <summary>Gets or sets the simulated approach rate, or -1 to use the map value.</summary>
    [ObservableProperty]
    [Undoable]
    public partial double ApproachRateOverride { get; set; } = -1;

    /// <summary>Gets or sets the simulated overall difficulty, or -1 to use the map value.</summary>
    [ObservableProperty]
    [Undoable]
    public partial double OverallDifficultyOverride { get; set; } = -1;

    /// <summary>Gets or sets the tolerated physics-update delay in milliseconds.</summary>
    [ObservableProperty]
    [Undoable]
    public partial int PhysicsUpdateLeniency { get; set; } = 9;

    /// <summary>Gets or sets whether analysis offers repair guidance.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool GetAutoFailFix { get; set; }

    /// <summary>Gets or sets whether an accepted repair may insert spinners automatically.</summary>
    [ObservableProperty]
    [Undoable]
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

    ProjectDefinition<AutoFailDetectorProject> IShellProjectFeature<AutoFailDetectorProject>.ProjectDefinition { get; } = new(
        "autofaildetectorproject.json",
        "Auto-fail Detector Projects",
        () => new AutoFailDetectorProject(),
        "auto-fail-detector-project.json",
        ToolConfigSchema.ForTool(AutoFailDetectorToolDefinition.Definition.Id));

    AutoFailDetectorProject IShellProjectFeature<AutoFailDetectorProject>.Snapshot()
    {
        return new AutoFailDetectorProject
        {
            ShowUnloadingObjects = ShowUnloadingObjects,
            ShowPotentialUnloadingObjects = ShowPotentialUnloadingObjects,
            ShowPotentialDisruptors = ShowPotentialDisruptors,
            ApproachRateOverride = ApproachRateOverride,
            OverallDifficultyOverride = OverallDifficultyOverride,
            PhysicsUpdateLeniency = PhysicsUpdateLeniency,
            GetAutoFailFix = GetAutoFailFix,
            AutoPlaceFix = AutoPlaceFix,
        };
    }

    void IShellProjectFeature<AutoFailDetectorProject>.Install(AutoFailDetectorProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        ShowUnloadingObjects = project.ShowUnloadingObjects;
        ShowPotentialUnloadingObjects = project.ShowPotentialUnloadingObjects;
        ShowPotentialDisruptors = project.ShowPotentialDisruptors;
        ApproachRateOverride = project.ApproachRateOverride;
        OverallDifficultyOverride = project.OverallDifficultyOverride;
        PhysicsUpdateLeniency = project.PhysicsUpdateLeniency;
        GetAutoFailFix = project.GetAutoFailFix;
        AutoPlaceFix = project.AutoPlaceFix;
    }

    /// <summary>Analyzes the current editor beatmap, falling back to the shell selection.</summary>
    /// <param name="cancellationToken">Cancels beatmap discovery or analysis.</param>
    /// <returns>A task that completes after QuickRun finishes.</returns>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string path = await workspace.ResolveQuickRunBeatmapAsync(
            cancellationToken: cancellationToken);
        await RunWithStateAsync(() => RunPathAsync(path, true, cancellationToken));
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        string? path = settings.AlwaysQuickRun
            ? await workspace.ResolveQuickRunBeatmapAsync()
            : workspace.SelectedPaths.FirstOrDefault();

        await RunPathAsync(path, settings.AlwaysQuickRun, CancellationToken.None);
    }

    [RelayCommand]
    private Task NavigateAsync(double time)
    {
        return launcher.OpenUriAsync(
            new Uri($"osu://edit/{Math.Round(time)}"));
    }

    private async Task RunPathAsync(string? path, bool quick, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            await notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Warning,
                Tool.DisplayName,
                DesktopStrings.AutoFailDetector_SelectBeatmap));
            return;
        }

        var result = await Execution.ExecuteAsync(
            new ToolExecutionRequest<AutoFailRun>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    context.ReportProgress(0.33, DesktopStrings.AutoFailDetector_ProgressLoading);
                    var run = await autoFail.AnalyzeAsync(
                        new AutoFailServiceOptions(
                            path,
                            ApproachRateOverride,
                            OverallDifficultyOverride,
                            PhysicsUpdateLeniency),
                        context.CancellationToken);
                    context.ReportProgress(0.67, DesktopStrings.AutoFailDetector_ProgressPlanning);
                    context.ReportProgress(1, DesktopStrings.AutoFailDetector_ProgressComplete);
                    return new ToolExecutionOutput<AutoFailRun>(run, Summarize(run.Analysis));
                }),
            CreateProgress(),
            cancellationToken);
        if (result.Status != ToolExecutionStatus.Succeeded || result.Value is null) return;

        InstallResult(result.Value);
        if (GetAutoFailFix) await OfferFixesAsync(result.Value, quick, cancellationToken);
    }

    private void InstallResult(AutoFailRun run)
    {
        HasRun = true;
        EndTime = run.MapEndTime;
        List<TimelineMarker> markers = [];
        if (ShowPotentialUnloadingObjects)
            markers.AddRange(run.Analysis.PotentialUnloadingObjects.Select(time =>
                new TimelineMarker(time, TimelineMarkerKind.Added)));
        if (ShowPotentialDisruptors)
            markers.AddRange(run.Analysis.Disruptors.Select(time =>
                new TimelineMarker(time, TimelineMarkerKind.Accent)));
        if (ShowUnloadingObjects)
            markers.AddRange(run.Analysis.UnloadingObjects.Select(time =>
                new TimelineMarker(time, TimelineMarkerKind.Removed)));
        Markers = markers.OrderBy(marker => marker.Time).ToArray();
    }

    private async Task OfferFixesAsync(AutoFailRun run, bool quick, CancellationToken cancellationToken)
    {
        try
        {
            // Plan enumeration is lazy; keep the solver off the UI thread like the legacy worker did.
            using var plans = await Task.Run(
                () => autoFail.GetFixPlans(run, cancellationToken).GetEnumerator(),
                cancellationToken);
            int solutionCount = 0;
            while (await Task.Run(plans.MoveNext, cancellationToken))
            {
                var plan = plans.Current;
                var choice = await dialogs.ShowMessageAsync(
                    new MessageDialogRequest<FixChoice>(
                        ApplicationText.Format(DesktopStrings.AutoFailDetector_SolutionTitle, ++solutionCount),
                        ApplicationText.Format(DesktopStrings.AutoFailDetector_SolutionPrompt,
                            plan.Guide,
                            Environment.NewLine,
                            Environment.NewLine),
                        AutoPlaceFix
                            ?
                            [
                                new DialogChoice<FixChoice>(DesktopStrings.AutoFailDetector_Yes, FixChoice.Apply, true),
                                new DialogChoice<FixChoice>(DesktopStrings.AutoFailDetector_No, FixChoice.Next),
                                new DialogChoice<FixChoice>(DesktopStrings.AutoFailDetector_Cancel, FixChoice.Cancel, IsCancel: true),
                            ]
                            :
                            [
                                new DialogChoice<FixChoice>(DesktopStrings.AutoFailDetector_Yes, FixChoice.Done, true),
                                new DialogChoice<FixChoice>(DesktopStrings.AutoFailDetector_No, FixChoice.Next),
                                new DialogChoice<FixChoice>(DesktopStrings.AutoFailDetector_Cancel, FixChoice.Cancel, IsCancel: true),
                            ],
                        FixChoice.Cancel),
                    cancellationToken);
                if (choice == FixChoice.Next) continue;
                if (choice == FixChoice.Apply)
                    await Execution.ExecuteAsync(
                        new ToolExecutionRequest<bool>(
                            Tool.Id + "-fix",
                            DesktopStrings.AutoFailDetector_FixToolName,
                            async context =>
                            {
                                await autoFail.ApplyFixAsync(
                                    run,
                                    plan,
                                    quick,
                                    context.CancellationToken);
                                return new ToolExecutionOutput<bool>(true, DesktopStrings.AutoFailDetector_FixApplied);
                            }),
                        cancellationToken: cancellationToken);

                return;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await dialogs.ShowMessageAsync(
                new MessageDialogRequest<bool>(
                    DesktopStrings.AutoFailDetector_FixErrorTitle,
                    DesktopStrings.AutoFailDetector_FixError,
                    [new DialogChoice<bool>(DesktopStrings.AutoFailDetector_Ok, true, true, true)],
                    false,
                    exception.ToString()),
                cancellationToken);
        }
    }

    private static string Summarize(AutoFailAnalysis analysis)
    {
        return analysis.HasAutoFail
            ? ApplicationText.Format(DesktopStrings.AutoFailDetector_SummaryDetected, analysis.UnloadingObjects.Count, analysis.PotentialUnloadingObjects.Count)
            : analysis.PotentialUnloadingObjects.Count > 0
                ? ApplicationText.Format(DesktopStrings.AutoFailDetector_SummaryPotential, analysis.PotentialUnloadingObjects.Count)
                : DesktopStrings.AutoFailDetector_SummaryNone;
    }

    private enum FixChoice { Apply, Next, Done, Cancel }
}
