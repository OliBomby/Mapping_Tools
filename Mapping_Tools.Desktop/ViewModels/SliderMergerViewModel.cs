using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.SliderMerger;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.SliderMerger;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Owns Slider Merger form state, project persistence, ordinary runs, and
/// current-editor QuickRun routing.
/// </summary>
public sealed partial class SliderMergerViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature
{
    internal const string OperationId = "slider-merger";

    private readonly ISliderMergerService _merger;
    private readonly ICurrentBeatmapLocator _currentBeatmap;
    private readonly IBeatmapWorkspace _workspace;
    private readonly ApplicationSettings _settings;
    private readonly ProjectDefinition<SliderMergerProject> _definition = new(
        "slidermergerproject.json",
        "Slider Merger Projects",
        static () => new SliderMergerProject(),
        "slider-merger-project.json");

    /// <summary>Gets the import modes in their legacy display order.</summary>
    public IReadOnlyList<SliderMergerImportMode> ImportModes { get; } =
        Enum.GetValues<SliderMergerImportMode>();

    /// <summary>Gets the path connection modes in display order.</summary>
    public IReadOnlyList<SliderMergerConnectionMode> ConnectionModes { get; } =
        Enum.GetValues<SliderMergerConnectionMode>();

    /// <summary>Gets or sets the source-object import mode.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeCodeVisible))]
    public partial SliderMergerImportMode ImportModeSetting { get; set; } = SliderMergerImportMode.Selected;

    /// <summary>Gets or sets the legacy time-code query.</summary>
    [ObservableProperty]
    public partial string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets how adjacent paths are joined.</summary>
    [ObservableProperty]
    public partial SliderMergerConnectionMode ConnectionModeSetting { get; set; } =
        SliderMergerConnectionMode.Move;

    /// <summary>Gets or sets the non-negative object connection tolerance in osu! pixels.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, double.MaxValue, ErrorMessage = "Enter a finite non-negative leniency.")]
    public partial double Leniency { get; set; } = 256;

    /// <summary>Gets or sets whether a fully linear merge uses the linear path type.</summary>
    [ObservableProperty]
    public partial bool LinearOnLinear { get; set; }

    /// <summary>Gets or sets whether matching uses a slider's playable end.</summary>
    [ObservableProperty]
    public partial bool MergeOnSliderEnd { get; set; } = true;

    /// <summary>Gets whether the time-code field is visible for Time import mode.</summary>
    public bool TimeCodeVisible => ImportModeSetting == SliderMergerImportMode.Time;

    /// <summary>
    /// Creates a Slider Merger presentation model.
    /// </summary>
    /// <param name="merger">Runs the framework-independent merge transformation.</param>
    /// <param name="execution">Coordinates background execution, cancellation, and reload.</param>
    /// <param name="currentBeatmap">Finds the beatmap currently open in osu!.</param>
    /// <param name="workspace">Supplies the shell's selected beatmap paths.</param>
    /// <param name="settings">Supplies the legacy Always QuickRun preference.</param>
    public SliderMergerViewModel(
        ISliderMergerService merger,
        IToolExecutionService execution,
        ICurrentBeatmapLocator currentBeatmap,
        IBeatmapWorkspace workspace,
        ApplicationSettings settings)
        : base(execution, OperationId)
    {
        _merger = merger ?? throw new ArgumentNullException(nameof(merger));
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
        if (ImportModeSetting == SliderMergerImportMode.Selected)
        {
            currentPath = await _currentBeatmap.FindCurrentBeatmapAsync();
        }

        IReadOnlyList<string> paths = ImportModeSetting == SliderMergerImportMode.Selected
            ? string.IsNullOrWhiteSpace(currentPath) ? [] : [currentPath]
            : _workspace.SelectedPaths;
        await RunPathsAsync(paths, _settings.AlwaysQuickRun, CancellationToken.None);
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

    void IShellProjectFeature.Install(object project) => Install((SliderMergerProject)project);

    private async Task RunPathsAsync(
        IReadOnlyList<string> paths,
        bool quick,
        CancellationToken cancellationToken)
    {
        if (paths.Count == 0)
        {
            return;
        }

        SliderMergerProject options = Snapshot();
        await Execution.ExecuteAsync(
                new ToolExecutionRequest<SliderMergerResult>(
                    OperationId,
                    "Slider Merger",
                    async context =>
                    {
                        SliderMergerResult result = await _merger.MergeAsync(
                            paths,
                            options,
                            new Progress<double>(value => context.ReportProgress(
                                value,
                                "Merging sliders")),
                            context.CancellationToken);
                        string message = $"Successfully merged {result.ObjectsMerged} " +
                                         $"{(result.ObjectsMerged == 1 ? "slider" : "sliders")}!";
                        return new ToolExecutionOutput<SliderMergerResult>(
                            result,
                            quick ? null : message,
                            reloadEditor: quick);
                    }),
                CreateProgress(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private SliderMergerProject Snapshot() => new()
    {
        ImportModeSetting = ImportModeSetting,
        TimeCode = TimeCode,
        ConnectionModeSetting = ConnectionModeSetting,
        Leniency = Leniency,
        LinearOnLinear = LinearOnLinear,
        MergeOnSliderEnd = MergeOnSliderEnd
    };

    private void Install(SliderMergerProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Enum.IsDefined(project.ImportModeSetting) ||
            !Enum.IsDefined(project.ConnectionModeSetting) ||
            !double.IsFinite(project.Leniency) ||
            project.Leniency < 0)
        {
            throw new InvalidDataException("Slider Merger project is incomplete.");
        }

        ImportModeSetting = project.ImportModeSetting;
        TimeCode = project.TimeCode ?? string.Empty;
        ConnectionModeSetting = project.ConnectionModeSetting;
        Leniency = project.Leniency;
        LinearOnLinear = project.LinearOnLinear;
        MergeOnSliderEnd = project.MergeOnSliderEnd;
    }
}
