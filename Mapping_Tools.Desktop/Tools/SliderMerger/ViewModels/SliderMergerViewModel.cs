using Mapping_Tools.Application.QuickRun.Contracts;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.SliderMerger.Models;
using Mapping_Tools.Desktop.Tools.SliderMerger.Models;
using Mapping_Tools.Desktop.Shell;

using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.SliderMerger.ViewModels;

/// <summary>
///     Owns Slider Merger form state, project persistence, ordinary runs, and
///     current-editor QuickRun routing.
/// </summary>
public sealed partial class SliderMergerViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature
{
    private readonly ICurrentBeatmapLocator currentBeatmap;

    private readonly ProjectDefinition<SliderMergerProject> definition = new(
        "slidermergerproject.json",
        "Slider Merger Projects",
        static () => new SliderMergerProject(),
        "slider-merger-project.json");

    private readonly ISliderMergerService merger;
    private readonly ApplicationSettings settings;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>
    ///     Creates a Slider Merger presentation model.
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
        : base(execution, SliderMergerToolDefinition.Definition)
    {
        this.merger = merger ?? throw new ArgumentNullException(nameof(merger));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>Gets the import modes in their legacy display order.</summary>
    public IReadOnlyList<HitObjectSelectionMode> ImportModes { get; } =
        Enum.GetValues<HitObjectSelectionMode>();

    /// <summary>Gets the path connection modes in display order.</summary>
    public IReadOnlyList<SliderMergerConnectionMode> ConnectionModes { get; } =
        Enum.GetValues<SliderMergerConnectionMode>();

    /// <summary>Gets or sets the source-object import mode.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeCodeVisible))]
    public partial HitObjectSelectionMode ImportModeSetting { get; set; } = HitObjectSelectionMode.Selected;

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
    public bool TimeCodeVisible => ImportModeSetting == HitObjectSelectionMode.Time;

    /// <inheritdoc />
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

    IProjectDefinition IShellProjectFeature.ProjectDefinition => definition;

    object IShellProjectFeature.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature.Install(object project)
    {
        Install((SliderMergerProject)project);
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        string? currentPath = null;
        if (ImportModeSetting == HitObjectSelectionMode.Selected) currentPath = await currentBeatmap.FindCurrentBeatmapAsync();

        var paths = ImportModeSetting == HitObjectSelectionMode.Selected
            ? string.IsNullOrWhiteSpace(currentPath) ? [] : [currentPath]
            : workspace.SelectedPaths;
        await RunPathsAsync(paths, settings.AlwaysQuickRun, CancellationToken.None);
    }

    private async Task RunPathsAsync(
        IReadOnlyList<string> paths,
        bool quick,
        CancellationToken cancellationToken)
    {
        if (paths.Count == 0) return;

        var options = Snapshot();
        await Execution.ExecuteAsync(
                new ToolExecutionRequest<SliderMergerResult>(
                Tool.Id,
                Tool.DisplayName,
                    async context =>
                    {
                        var result = await merger.MergeAsync(
                            paths,
                            options,
                            new Progress<double>(value => context.ReportProgress(
                                value,
                                "Merging sliders")),
                            context.CancellationToken);
                        string message = $"Successfully merged {result.ObjectsMerged} " + $"{(result.ObjectsMerged == 1 ? "slider" : "sliders")}!";
                        return new ToolExecutionOutput<SliderMergerResult>(
                            result,
                            quick ? null : message,
                            quick);
                    }),
                CreateProgress(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private SliderMergerProject Snapshot()
    {
        return new SliderMergerProject
        {
            ImportModeSetting = ImportModeSetting,
            TimeCode = TimeCode,
            ConnectionModeSetting = ConnectionModeSetting,
            Leniency = Leniency,
            LinearOnLinear = LinearOnLinear,
            MergeOnSliderEnd = MergeOnSliderEnd,
        };
    }

    private void Install(SliderMergerProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        ImportModeSetting = project.ImportModeSetting;
        TimeCode = project.TimeCode ?? string.Empty;
        ConnectionModeSetting = project.ConnectionModeSetting;
        Leniency = project.Leniency;
        LinearOnLinear = project.LinearOnLinear;
        MergeOnSliderEnd = project.MergeOnSliderEnd;
    }
}
