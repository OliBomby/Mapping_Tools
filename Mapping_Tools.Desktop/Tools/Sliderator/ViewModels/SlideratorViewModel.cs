using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Interactions.Converters;
using Mapping_Tools.Application.Interactions.Dialogs;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.Sliderator;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tools.Sliderator.Models;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.Sliderator.ViewModels;

/// <summary>
///     Owns Sliderator's graph, source selection, preview inputs, persistence, and
///     ordinary/QuickRun execution without retaining a reference to a view.
/// </summary>
public sealed partial class SlideratorViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature
{
    private readonly ICurrentBeatmapLocator currentBeatmap;

    private readonly ProjectDefinition<SlideratorProject> definition = new(
        "slideratorproject.json",
        "Sliderator Projects",
        static () => new SlideratorProject(),
        "sliderator-project.json");

    private readonly IDialogService dialogs;
    private readonly ApplicationSettings settings;

    private readonly ISlideratorService sliderator;
    private readonly IBeatmapWorkspace workspace;
    private GraphState? acceptedGraphState;
    private GraphState graphState = SlideratorEngineOptions.CreatePositionGraph(3);
    private bool settingGraphState;
    private bool synchronizingGraphBounds;

    /// <summary>
    ///     Creates a Sliderator presentation model.
    /// </summary>
    /// <param name="sliderator">Runs the Core engine through Application ports.</param>
    /// <param name="execution">Coordinates background work, cancellation, and notifications.</param>
    /// <param name="currentBeatmap">Finds the map currently open in osu!.</param>
    /// <param name="workspace">Supplies the shell's selected beatmap paths for disk-based imports.</param>
    /// <param name="settings">Supplies the AlwaysQuickRun preference.</param>
    /// <param name="dialogs">Presents validation, confirmation, and scaling dialogs.</param>
    public SlideratorViewModel(
        ISlideratorService sliderator,
        IToolExecutionService execution,
        ICurrentBeatmapLocator currentBeatmap,
        IBeatmapWorkspace workspace,
        ApplicationSettings settings,
        IDialogService dialogs)
        : base(execution, SlideratorToolDefinition.Definition)
    {
        this.sliderator = sliderator ?? throw new ArgumentNullException(nameof(sliderator));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        ImportCommand = new AsyncRelayCommand(ImportAsync);
        MoveLeftCommand = new AsyncRelayCommand(() => MoveLeftAsync(false));
        MoveRightCommand = new AsyncRelayCommand(() => MoveRightAsync(false));
        GraphToggleCommand = new RelayCommand(ToggleGraphMode);
        ClearGraphCommand = new AsyncRelayCommand(ClearGraphAsync);
        ScaleCompleteCommand = new AsyncRelayCommand(ScaleCompleteAsync);
        LoadedHitObjects.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(VisibleHitObject));
            OnPropertyChanged(nameof(ExpectedSegments));
        };
        acceptedGraphState = GraphState.Clone();
        UpdateGraphDerivedValues();
    }

    /// <summary>Gets the source import modes in legacy display order.</summary>
    public IReadOnlyList<HitObjectSelectionMode> ImportModes { get; } =
        Enum.GetValues<HitObjectSelectionMode>();

    /// <summary>Gets the export modes in legacy display order.</summary>
    public IReadOnlyList<SlideratorExportMode> ExportModes { get; } =
        Enum.GetValues<SlideratorExportMode>();

    /// <summary>Gets the graph modes in legacy display order.</summary>
    public IReadOnlyList<SlideratorGraphMode> GraphModes { get; } =
        Enum.GetValues<SlideratorGraphMode>();

    /// <summary>Gets or sets the selection source used by Import.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeCodeVisible))]
    public partial HitObjectSelectionMode ImportModeSetting { get; set; } = HitObjectSelectionMode.Selected;

    /// <summary>Gets or sets the time-code selection expression.</summary>
    [ObservableProperty]
    public partial string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the displayed source slider index.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleHitObject))]
    public partial int VisibleHitObjectIndex { get; set; }

    /// <summary>Gets the imported slider candidates.</summary>
    public ObservableCollection<HitObject> LoadedHitObjects { get; } = [];

    /// <summary>Gets the slider currently used by the preview and export.</summary>
    public HitObject? VisibleHitObject =>
        VisibleHitObjectIndex >= 0 && VisibleHitObjectIndex < LoadedHitObjects.Count
            ? LoadedHitObjects[VisibleHitObjectIndex]
            : null;

    /// <summary>Gets or sets the map's global slider multiplier.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0.4, 3.6, ErrorMessage = "Global SV must be between 0.4 and 3.6.")]
    public partial double GlobalSv { get; set; } = 1.4;

    /// <summary>Gets or sets the graph duration in beats.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, 10000, ErrorMessage = "Beat length must be between 0 and 10000.")]
    public partial double GraphBeats { get; set; } = 3;

    /// <summary>Gets or sets the graph playback BPM.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(double.Epsilon, double.MaxValue, ErrorMessage = "BPM must be greater than zero.")]
    public partial double BeatsPerMinute { get; set; } = 180;

    /// <summary>Gets or sets the graph-to-preview slider pixel length.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SvGraphMultiplier))]
    [NotifyPropertyChangedFor(nameof(ExpectedSegments))]
    public partial double PixelLength { get; private set; } = 100;

    /// <summary>Gets or sets the timestamp to which the generated object is exported.</summary>
    [ObservableProperty]
    public partial double ExportTime { get; set; }

    /// <summary>Gets or sets whether export adds or replaces the object at the timestamp.</summary>
    [ObservableProperty]
    public partial SlideratorExportMode ExportModeSetting { get; set; } = SlideratorExportMode.Add;

    /// <summary>Gets or sets whether the graph is interpreted as position or velocity.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpectedSegments))]
    public partial SlideratorGraphMode GraphModeSetting { get; set; } = SlideratorGraphMode.Position;

    /// <summary>Gets or sets the stream beat subdivision.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, 16, ErrorMessage = "Beat snap divisor must be between 1 and 16.")]
    [NotifyPropertyChangedFor(nameof(ExpectedSegments))]
    public partial int BeatSnapDivisor { get; set; } = 4;

    /// <summary>Gets or sets the self-imposed normal-slider SV limit.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, 100000, ErrorMessage = "SV limit must be between 0 and 100000.")]
    public partial double VelocityLimit { get; set; } = 10;

    /// <summary>Gets or sets whether red source anchors are drawn in the preview.</summary>
    [ObservableProperty]
    public partial bool ShowRedAnchors { get; set; }

    /// <summary>Gets or sets whether graph anchors are drawn in the preview.</summary>
    [ObservableProperty]
    public partial bool ShowGraphAnchors { get; set; }

    /// <summary>Gets or sets whether the new SV is manually controlled.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpectedSegments))]
    public partial bool ManualVelocity { get; set; }

    /// <summary>Gets or sets the selected SV used by Sliderator's optimizer.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, double.MaxValue, ErrorMessage = "New SV must be non-negative.")]
    [NotifyPropertyChangedFor(nameof(ExpectedSegments))]
    public partial double NewVelocity { get; set; } = 1;

    /// <summary>Gets or sets the minimum normal-slider dendrite length.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, 12, ErrorMessage = "Minimum tumour length must be between 1 and 12.")]
    public partial double MinDendrite { get; set; } = 2;

    /// <summary>Gets or sets whether output velocity is delegated to BPM redlines.</summary>
    [ObservableProperty]
    public partial bool DelegateToBpm { get; set; }

    /// <summary>Gets or sets whether delegated output suppresses slider ticks.</summary>
    [ObservableProperty]
    public partial bool RemoveSliderTicks { get; set; }

    /// <summary>Gets or sets the normal-slider format radio state.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpectedSegments))]
    public partial bool ExportAsNormal { get; set; } = true;

    /// <summary>Gets or sets the stream format radio state.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpectedSegments))]
    public partial bool ExportAsStream { get; set; }

    /// <summary>Gets or sets the invisible-slider format radio state.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpectedSegments))]
    public partial bool ExportAsInvisibleSlider { get; set; }

    /// <summary>Gets or sets the shared Core graph state.</summary>
    public GraphState GraphState
    {
        get => graphState;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!settingGraphState) ClipGraphAnchorToVelocityLimit(value);

            if (ReferenceEquals(graphState, value)) return;

            graphState = value;
            acceptedGraphState = value.Clone();
            double graphWidth = value.MaxX - value.MinX;
            if (double.IsFinite(graphWidth) && !Precision.AlmostEquals(GraphBeats, graphWidth))
            {
                synchronizingGraphBounds = true;
                try
                {
                    GraphBeats = graphWidth;
                }
                finally
                {
                    synchronizingGraphBounds = false;
                }
            }

            OnPropertyChanged(nameof(GraphState));
            OnPropertyChanged(nameof(ExpectedSegments));
            UpdateGraphDerivedValues();
        }
    }

    /// <summary>Gets the graph's display label.</summary>
    public string GraphModeText => GraphModeSetting == SlideratorGraphMode.Position ? "X" : "V";

    /// <summary>Gets whether the graph's horizontal axis is visible in velocity mode.</summary>
    public bool HorizontalAxisVisible => GraphModeSetting == SlideratorGraphMode.Velocity;

    /// <summary>Gets whether the graph's vertical axis is visible.</summary>
    public bool VerticalAxisVisible => false;

    /// <summary>Gets whether the position graph locks its first anchor to zero.</summary>
    public bool StartPointLockedY => GraphModeSetting == SlideratorGraphMode.Position;

    /// <summary>Gets the graph minimum Y bound for the active mode.</summary>
    public double GraphMinY => GraphModeSetting == SlideratorGraphMode.Position ? 0 : -VelocityLimit;

    /// <summary>Gets the graph maximum Y bound for the active mode.</summary>
    public double GraphMaxY => GraphModeSetting == SlideratorGraphMode.Position ? 1 : VelocityLimit;

    /// <summary>Gets the optional custom slider length used by the presentation preview.</summary>
    public double? PreviewPixelLength { get; private set; }

    /// <summary>Gets the preview progress, normalized to the generated source path.</summary>
    public double PreviewProgress { get; private set; } = -1;

    /// <summary>Gets the calculated graph duration in milliseconds.</summary>
    public double GraphDuration => GraphBeats / BeatsPerMinute * 60000;

    /// <summary>Gets the conversion from graph SV units to preview completion.</summary>
    public double SvGraphMultiplier => 100 * GlobalSv / PixelLength;

    /// <summary>Gets whether the time-code input is relevant for the selected mode.</summary>
    public bool TimeCodeVisible => ImportModeSetting == HitObjectSelectionMode.Time;

    /// <summary>Gets the estimated output segment/object count.</summary>
    public long ExpectedSegments
    {
        get
        {
            if (ExportAsStream) return (long)(GraphBeats * BeatSnapDivisor) + 1;

            if (ExportAsInvisibleSlider) return 16 + 7 * ((long)GraphDuration - 1);

            return (long)((NewVelocity * 100 * GlobalSv * GraphBeats - DistanceTraveled) / MinDendrite * 2 + DistanceTraveled / 10);
        }
    }

    /// <summary>Gets the current graph distance in preview pixels.</summary>
    public double DistanceTraveled { get; private set; }

    /// <summary>Gets the most recent imported editor state preference.</summary>
    public bool DoEditorRead { get; private set; }

    /// <summary>Gets or sets the explicit interaction boundary used for Shift navigation.</summary>
    public ISlideratorInteraction? Interaction { get; set; }

    /// <summary>Gets the import command.</summary>
    public IAsyncRelayCommand ImportCommand { get; }

    /// <summary>Gets the previous-slider command.</summary>
    public IAsyncRelayCommand MoveLeftCommand { get; }

    /// <summary>Gets the next-slider command.</summary>
    public IAsyncRelayCommand MoveRightCommand { get; }

    /// <summary>Gets the graph-mode command.</summary>
    public IRelayCommand GraphToggleCommand { get; }

    /// <summary>Gets the reset-graph command.</summary>
    public IAsyncRelayCommand ClearGraphCommand { get; }

    /// <summary>Gets the graph scaling command.</summary>
    public IAsyncRelayCommand ScaleCompleteCommand { get; }

    /// <summary>Runs the current editor map through the QuickRun import/export path.</summary>
    /// <param name="cancellationToken">Cancels editor discovery or generation.</param>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(path)) return;

        await RunWithStateAsync(async () =>
        {
            if (!await ImportForPathAsync(path, HitObjectSelectionMode.Selected, cancellationToken)) return;

            await RunPathAsync(path, true, true, cancellationToken);
        });
    }

    IProjectDefinition IShellProjectFeature.ProjectDefinition => definition;

    object IShellProjectFeature.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature.Install(object project)
    {
        Install((SlideratorProject)project);
    }

    /// <summary>Places the currently visible slider for Shift navigation without reloading the editor.</summary>
    /// <param name="cancellationToken">Cancels map discovery or placement.</param>
    /// <returns><see langword="true" /> when the placement completed successfully.</returns>
    public async Task<bool> RunFastPlacementAsync(CancellationToken cancellationToken = default)
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(path) || VisibleHitObject is null) return false;

        bool succeeded = false;
        await RunWithStateAsync(async () =>
        {
            succeeded = await RunPathAsync(path, true, false, cancellationToken);
        });
        return succeeded;
    }

    /// <summary>Runs Shift navigation after the explicit interaction boundary completes.</summary>
    /// <param name="forward">Whether to move toward the next slider.</param>
    /// <param name="fast">Whether to place the current result before moving.</param>
    /// <returns>A task completed after placement and navigation.</returns>
    public async Task MoveAsync(bool forward, bool fast)
    {
        if (fast && Interaction is not null)
            if (!await Interaction.RunFastAsync().ConfigureAwait(true))
                return;

        int next = VisibleHitObjectIndex + (forward ? 1 : -1);
        if (next < 0 || next >= LoadedHitObjects.Count)
        {
            await dialogs.ShowMessageAsync(
                new MessageDialogRequest<bool>(
                    "Sliderator",
                    forward
                        ? "You've reached the end of the slider list."
                        : "You've reached the start of the slider list.",
                    [new DialogChoice<bool>("OK", true, true, true)],
                    false));
            return;
        }

        VisibleHitObjectIndex = next;
        UpdateVisibleHitObject();
    }

    /// <summary>Runs a non-fast previous-slider navigation request.</summary>
    /// <param name="fast">Whether to place the current slider before navigating.</param>
    public Task MoveLeftAsync(bool fast)
    {
        return MoveAsync(false, fast);
    }

    /// <summary>Runs a non-fast next-slider navigation request.</summary>
    /// <param name="fast">Whether to place the current slider before navigating.</param>
    public Task MoveRightAsync(bool fast)
    {
        return MoveAsync(true, fast);
    }

    /// <summary>Checks whether a graph state stays within the configured normal-slider SV limit.</summary>
    /// <param name="state">The candidate graph state produced by the graph editor.</param>
    /// <returns><see langword="true" /> when the graph can be used for normal-slider export.</returns>
    public bool IsGraphWithinVelocityLimit(GraphState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!ExportAsNormal) return true;

        return IsVelocityWithinLimit(GetMaximumVelocity(state), VelocityLimit);
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync();
        if (string.IsNullOrWhiteSpace(path) || VisibleHitObject is null)
        {
            await ShowMessageAsync("Import a slider before running Sliderator.");
            return;
        }

        await RunPathAsync(
            path,
            settings.AlwaysQuickRun,
            settings.AlwaysQuickRun,
            CancellationToken.None);
    }

    private async Task ImportAsync()
    {
        string? path = ImportModeSetting == HitObjectSelectionMode.Selected
            ? await currentBeatmap.FindCurrentBeatmapAsync()
            : workspace.SelectedPaths.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(path))
        {
            await ShowMessageAsync(
                ImportModeSetting == HitObjectSelectionMode.Selected
                    ? "No beatmap is open in osu!."
                    : "Select a beatmap to import from.");
            return;
        }

        await ImportForPathAsync(path, ImportModeSetting, CancellationToken.None);
    }

    private async Task<bool> ImportForPathAsync(
        string path,
        HitObjectSelectionMode mode,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sliderator
                .ImportAsync(path, mode, TimeCode, cancellationToken);
            if (result.Sliders.Count == 0) return false;

            LoadedHitObjects.Clear();
            foreach (var hitObject in result.Sliders) LoadedHitObjects.Add(hitObject);

            GlobalSv = result.GlobalSv;
            DoEditorRead = result.PreferLiveEditor;
            VisibleHitObjectIndex = 0;
            UpdateVisibleHitObject();
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(exception.Message);
            return false;
        }
    }

    private async Task<bool> RunPathAsync(
        string path,
        bool quick,
        bool reloadEditor,
        CancellationToken cancellationToken)
    {
        var sourceSlider = VisibleHitObject;
        if (sourceSlider is null) return false;

        var project = Snapshot();
        bool preferLiveEditor = DoEditorRead;
        DoEditorRead = false;
        var execution = await Execution.ExecuteAsync(
            new ToolExecutionRequest<SlideratorResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    var result = await sliderator.RunAsync(
                        path,
                        project,
                        sourceSlider,
                        reloadEditor,
                        new Progress<double>(value => context.ReportProgress(value, "Sliderating")),
                        context.CancellationToken,
                        preferLiveEditor);
                    return new ToolExecutionOutput<SlideratorResult>(
                        result,
                        quick ? null : "Done!",
                        reloadEditor);
                }),
            CreateProgress(),
            cancellationToken);
        return execution.Status == ToolExecutionStatus.Succeeded && execution.Value is not null;
    }

    private SlideratorProject Snapshot()
    {
        return new SlideratorProject
        {
            ImportModeSetting = ImportModeSetting,
            TimeCode = TimeCode,
            LoadedHitObjects = LoadedHitObjects.ToList(),
            VisibleHitObjectIndex = VisibleHitObjectIndex,
            GlobalSv = GlobalSv,
            GraphBeats = GraphBeats,
            BeatsPerMinute = BeatsPerMinute,
            PixelLength = PixelLength,
            BeatSnapDivisor = BeatSnapDivisor,
            ExportTime = ExportTime,
            ExportModeSetting = ExportModeSetting,
            GraphModeSetting = GraphModeSetting,
            VelocityLimit = VelocityLimit,
            ShowRedAnchors = ShowRedAnchors,
            ShowGraphAnchors = ShowGraphAnchors,
            ManualVelocity = ManualVelocity,
            NewVelocity = NewVelocity,
            MinDendrite = MinDendrite,
            DistanceTraveled = DistanceTraveled,
            DelegateToBpm = DelegateToBpm,
            RemoveSliderTicks = RemoveSliderTicks,
            ExportAsNormal = ExportAsNormal,
            ExportAsStream = ExportAsStream,
            ExportAsInvisibleSlider = ExportAsInvisibleSlider,
            GraphState = GraphState.Clone(),
            DoEditorRead = DoEditorRead,
        };
    }

    private void Install(SlideratorProject project)
    {
        ImportModeSetting = project.ImportModeSetting;
        TimeCode = project.TimeCode ?? string.Empty;
        LoadedHitObjects.Clear();
        foreach (HitObject hitObject in project.LoadedHitObjects ?? []) LoadedHitObjects.Add(hitObject);
        VisibleHitObjectIndex = project.VisibleHitObjectIndex;
        GlobalSv = project.GlobalSv;
        GraphBeats = project.GraphBeats;
        BeatsPerMinute = project.BeatsPerMinute;
        ExportTime = project.ExportTime;
        ExportModeSetting = project.ExportModeSetting;
        GraphModeSetting = project.GraphModeSetting;
        BeatSnapDivisor = project.BeatSnapDivisor;
        VelocityLimit = project.VelocityLimit;
        ShowRedAnchors = project.ShowRedAnchors;
        ShowGraphAnchors = project.ShowGraphAnchors;
        ManualVelocity = project.ManualVelocity;
        NewVelocity = project.NewVelocity;
        MinDendrite = project.MinDendrite;
        DelegateToBpm = project.DelegateToBpm;
        RemoveSliderTicks = project.RemoveSliderTicks;
        ExportAsNormal = project.ExportAsNormal;
        ExportAsStream = project.ExportAsStream;
        ExportAsInvisibleSlider = project.ExportAsInvisibleSlider;
        DoEditorRead = project.DoEditorRead;
        GraphState state = project.GraphState.Clone();
        state.MinY = GraphMinY;
        state.MaxY = GraphMaxY;
        SetGraphState(state);
        UpdateVisibleHitObject();
        ExportTime = project.ExportTime;
    }

    private void UpdateVisibleHitObject()
    {
        var visible = VisibleHitObject;
        if (visible is not null)
        {
            PixelLength = visible.PixelLength;
            double bpm = visible.UnInheritedTimingPoint is null
                ? 180
                : 60000 / visible.UnInheritedTimingPoint.MpB;
            BeatsPerMinute = double.IsFinite(bpm) && bpm > 0 ? bpm : 180;
            GraphBeats = BeatsPerMinute * visible.TemporalLength / 60000;
            ExportTime = visible.Time;
        }

        UpdateGraphDerivedValues();
        OnPropertyChanged(nameof(VisibleHitObject));
    }

    partial void OnVisibleHitObjectIndexChanged(int value)
    {
        UpdateVisibleHitObject();
    }

    partial void OnGraphBeatsChanged(double value)
    {
        if (!synchronizingGraphBounds && double.IsFinite(value) && value >= 0)
        {
            GraphState state = GraphState.Clone();
            double oldMinX = state.MinX;
            double oldWidth = state.MaxX - oldMinX;
            state.MaxX = oldMinX + value;
            if (oldWidth > Precision.DOUBLE_EPSILON)
            {
                foreach (var anchor in state.Anchors)
                {
                    double x = oldMinX + value * (anchor.Pos.X - oldMinX) / oldWidth;
                    anchor.Pos = new Vector2((float)x, anchor.Pos.Y);
                }
            }

            SetGraphState(state);
        }

        if (VisibleHitObject is not null && double.IsFinite(value) && double.IsFinite(BeatsPerMinute) && BeatsPerMinute > 0)
            VisibleHitObject.TemporalLength = value / BeatsPerMinute * 60000;

        OnPropertyChanged(nameof(GraphDuration));
        UpdateGraphDerivedValues();
    }

    partial void OnBeatsPerMinuteChanged(double value)
    {
        if (VisibleHitObject?.UnInheritedTimingPoint is not null && double.IsFinite(value) && value > 0)
        {
            VisibleHitObject.UnInheritedTimingPoint.MpB = 60000 / value;
            VisibleHitObject.TemporalLength = GraphBeats / value * 60000;
        }

        OnPropertyChanged(nameof(GraphDuration));
        UpdateGraphDerivedValues();
    }

    partial void OnGlobalSvChanged(double value)
    {
        UpdateGraphDerivedValues();
    }

    partial void OnVelocityLimitChanged(double value)
    {
        if (GraphModeSetting == SlideratorGraphMode.Velocity)
        {
            GraphState state = GraphState.Clone();
            state.MinY = -value;
            state.MaxY = value;
            SetGraphState(state);
            return;
        }

        UpdateGraphDerivedValues();
    }

    partial void OnGraphModeSettingChanged(SlideratorGraphMode value)
    {
        OnPropertyChanged(nameof(GraphModeText));
        OnPropertyChanged(nameof(HorizontalAxisVisible));
        OnPropertyChanged(nameof(VerticalAxisVisible));
        OnPropertyChanged(nameof(StartPointLockedY));
        OnPropertyChanged(nameof(GraphMinY));
        OnPropertyChanged(nameof(GraphMaxY));
        UpdateGraphDerivedValues();
    }

    private void SetGraphState(GraphState state)
    {
        settingGraphState = true;
        try
        {
            GraphState = state;
        }
        finally
        {
            settingGraphState = false;
        }
    }

    private void ClipGraphAnchorToVelocityLimit(GraphState candidate)
    {
        if (!ExportAsNormal || acceptedGraphState is null)
            return;

        int anchorIndex = FindChangedAnchorIndex(acceptedGraphState, candidate);
        if (anchorIndex <= 0 || anchorIndex >= candidate.Anchors.Count || acceptedGraphState.Anchors.Count != candidate.Anchors.Count)
            return;

        var candidateAnchor = candidate.Anchors[anchorIndex];
        var acceptedAnchor = acceptedGraphState.Anchors[anchorIndex];
        bool positionChanged = Math.Abs(candidateAnchor.Pos.X - acceptedAnchor.Pos.X) > 1e-9
            || Math.Abs(candidateAnchor.Pos.Y - acceptedAnchor.Pos.Y) > 1e-9;
        bool tensionChanged = Math.Abs(candidateAnchor.Tension - acceptedAnchor.Tension) > 1e-9;
        bool interpolatorChanged = candidateAnchor.Interpolator.GetType() != acceptedAnchor.Interpolator.GetType();
        if (!positionChanged && !tensionChanged && !interpolatorChanged)
            return;

        // Keep an over-limit graph editable so the user can reduce its maximum
        // slope. Until it is valid again, its current maximum is the edit ceiling.
        double editVelocityLimit = Math.Max(VelocityLimit, GetMaximumVelocity(acceptedGraphState));
        if (!IsGraphOverSpeedLimit(candidate, anchorIndex, editVelocityLimit)) return;

        if (tensionChanged && !positionChanged)
        {
            ClipAnchorTensionToVelocityLimit(candidate, acceptedGraphState, anchorIndex, editVelocityLimit);
            if (IsGraphOverSpeedLimit(candidate, anchorIndex, editVelocityLimit)) CopyGraphState(acceptedGraphState, candidate);
            return;
        }

        if (GraphModeSetting != SlideratorGraphMode.Position) return;

        List<(double Min, double Max)> bounds = [];
        AddPreviousVelocityBounds(candidate, anchorIndex, bounds, editVelocityLimit);
        AddNextVelocityBounds(candidate, anchorIndex, bounds, editVelocityLimit);
        if (bounds.Count == 0) return;

        double lowerBound = bounds.Max(bound => bound.Min);
        double upperBound = bounds.Min(bound => bound.Max);
        if (lowerBound <= upperBound)
        {
            double clippedY = Math.Clamp(candidateAnchor.Pos.Y, lowerBound, upperBound);
            candidateAnchor.Pos = new Vector2(candidateAnchor.Pos.X, clippedY);
        }
        else
        {
            ClipAnchorAlongMovement(candidate, acceptedGraphState, anchorIndex, editVelocityLimit);
        }

        if (IsGraphOverSpeedLimit(candidate, anchorIndex, editVelocityLimit)) CopyGraphState(acceptedGraphState, candidate);
    }

    private void ClipAnchorTensionToVelocityLimit(
        GraphState candidate,
        GraphState accepted,
        int anchorIndex,
        double velocityLimit)
    {
        double acceptedTension = Math.Clamp(accepted.Anchors[anchorIndex].Tension, -1, 1);
        double candidateTension = Math.Clamp(candidate.Anchors[anchorIndex].Tension, -1, 1);
        double lower = 0;
        double upper = 1;
        for (int iteration = 0; iteration < 24; iteration++)
        {
            double progress = (lower + upper) / 2;
            candidate.Anchors[anchorIndex].Tension = acceptedTension + (candidateTension - acceptedTension) * progress;
            if (IsGraphOverSpeedLimit(candidate, anchorIndex, velocityLimit)) upper = progress;
            else lower = progress;
        }

        candidate.Anchors[anchorIndex].Tension = Math.Clamp(
            acceptedTension + (candidateTension - acceptedTension) * lower,
            -1,
            1);
    }

    private void ClipAnchorAlongMovement(
        GraphState candidate,
        GraphState accepted,
        int anchorIndex,
        double velocityLimit)
    {
        var acceptedPosition = accepted.Anchors[anchorIndex].Pos;
        var candidateAnchor = candidate.Anchors[anchorIndex];
        var candidatePosition = candidateAnchor.Pos;
        double lower = 0;
        double upper = 1;
        for (int iteration = 0; iteration < 24; iteration++)
        {
            double progress = (lower + upper) / 2;
            candidateAnchor.Pos = new Vector2(
                acceptedPosition.X + (candidatePosition.X - acceptedPosition.X) * progress,
                acceptedPosition.Y + (candidatePosition.Y - acceptedPosition.Y) * progress);
            if (IsGraphOverSpeedLimit(candidate, anchorIndex, velocityLimit)) upper = progress;
            else lower = progress;
        }

        candidateAnchor.Pos = new Vector2(
            acceptedPosition.X + (candidatePosition.X - acceptedPosition.X) * lower,
            acceptedPosition.Y + (candidatePosition.Y - acceptedPosition.Y) * lower);
    }

    private static int FindChangedAnchorIndex(GraphState previous, GraphState candidate)
    {
        for (int index = 0; index < candidate.Anchors.Count; index++)
        {
            if (index >= previous.Anchors.Count
                || Math.Abs(previous.Anchors[index].Pos.X - candidate.Anchors[index].Pos.X) > 1e-9
                || Math.Abs(previous.Anchors[index].Pos.Y - candidate.Anchors[index].Pos.Y) > 1e-9
                || Math.Abs(previous.Anchors[index].Tension - candidate.Anchors[index].Tension) > 1e-9
                || previous.Anchors[index].Interpolator.GetType() != candidate.Anchors[index].Interpolator.GetType())
                return index;
        }

        return -1;
    }

    private bool IsGraphOverSpeedLimit(GraphState state, int anchorIndex, double velocityLimit)
    {
        return IsAnchorOverSpeedLimit(state, anchorIndex, velocityLimit)
            || !IsVelocityWithinLimit(GetMaximumVelocity(state), velocityLimit);
    }

    private bool IsAnchorOverSpeedLimit(GraphState state, int anchorIndex, double velocityLimit)
    {
        return IsPreviousSegmentOverSpeedLimit(state, anchorIndex, velocityLimit)
            || IsNextSegmentOverSpeedLimit(state, anchorIndex, velocityLimit);
    }

    private bool IsPreviousSegmentOverSpeedLimit(GraphState state, int anchorIndex, double velocityLimit)
    {
        if (anchorIndex <= 0) return false;

        var anchor = state.Anchors[anchorIndex];
        var previous = state.Anchors[anchorIndex - 1];
        if (GraphModeSetting == SlideratorGraphMode.Velocity)
            return Math.Abs(GraphInterpolatorCatalog.GetBiggestValue(anchor.Interpolator)) > velocityLimit;

        double difference = anchor.Pos.Y - previous.Pos.Y;
        double distance = anchor.Pos.X - previous.Pos.X;
        if (!double.IsFinite(distance) || Math.Abs(distance) <= Precision.DOUBLE_EPSILON)
            return true;

        double maximumDerivative = GraphInterpolatorCatalog.GetBiggestDerivative(anchor.Interpolator);
        double velocity = Math.Abs(maximumDerivative * difference / distance) / SvGraphMultiplier;
        return !double.IsFinite(velocity) || velocity > velocityLimit + Precision.DOUBLE_EPSILON;
    }

    private bool IsNextSegmentOverSpeedLimit(GraphState state, int anchorIndex, double velocityLimit)
    {
        if (anchorIndex >= state.Anchors.Count - 1) return false;

        var anchor = state.Anchors[anchorIndex];
        var next = state.Anchors[anchorIndex + 1];
        if (GraphModeSetting == SlideratorGraphMode.Velocity)
            return Math.Abs(GraphInterpolatorCatalog.GetBiggestValue(next.Interpolator)) > velocityLimit;

        double difference = next.Pos.Y - anchor.Pos.Y;
        double distance = next.Pos.X - anchor.Pos.X;
        if (!double.IsFinite(distance) || Math.Abs(distance) <= Precision.DOUBLE_EPSILON)
            return true;

        double maximumDerivative = GraphInterpolatorCatalog.GetBiggestDerivative(next.Interpolator);
        double velocity = Math.Abs(maximumDerivative * difference / distance) / SvGraphMultiplier;
        return !double.IsFinite(velocity) || velocity > velocityLimit + Precision.DOUBLE_EPSILON;
    }

    private void AddPreviousVelocityBounds(
        GraphState state,
        int anchorIndex,
        ICollection<(double Min, double Max)> bounds,
        double velocityLimit)
    {
        var anchor = state.Anchors[anchorIndex];
        var previous = state.Anchors[anchorIndex - 1];
        double maximumDerivative = GraphInterpolatorCatalog.GetBiggestDerivative(anchor.Interpolator);
        double distance = anchor.Pos.X - previous.Pos.X;
        if (Math.Abs(distance) <= Precision.DOUBLE_EPSILON)
        {
            bounds.Add((previous.Pos.Y, previous.Pos.Y));
            return;
        }

        double allowedDifference = velocityLimit * SvGraphMultiplier * distance / maximumDerivative;
        bounds.Add((
            previous.Pos.Y + Precision.DOUBLE_EPSILON - allowedDifference,
            previous.Pos.Y - Precision.DOUBLE_EPSILON + allowedDifference));
    }

    private void AddNextVelocityBounds(
        GraphState state,
        int anchorIndex,
        ICollection<(double Min, double Max)> bounds,
        double velocityLimit)
    {
        if (anchorIndex >= state.Anchors.Count - 1) return;

        var anchor = state.Anchors[anchorIndex];
        var next = state.Anchors[anchorIndex + 1];
        double maximumDerivative = GraphInterpolatorCatalog.GetBiggestDerivative(next.Interpolator);
        double distance = next.Pos.X - anchor.Pos.X;
        if (Math.Abs(distance) <= Precision.DOUBLE_EPSILON)
        {
            bounds.Add((next.Pos.Y, next.Pos.Y));
            return;
        }

        double allowedDifference = velocityLimit * SvGraphMultiplier * distance / maximumDerivative;
        bounds.Add((
            next.Pos.Y + Precision.DOUBLE_EPSILON - allowedDifference,
            next.Pos.Y - Precision.DOUBLE_EPSILON + allowedDifference));
    }

    private double GetMaximumVelocity(GraphState state)
    {
        SlideratorEngineOptions options = new()
        {
            GlobalSv = GlobalSv,
            PixelLength = PixelLength,
            GraphModeSetting = GraphModeSetting,
            GraphState = state,
        };
        return SlideratorEngine.GetMaximumVelocity(options);
    }

    private static bool IsVelocityWithinLimit(double velocity, double velocityLimit)
    {
        return velocity <= velocityLimit + Precision.DOUBLE_EPSILON;
    }

    private static void CopyGraphState(GraphState source, GraphState target)
    {
        target.Anchors = source.Anchors.Select(anchor => anchor.Clone()).ToList();
        target.MinX = source.MinX;
        target.MinY = source.MinY;
        target.MaxX = source.MaxX;
        target.MaxY = source.MaxY;
    }

    private void UpdateGraphDerivedValues()
    {
        SlideratorEngineOptions options = new()
        {
            GlobalSv = GlobalSv,
            GraphBeats = GraphBeats,
            BeatsPerMinute = BeatsPerMinute,
            PixelLength = PixelLength,
            GraphModeSetting = GraphModeSetting,
            GraphState = GraphState,
        };
        DistanceTraveled = GraphModeSetting == SlideratorGraphMode.Velocity
            ? GraphState.GetIntegral(0, GraphBeats) * SvGraphMultiplier * PixelLength
            : GraphMath.GetDistanceTraveled(GraphState.Anchors) * PixelLength;
        if (!ManualVelocity) NewVelocity = SlideratorEngine.GetMaximumVelocity(options);

        OnPropertyChanged(nameof(DistanceTraveled));
        OnPropertyChanged(nameof(SvGraphMultiplier));
        OnPropertyChanged(nameof(ExpectedSegments));
        UpdatePreview();
    }

    /// <summary>Evaluates the Core graph at one repeating animation timestamp.</summary>
    /// <param name="elapsedMilliseconds">Elapsed preview time across the graph and its one-second hold.</param>
    /// <returns>A normalized preview progress value, or <c>-1</c> during the hold.</returns>
    public double EvaluatePreviewProgress(double elapsedMilliseconds)
    {
        double duration = GraphDuration;
        if (!double.IsFinite(duration) || duration <= 0 || elapsedMilliseconds < 0) return 0;

        double cycleDuration = duration + 1000;
        double cycleMilliseconds = elapsedMilliseconds % cycleDuration;
        if (cycleMilliseconds >= duration) return -1;

        double graphMilliseconds = cycleMilliseconds;
        double graphValue = GraphModeSetting == SlideratorGraphMode.Velocity
            ? GraphState.GetIntegral(0, graphMilliseconds * BeatsPerMinute / 60000) * SvGraphMultiplier
            : GraphState.GetValue(graphMilliseconds * BeatsPerMinute / 60000);
        double maximum = SlideratorEngine.GetMaxCompletion(new SlideratorEngineOptions
        {
            GlobalSv = GlobalSv,
            PixelLength = PixelLength,
            GraphModeSetting = GraphModeSetting,
            GraphState = GraphState,
        });
        return maximum <= Precision.DOUBLE_EPSILON
            ? 0
            : Math.Clamp(graphValue / maximum, 0, 1);
    }

    /// <summary>Sets the animation progress used by the shared preview control.</summary>
    /// <param name="progress">Normalized progress, or a negative value to hide the ball.</param>
    public void SetPreviewProgress(double progress)
    {
        PreviewProgress = progress;
        OnPropertyChanged(nameof(PreviewProgress));
    }

    private void UpdatePreview()
    {
        var visible = VisibleHitObject;
        if (visible is null)
        {
            PreviewPixelLength = null;
            OnPropertyChanged(nameof(PreviewPixelLength));
            return;
        }

        SlideratorEngineOptions options = new()
        {
            GlobalSv = GlobalSv,
            PixelLength = PixelLength,
            GraphModeSetting = GraphModeSetting,
            GraphState = GraphState,
        };
        double customLength = SlideratorEngine.GetMaxCompletion(options) * PixelLength;
        PreviewPixelLength = double.IsFinite(customLength) && customLength >= 0 ? customLength : null;
        OnPropertyChanged(nameof(PreviewPixelLength));
    }

    private void ToggleGraphMode()
    {
        SlideratorGraphMode mode = GraphModeSetting == SlideratorGraphMode.Position
            ? SlideratorGraphMode.Velocity
            : SlideratorGraphMode.Position;

        GraphState state = GraphState.Clone();
        state.MinY = mode == SlideratorGraphMode.Position ? 0 : -VelocityLimit;
        state.MaxY = mode == SlideratorGraphMode.Position ? 1 : VelocityLimit;
        if (mode == SlideratorGraphMode.Position && state.Anchors.Count > 0)
            state.Anchors[0].Pos = new Vector2(state.Anchors[0].Pos.X, 0);

        GraphModeSetting = mode;
        SetGraphState(state);
    }

    private GraphState CreateResetGraphState()
    {
        if (GraphModeSetting == SlideratorGraphMode.Position)
            return SlideratorEngineOptions.CreatePositionGraph(GraphBeats);

        return new GraphState(
            [
                new GraphAnchor(new Vector2(0, 1)),
                new GraphAnchor(new Vector2(GraphBeats, 1)),
            ],
            0,
            -VelocityLimit,
            GraphBeats,
            VelocityLimit);
    }

    private async Task ClearGraphAsync()
    {
        bool confirmed = await dialogs.ShowMessageAsync(
            new MessageDialogRequest<bool>(
                "Confirm deletion",
                "Clear the graph?",
                [
                    new DialogChoice<bool>("Delete", true, true),
                    new DialogChoice<bool>("Cancel", false, IsCancel: true),
                ],
                false));
        if (!confirmed) return;

        SetGraphState(CreateResetGraphState());
    }

    private async Task ScaleCompleteAsync()
    {
        double maximum = GraphModeSetting == SlideratorGraphMode.Velocity
            ? GraphState.GetMaxIntegral()
            : GraphState.GetMaxValue();
        if (Math.Abs(maximum) < Precision.DOUBLE_EPSILON) return;

        var result = await dialogs.ShowValueAsync(
            new ValueDialogRequest<double>(
                "Scale graph",
                "Scale graph maximum to:",
                1,
                new InvariantDoubleConverter()));
        if (!result.Accepted || !double.IsFinite(result.Value)) return;

        double target = result.Value;

        GraphState state = GraphState.Clone();
        foreach (var anchor in state.Anchors) anchor.Pos = new Vector2(anchor.Pos.X, (float)(anchor.Pos.Y * target / maximum));

        SetGraphState(state);
    }

    private async Task ShowMessageAsync(string message)
    {
        await dialogs.ShowMessageAsync(
            new MessageDialogRequest<bool>(
                "Sliderator",
                message,
                [new DialogChoice<bool>("OK", true, true, true)],
                false));
    }
}
