using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.SliderCompletionator;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.SliderCompletionator;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Owns Slider Completionator form state, project persistence, ordinary runs,
/// and the current-editor QuickRun path.
/// </summary>
public sealed partial class SliderCompletionatorViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature
{
    internal const string OperationId = "slider-completionator";

    private readonly ISliderCompletionatorService _completionator;
    private readonly ICurrentBeatmapLocator _currentBeatmap;
    private readonly IBeatmapWorkspace _workspace;
    private readonly ApplicationSettings _settings;
    private readonly ProjectDefinition<SliderCompletionatorProject> _definition = new(
        "slidercompletionatorproject.json",
        "Slider Completionator Projects",
        static () => new SliderCompletionatorProject(),
        "slider-completionator-project.json");

    /// <summary>Gets the import modes in their legacy display order.</summary>
    public IReadOnlyList<SliderCompletionatorImportMode> ImportModes { get; } =
        Enum.GetValues<SliderCompletionatorImportMode>();

    /// <summary>Gets the calculated-value choices in their legacy display order.</summary>
    public IReadOnlyList<SliderCompletionatorFreeVariable> FreeVariables { get; } =
        Enum.GetValues<SliderCompletionatorFreeVariable>();

    /// <summary>Gets or sets the source-object import mode.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeCodeVisible))]
    public partial SliderCompletionatorImportMode ImportModeSetting { get; set; } =
        SliderCompletionatorImportMode.Selected;

    /// <summary>Gets or sets the value calculated from the other slider inputs.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationVisible))]
    [NotifyPropertyChangedFor(nameof(EndTimeVisible))]
    [NotifyPropertyChangedFor(nameof(LengthVisible))]
    [NotifyPropertyChangedFor(nameof(VelocityVisible))]
    public partial SliderCompletionatorFreeVariable FreeVariableSetting { get; set; } =
        SliderCompletionatorFreeVariable.Velocity;

    /// <summary>Gets or sets the legacy time-code query.</summary>
    [ObservableProperty]
    public partial string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the requested duration in beats, or <c>-1</c> to preserve it.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(double.MinValue, double.MaxValue, ErrorMessage = "Enter a finite duration.")]
    public partial double Duration { get; set; } = -1;

    /// <summary>Gets or sets the requested end time in milliseconds, or <c>-1</c> to preserve it.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(double.MinValue, double.MaxValue, ErrorMessage = "Enter a finite end time.")]
    public partial double EndTime { get; set; } = -1;

    /// <summary>Gets or sets the requested complete-path fraction, or <c>-1</c> to preserve it.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(double.MinValue, double.MaxValue, ErrorMessage = "Enter a finite length.")]
    public partial double Length { get; set; } = 1;

    /// <summary>Gets or sets the requested slider velocity multiplier, or <c>-1</c> to preserve it.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(double.MinValue, double.MaxValue, ErrorMessage = "Enter a finite velocity.")]
    public partial double SliderVelocity { get; set; } = -1;

    /// <summary>Gets or sets whether anchors are moved to the new slider length.</summary>
    [ObservableProperty]
    public partial bool MoveAnchors { get; set; }

    /// <summary>Gets or sets whether end time replaces duration input.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationVisible))]
    [NotifyPropertyChangedFor(nameof(EndTimeVisible))]
    public partial bool UseEndTime { get; set; }

    /// <summary>Gets or sets whether the live editor playhead supplies the end time.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndTimeVisible))]
    public partial bool UseCurrentEditorTime { get; set; }

    /// <summary>Gets or sets whether slider velocity is delegated to BPM timing points.</summary>
    [ObservableProperty]
    public partial bool DelegateToBpm { get; set; }

    /// <summary>Gets or sets whether delegated sliders remove slider ticks.</summary>
    [ObservableProperty]
    public partial bool RemoveSliderTicks { get; set; }

    /// <summary>Gets whether the time-code field is visible for the selected import mode.</summary>
    public bool TimeCodeVisible => ImportModeSetting == SliderCompletionatorImportMode.Time;

    /// <summary>Gets whether the duration field is visible for the selected free variable.</summary>
    public bool DurationVisible =>
        FreeVariableSetting != SliderCompletionatorFreeVariable.Duration && !UseEndTime;

    /// <summary>Gets whether the explicit end-time field is visible.</summary>
    public bool EndTimeVisible =>
        FreeVariableSetting != SliderCompletionatorFreeVariable.Duration &&
        UseEndTime &&
        !UseCurrentEditorTime;

    /// <summary>Gets whether the slider length field is visible.</summary>
    public bool LengthVisible => FreeVariableSetting != SliderCompletionatorFreeVariable.Length;

    /// <summary>Gets whether the slider velocity field is visible.</summary>
    public bool VelocityVisible => FreeVariableSetting != SliderCompletionatorFreeVariable.Velocity;

    /// <summary>
    /// Creates a Slider Completionator presentation model.
    /// </summary>
    /// <param name="completionator">Runs the framework-independent slider transformation.</param>
    /// <param name="execution">Coordinates background execution and notifications.</param>
    /// <param name="currentBeatmap">Finds the beatmap currently open in osu!.</param>
    /// <param name="workspace">Supplies the shell's selected beatmap paths.</param>
    /// <param name="settings">Supplies QuickRun preferences.</param>
    public SliderCompletionatorViewModel(
        ISliderCompletionatorService completionator,
        IToolExecutionService execution,
        ICurrentBeatmapLocator currentBeatmap,
        IBeatmapWorkspace workspace,
        ApplicationSettings settings)
        : base(execution, OperationId)
    {
        _completionator = completionator ?? throw new ArgumentNullException(nameof(completionator));
        _currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
        if (ImportModeSetting == SliderCompletionatorImportMode.Selected)
        {
            currentPath = await _currentBeatmap.FindCurrentBeatmapAsync();
        }

        IReadOnlyList<string> paths = ImportModeSetting == SliderCompletionatorImportMode.Selected
            ? string.IsNullOrWhiteSpace(currentPath) ? [] : [currentPath]
            : _workspace.SelectedPaths;
        await RunPathsAsync(paths, quick: _settings.AlwaysQuickRun, CancellationToken.None);
    }

    /// <inheritdoc/>
    protected override bool PrepareRun()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    string IQuickRun.OperationId => OperationId;

    IProjectDefinition IShellProjectFeature.ProjectDefinition => _definition;

    object IShellProjectFeature.Snapshot() => Snapshot();

    void IShellProjectFeature.Install(object project) => Install((SliderCompletionatorProject)project);

    private async Task RunPathsAsync(
        IReadOnlyList<string> paths,
        bool quick,
        CancellationToken cancellationToken)
    {
        if (paths.Count == 0)
        {
            return;
        }

        SliderCompletionatorOptions options = Snapshot();
        await Execution.ExecuteAsync(
                new ToolExecutionRequest<SliderCompletionatorResult>(
                    OperationId,
                    "Slider Completionator",
                    async context =>
                    {
                        SliderCompletionatorResult result = await _completionator.CompleteAsync(
                            paths,
                            options,
                            new Progress<double>(value => context.ReportProgress(
                                value,
                                "Completing sliders")),
                            context.CancellationToken);
                        string message = $"Successfully completed {result.SlidersCompleted} " +
                                         $"{(result.SlidersCompleted == 1 ? "slider" : "sliders") }!";
                        return new ToolExecutionOutput<SliderCompletionatorResult>(
                            result,
                            quick ? null : message,
                            reloadEditor: quick);
                    }),
                CreateProgress(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private SliderCompletionatorProject Snapshot() => new()
    {
        ImportModeSetting = ImportModeSetting,
        FreeVariableSetting = FreeVariableSetting,
        TimeCode = TimeCode,
        Duration = Duration,
        EndTime = EndTime,
        Length = Length,
        SliderVelocity = SliderVelocity,
        MoveAnchors = MoveAnchors,
        UseEndTime = UseEndTime,
        UseCurrentEditorTime = UseCurrentEditorTime,
        DelegateToBpm = DelegateToBpm,
        RemoveSliderTicks = RemoveSliderTicks
    };

    private void Install(SliderCompletionatorProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Enum.IsDefined(project.ImportModeSetting) ||
            !Enum.IsDefined(project.FreeVariableSetting) ||
            !double.IsFinite(project.Duration) ||
            !double.IsFinite(project.EndTime) ||
            !double.IsFinite(project.Length) ||
            !double.IsFinite(project.SliderVelocity))
        {
            throw new InvalidDataException("Slider Completionator project is incomplete.");
        }

        ImportModeSetting = project.ImportModeSetting;
        FreeVariableSetting = project.FreeVariableSetting;
        TimeCode = project.TimeCode ?? string.Empty;
        Duration = project.Duration;
        EndTime = project.EndTime;
        Length = project.Length;
        SliderVelocity = project.SliderVelocity;
        MoveAnchors = project.MoveAnchors;
        UseEndTime = project.UseEndTime;
        UseCurrentEditorTime = project.UseCurrentEditorTime;
        DelegateToBpm = project.DelegateToBpm;
        RemoveSliderTicks = project.RemoveSliderTicks;
    }
}
