using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Interactions.Converters;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Sliderator;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.Graph;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Tools.Sliderator;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Owns Sliderator's graph, source selection, preview inputs, persistence, and
/// ordinary/QuickRun execution without retaining a reference to a view.
/// </summary>
public sealed partial class SlideratorViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature
{
    internal const string OperationId = "sliderator";

    private readonly ISlideratorService sliderator;
    private readonly ICurrentBeatmapLocator currentBeatmap;
    private readonly ApplicationSettings settings;
    private readonly IDialogService dialogs;
    private readonly ProjectDefinition<SlideratorProject> definition = new(
        "slideratorproject.json",
        "Sliderator Projects",
        static () => new SlideratorProject(),
        "sliderator-project.json");

    /// <summary>Gets the source import modes in legacy display order.</summary>
    public IReadOnlyList<SlideratorImportMode> ImportModes { get; } =
        Enum.GetValues<SlideratorImportMode>();

    /// <summary>Gets the export modes in legacy display order.</summary>
    public IReadOnlyList<SlideratorExportMode> ExportModes { get; } =
        Enum.GetValues<SlideratorExportMode>();

    /// <summary>Gets the graph modes in legacy display order.</summary>
    public IReadOnlyList<SlideratorGraphMode> GraphModes { get; } =
        Enum.GetValues<SlideratorGraphMode>();

    /// <summary>Gets or sets the selection source used by Import.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeCodeVisible))]
    public partial SlideratorImportMode ImportModeSetting { get; set; } = SlideratorImportMode.Selected;

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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpectedSegments))]
    public partial GraphState GraphState { get; set; } = SlideratorOptions.CreatePositionGraph(3);

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
    public bool TimeCodeVisible => ImportModeSetting == SlideratorImportMode.Time;

    /// <summary>Gets the estimated output segment/object count.</summary>
    public long ExpectedSegments
    {
        get
        {
            if (ExportAsStream)
            {
                return (long)(GraphBeats * BeatSnapDivisor) + 1;
            }

            if (ExportAsInvisibleSlider)
            {
                return 16 + 7 * ((long)GraphDuration - 1);
            }

            return (long)((NewVelocity * 100 * GlobalSv * GraphBeats - DistanceTraveled) /
                           MinDendrite * 2 + DistanceTraveled / 10);
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

    /// <summary>
    /// Creates a Sliderator presentation model.
    /// </summary>
    /// <param name="sliderator">Runs the Core engine through Application ports.</param>
    /// <param name="execution">Coordinates background work, cancellation, and notifications.</param>
    /// <param name="currentBeatmap">Finds the map currently open in osu!.</param>
    /// <param name="settings">Supplies the AlwaysQuickRun preference.</param>
    /// <param name="dialogs">Presents validation, confirmation, and scaling dialogs.</param>
    public SlideratorViewModel(
        ISlideratorService sliderator,
        IToolExecutionService execution,
        ICurrentBeatmapLocator currentBeatmap,
        ApplicationSettings settings,
        IDialogService dialogs)
        : base(execution, OperationId)
    {
        this.sliderator = sliderator ?? throw new ArgumentNullException(nameof(sliderator));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
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
        UpdateGraphDerivedValues();
    }

    /// <summary>Runs the current editor map through the QuickRun import/export path.</summary>
    /// <param name="cancellationToken">Cancels editor discovery or generation.</param>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await RunWithStateAsync(async () =>
        {
            if (!await ImportForPathAsync(path, SlideratorImportMode.Selected, cancellationToken))
            {
                return;
            }

            await RunPathAsync(path, quick: true, reloadEditor: true, cancellationToken);
        });
    }

    /// <summary>Places the currently visible slider for Shift navigation without reloading the editor.</summary>
    /// <param name="cancellationToken">Cancels map discovery or placement.</param>
    /// <returns><see langword="true"/> when the placement completed successfully.</returns>
    public async Task<bool> RunFastPlacementAsync(CancellationToken cancellationToken = default)
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(path) || VisibleHitObject is null)
        {
            return false;
        }

        bool succeeded = false;
        await RunWithStateAsync(async () =>
        {
            succeeded = await RunPathAsync(path, quick: true, reloadEditor: false, cancellationToken);
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
        {
            if (!await Interaction.RunFastAsync().ConfigureAwait(true))
            {
                return;
            }
        }

        int next = VisibleHitObjectIndex + (forward ? 1 : -1);
        if (next < 0 || next >= LoadedHitObjects.Count)
        {
            await dialogs.ShowMessageAsync(
                new MessageDialogRequest<bool>(
                    "Sliderator",
                    forward
                        ? "You've reached the end of the slider list."
                        : "You've reached the start of the slider list.",
                    [new DialogChoice<bool>("OK", true, IsDefault: true, IsCancel: true)],
                    false));
            return;
        }

        VisibleHitObjectIndex = next;
        UpdateVisibleHitObject();
    }

    /// <summary>Runs a non-fast previous-slider navigation request.</summary>
    /// <param name="fast">Whether to place the current slider before navigating.</param>
    public Task MoveLeftAsync(bool fast) => MoveAsync(false, fast);

    /// <summary>Runs a non-fast next-slider navigation request.</summary>
    /// <param name="fast">Whether to place the current slider before navigating.</param>
    public Task MoveRightAsync(bool fast) => MoveAsync(true, fast);

    /// <summary>Refreshes the preview geometry after the graph control changes state.</summary>
    /// <param name="state">The state emitted by the shared graph control.</param>
    public void ApplyGraphState(GraphState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        GraphState = state.Clone();
        UpdateGraphDerivedValues();
    }

    /// <summary>Checks whether a graph state stays within the configured normal-slider SV limit.</summary>
    /// <param name="state">The candidate graph state produced by the graph editor.</param>
    /// <returns><see langword="true"/> when the graph can be used for normal-slider export.</returns>
    public bool IsGraphWithinVelocityLimit(GraphState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!ExportAsNormal)
        {
            return true;
        }

        SlideratorOptions options = new()
        {
            GlobalSv = GlobalSv,
            PixelLength = PixelLength,
            GraphModeSetting = GraphModeSetting,
            GraphState = state
        };
        return SlideratorEngine.GetMaximumVelocity(options) <= VelocityLimit + Precision.DoubleEpsilon;
    }

    /// <inheritdoc/>
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
                reloadEditor: settings.AlwaysQuickRun,
                CancellationToken.None);
    }

    /// <inheritdoc/>
    protected override bool PrepareRun()
    {
        ValidateAllProperties();
        return !HasErrors && LoadedHitObjects.Count > 0 && VisibleHitObject is not null;
    }

    string IQuickRun.OperationId => OperationId;

    IProjectDefinition IShellProjectFeature.ProjectDefinition => definition;

    object IShellProjectFeature.Snapshot() => Snapshot();

    void IShellProjectFeature.Install(object project) => Install((SlideratorProject)project);

    private async Task ImportAsync()
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            await ShowMessageAsync("No beatmap is open in osu!.");
            return;
        }

        await ImportForPathAsync(path, ImportModeSetting, CancellationToken.None);
    }

    private async Task<bool> ImportForPathAsync(
        string path,
        SlideratorImportMode mode,
        CancellationToken cancellationToken)
    {
        try
        {
            SlideratorImportResult result = await sliderator
                .ImportAsync(path, mode, TimeCode, cancellationToken);
            if (result.Sliders.Count == 0)
            {
                return false;
            }

            LoadedHitObjects.Clear();
            foreach (HitObject hitObject in result.Sliders)
            {
                LoadedHitObjects.Add(hitObject);
            }

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
        HitObject? sourceSlider = VisibleHitObject;
        if (sourceSlider is null)
        {
            return false;
        }

        SlideratorProject project = Snapshot();
        bool preferLiveEditor = DoEditorRead;
        DoEditorRead = false;
        ToolExecutionResult<SlideratorResult> execution = await Execution.ExecuteAsync(
                new ToolExecutionRequest<SlideratorResult>(
                    OperationId,
                    "Sliderator",
                    async context =>
                    {
                        SlideratorResult result = await sliderator.RunAsync(
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
        };
    }

    private void Install(SlideratorProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Enum.IsDefined(project.ImportModeSetting) ||
            !Enum.IsDefined(project.ExportModeSetting) ||
            !Enum.IsDefined(project.GraphModeSetting) ||
            !double.IsFinite(project.GlobalSv) ||
            !double.IsFinite(project.GraphBeats) ||
            !double.IsFinite(project.BeatsPerMinute) ||
            !double.IsFinite(project.VelocityLimit) ||
            !double.IsFinite(project.NewVelocity) ||
            !double.IsFinite(project.MinDendrite) ||
            project.GraphState is null)
        {
            throw new InvalidDataException("Sliderator project is incomplete.");
        }

        ImportModeSetting = project.ImportModeSetting;
        TimeCode = project.TimeCode ?? string.Empty;
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
        GraphState = project.GraphState.Clone();
        LoadedHitObjects.Clear();
        VisibleHitObjectIndex = 0;
        DoEditorRead = false;
        UpdateVisibleHitObject();
        ExportTime = project.ExportTime;
    }

    private void UpdateVisibleHitObject()
    {
        HitObject? visible = VisibleHitObject;
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

    partial void OnVisibleHitObjectIndexChanged(int value) => UpdateVisibleHitObject();

    partial void OnGraphBeatsChanged(double value)
    {
        if (VisibleHitObject is not null && double.IsFinite(value) && double.IsFinite(BeatsPerMinute) && BeatsPerMinute > 0)
        {
            VisibleHitObject.TemporalLength = value / BeatsPerMinute * 60000;
        }

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

    partial void OnGlobalSvChanged(double value) => UpdateGraphDerivedValues();
    partial void OnVelocityLimitChanged(double value) => UpdateGraphDerivedValues();
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
    partial void OnGraphStateChanged(GraphState value) => UpdateGraphDerivedValues();

    private void UpdateGraphDerivedValues()
    {
        SlideratorOptions options = new()
        {
            GlobalSv = GlobalSv,
            GraphBeats = GraphBeats,
            BeatsPerMinute = BeatsPerMinute,
            PixelLength = PixelLength,
            GraphModeSetting = GraphModeSetting,
            GraphState = GraphState
        };
        DistanceTraveled = GraphModeSetting == SlideratorGraphMode.Velocity
            ? GraphState.GetIntegral(0, GraphBeats) * SvGraphMultiplier * PixelLength
            : GraphMath.GetDistanceTraveled(GraphState.Anchors) * PixelLength;
        if (!ManualVelocity)
        {
            NewVelocity = SlideratorEngine.GetMaximumVelocity(options);
        }

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
        if (!double.IsFinite(duration) || duration <= 0 || elapsedMilliseconds < 0)
        {
            return 0;
        }

        double cycleDuration = duration + 1000;
        double cycleMilliseconds = elapsedMilliseconds % cycleDuration;
        if (cycleMilliseconds >= duration)
        {
            return -1;
        }

        double graphMilliseconds = cycleMilliseconds;
        double graphValue = GraphModeSetting == SlideratorGraphMode.Velocity
            ? GraphState.GetIntegral(0, graphMilliseconds * BeatsPerMinute / 60000) * SvGraphMultiplier
            : GraphState.GetValue(graphMilliseconds * BeatsPerMinute / 60000);
        double maximum = SlideratorEngine.GetMaxCompletion(new SlideratorOptions
        {
            GlobalSv = GlobalSv,
            PixelLength = PixelLength,
            GraphModeSetting = GraphModeSetting,
            GraphState = GraphState
        });
        return maximum <= Precision.DoubleEpsilon
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
        HitObject? visible = VisibleHitObject;
        if (visible is null)
        {
            PreviewPixelLength = null;
            OnPropertyChanged(nameof(PreviewPixelLength));
            return;
        }

        SlideratorOptions options = new()
        {
            GlobalSv = GlobalSv,
            PixelLength = PixelLength,
            GraphModeSetting = GraphModeSetting,
            GraphState = GraphState
        };
        double customLength = SlideratorEngine.GetMaxCompletion(options) * PixelLength;
        PreviewPixelLength = double.IsFinite(customLength) && customLength >= 0 ? customLength : null;
        OnPropertyChanged(nameof(PreviewPixelLength));
    }

    private void ToggleGraphMode()
    {
        GraphModeSetting = GraphModeSetting == SlideratorGraphMode.Position
            ? SlideratorGraphMode.Velocity
            : SlideratorGraphMode.Position;
        GraphState = GraphState.Clone();
        GraphState.MinY = GraphModeSetting == SlideratorGraphMode.Position ? 0 : -VelocityLimit;
        GraphState.MaxY = GraphModeSetting == SlideratorGraphMode.Position ? 1 : VelocityLimit;
        if (GraphModeSetting == SlideratorGraphMode.Position && GraphState.Anchors.Count > 0)
        {
            GraphState.Anchors[0].Pos = new Vector2(GraphState.Anchors[0].Pos.X, 0);
        }

        UpdateGraphDerivedValues();
    }

    private async Task ClearGraphAsync()
    {
        bool confirmed = await dialogs.ShowMessageAsync(
            new MessageDialogRequest<bool>(
                "Confirm deletion",
                "Clear the graph?",
                [
                    new DialogChoice<bool>("Delete", true, IsDefault: true),
                    new DialogChoice<bool>("Cancel", false, IsCancel: true)
                ],
                false));
        if (!confirmed)
        {
            return;
        }

        GraphState = SlideratorOptions.CreatePositionGraph(GraphBeats);
        if (GraphModeSetting == SlideratorGraphMode.Velocity)
        {
            double velocity = MathHelper.Clamp(
                PixelLength / GraphBeats / GlobalSv / 100,
                -VelocityLimit,
                VelocityLimit);
            GraphState = new GraphState(
                [
                    new GraphAnchor(new Vector2(0, (float)velocity)),
                    new GraphAnchor(new Vector2((float)GraphBeats, (float)velocity))
                ],
                0,
                -VelocityLimit,
                GraphBeats,
                VelocityLimit);
        }

        UpdateGraphDerivedValues();
    }

    private async Task ScaleCompleteAsync()
    {
        double maximum = GraphModeSetting == SlideratorGraphMode.Velocity
            ? GraphState.GetMaxIntegral()
            : GraphState.GetMaxValue();
        if (Math.Abs(maximum) < Precision.DoubleEpsilon)
        {
            return;
        }

        ValueDialogResult<double> result = await dialogs.ShowValueAsync(
            new ValueDialogRequest<double>(
                "Scale graph",
                "Scale graph maximum to:",
                1,
                new InvariantDoubleConverter()));
        if (!result.Accepted || !double.IsFinite(result.Value))
        {
            return;
        }

        double target = result.Value;

        GraphState = GraphState.Clone();
        foreach (GraphAnchor anchor in GraphState.Anchors)
        {
            anchor.Pos = new Vector2(anchor.Pos.X, (float)(anchor.Pos.Y * target / maximum));
        }

        UpdateGraphDerivedValues();
    }

    private async Task ShowMessageAsync(string message)
    {
        await dialogs.ShowMessageAsync(
            new MessageDialogRequest<bool>(
                "Sliderator",
                message,
                [new DialogChoice<bool>("OK", true, IsDefault: true, IsCancel: true)],
                false));
    }
}
