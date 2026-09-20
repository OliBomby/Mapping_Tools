using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.Tools.AutoFail.Models;
using Mapping_Tools.Desktop.Controls.Timeline;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.AutoFailDetector.ViewModels;

/// <summary>Coordinates Auto-fail Detector options, execution, fixes, and timeline output.</summary>
public sealed partial class AutoFailDetectorViewModel : SingleRunToolViewModel, IQuickRun
{
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
                "Select a beatmap or open one in osu! before running the detector."));
            return;
        }

        var result = await Execution.ExecuteAsync(
            new ToolExecutionRequest<AutoFailRun>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    context.ReportProgress(0.33, "Loading beatmap");
                    var run = await autoFail.AnalyzeAsync(
                        new AutoFailServiceOptions(
                            path,
                            ApproachRateOverride,
                            OverallDifficultyOverride,
                            PhysicsUpdateLeniency),
                        context.CancellationToken);
                    context.ReportProgress(0.67, "Planning fixes");
                    context.ReportProgress(1, "Analysis complete");
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
                        $"Solution {++solutionCount}",
                        $"{plan.Guide}{Environment.NewLine}{Environment.NewLine}Do you want to use this solution?",
                        AutoPlaceFix
                            ?
                            [
                                new DialogChoice<FixChoice>("Yes", FixChoice.Apply, true),
                                new DialogChoice<FixChoice>("No", FixChoice.Next),
                                new DialogChoice<FixChoice>("Cancel", FixChoice.Cancel, IsCancel: true),
                            ]
                            :
                            [
                                new DialogChoice<FixChoice>("Yes", FixChoice.Done, true),
                                new DialogChoice<FixChoice>("No", FixChoice.Next),
                                new DialogChoice<FixChoice>("Cancel", FixChoice.Cancel, IsCancel: true),
                            ],
                        FixChoice.Cancel),
                    cancellationToken);
                if (choice == FixChoice.Next) continue;
                if (choice == FixChoice.Apply)
                    await Execution.ExecuteAsync(
                        new ToolExecutionRequest<bool>(
                            Tool.Id + "-fix",
                            "Auto-fail Fix",
                            async context =>
                            {
                                await autoFail.ApplyFixAsync(
                                    run,
                                    plan,
                                    quick,
                                    context.CancellationToken);
                                return new ToolExecutionOutput<bool>(true, "Applied the auto-fail fix.");
                            }),
                        cancellationToken: cancellationToken);

                return;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await dialogs.ShowMessageAsync(
                new MessageDialogRequest<bool>(
                    "Auto-fail fix",
                    "Could not create an auto-fail fix guide.",
                    [new DialogChoice<bool>("OK", true, true, true)],
                    false,
                    exception.Message),
                cancellationToken);
        }
    }

    private static string Summarize(AutoFailAnalysis analysis)
    {
        return analysis.HasAutoFail
            ? $"{analysis.UnloadingObjects.Count} unloading objects detected and {analysis.PotentialUnloadingObjects.Count} potential unloading objects detected!"
            : analysis.PotentialUnloadingObjects.Count > 0
                ? $"No auto-fail, but {analysis.PotentialUnloadingObjects.Count} potential unloading objects detected."
                : "No auto-fail detected.";
    }

    private enum FixChoice { Apply, Next, Done, Cancel }
}
