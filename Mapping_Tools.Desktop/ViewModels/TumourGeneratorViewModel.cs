using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Interactions.Dialogs;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Tools.TumourGenerating.Models;
using Mapping_Tools.Core.Tools.TumourGenerating.Templates;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels.Adapters;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Owns Tumour Generator 2 settings, graph-backed layers, preview state,
///     project persistence, and ordinary or QuickRun execution.
/// </summary>
public sealed partial class TumourGeneratorViewModel : SingleRunToolViewModel,
    IShellProjectFeature,
    IQuickRun,
    IShellFeatureActivation,
    IDisposable
{
    private readonly ICurrentBeatmapLocator currentBeatmap;

    private readonly ProjectDefinition<TumourGeneratorProject> definition = new(
        "tumourgeneratorproject.json",
        "Tumour Generator Projects",
        static () => new TumourGeneratorProject(),
        "tumour-generator-project.json");

    private readonly IDialogService dialogs;

    private readonly ITumourGeneratorService generator;
    private readonly object previewGate = new();
    private readonly ApplicationSettings settings;
    private readonly IBeatmapWorkspace workspace;
    private int currentLayerIndex;
    private bool disposed;
    private bool isActive;
    private CancellationTokenSource? previewCancellation;
    private HitObject previewHitObject = new("0,0,0,2,0,L|256:0,1,256");
    private HitObject? tumouredPreviewHitObject;

    /// <summary>
    ///     Creates the Tumour Generator 2 presentation model.
    /// </summary>
    /// <param name="generator">Runs Core generation through Application ports.</param>
    /// <param name="execution">Coordinates cancellation, progress, and reload.</param>
    /// <param name="currentBeatmap">Finds the beatmap currently open in osu!.</param>
    /// <param name="workspace">Supplies ordinary-run map selection.</param>
    /// <param name="settings">Supplies the AlwaysQuickRun preference.</param>
    /// <param name="dialogs">Presents empty-selection and error messages.</param>
    public TumourGeneratorViewModel(
        ITumourGeneratorService generator,
        IToolExecutionService execution,
        ICurrentBeatmapLocator currentBeatmap,
        IBeatmapWorkspace workspace,
        ApplicationSettings settings,
        IDialogService dialogs)
        : base(execution, MappingToolDefinitions.TumourGenerator)
    {
        this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

        TumourLayers.CollectionChanged += OnLayersChanged;
        TumourLayers.Add(new ObservableTumourLayer(TumourLayer.GetDefaultLayer()));
        QueuePreview();
    }

    /// <summary>Gets the object-selection modes in the legacy display order.</summary>
    public IReadOnlyList<HitObjectSelectionMode> ImportModes { get; } =
        Enum.GetValues<HitObjectSelectionMode>();

    /// <summary>Gets the geometric templates in the legacy display order.</summary>
    public IReadOnlyList<TumourTemplate> TumourTemplates { get; } =
        Enum.GetValues<TumourTemplate>();

    /// <summary>Gets the path-wrapping modes in the legacy display order.</summary>
    public IReadOnlyList<WrappingMode> WrappingModes { get; } =
        Enum.GetValues<WrappingMode>();

    /// <summary>Gets the sidedness modes in the legacy display order.</summary>
    public IReadOnlyList<TumourSidedness> TumourSides { get; } =
        Enum.GetValues<TumourSidedness>();

    /// <summary>Gets the editable layers in generation order.</summary>
    public ObservableCollection<ObservableTumourLayer> TumourLayers { get; } = [];

    /// <summary>Gets or sets the source used when importing or running.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeCodeVisible))]
    public partial HitObjectSelectionMode ImportModeSetting { get; set; } = HitObjectSelectionMode.Selected;

    /// <summary>Gets or sets the time query used by time-based selection.</summary>
    [ObservableProperty]
    public partial string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets whether only middle anchors are retained.</summary>
    [ObservableProperty]
    public partial bool JustMiddleAnchors { get; set; }

    /// <summary>Gets or sets the global tumour size scalar.</summary>
    [ObservableProperty]
    public partial double Scale { get; set; } = 1;

    /// <summary>Gets or sets the Circle Size used by the preview visualizer.</summary>
    [ObservableProperty]
    public partial double CircleSize { get; set; } = 4;

    /// <summary>Gets or sets whether slider velocity is corrected after generation.</summary>
    [ObservableProperty]
    public partial bool FixSv { get; set; } = true;

    /// <summary>Gets or sets whether corrected velocity is delegated to BPM redlines.</summary>
    [ObservableProperty]
    public partial bool DelegateToBpm { get; set; }

    /// <summary>Gets or sets whether delegated velocity removes slider ticks.</summary>
    [ObservableProperty]
    public partial bool RemoveSliderTicks { get; set; }

    /// <summary>Gets whether delegated slider-tick removal is currently applicable.</summary>
    public bool RemoveSliderTicksEnabled => FixSv && DelegateToBpm;

    /// <summary>Gets or sets whether advanced layer controls are visible.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TumourStartSliderMin))]
    public partial bool AdvancedOptions { get; set; }

    /// <summary>Gets or sets whether reconstruction diagnostics are enabled.</summary>
    [ObservableProperty]
    public partial bool DebugConstruction { get; set; }

    /// <summary>Gets whether the time-code field applies to the current import mode.</summary>
    public bool TimeCodeVisible => ImportModeSetting == HitObjectSelectionMode.Time;

    /// <summary>Gets whether the parameter graph is shown for the current template.</summary>
    public bool TumourParameterGraphVisible => AdvancedOptions || CurrentLayer?.TumourTemplate.NeedsParameter == true;

    /// <summary>Gets or sets the slider displayed in the preview.</summary>
    public HitObject PreviewHitObject
    {
        get => previewHitObject;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(previewHitObject, value)) return;

            SetProperty(ref previewHitObject, value);
            QueuePreview();
        }
    }

    /// <summary>Gets the most recently generated preview slider.</summary>
    public HitObject? TumouredPreviewHitObject
    {
        get => tumouredPreviewHitObject;
        private set
        {
            if (SetProperty(ref tumouredPreviewHitObject, value)) IsProcessingPreview = false;
        }
    }

    /// <summary>Gets whether the latest preview request is still running.</summary>
    [ObservableProperty]
    public partial bool IsProcessingPreview { get; private set; }

    /// <summary>Gets or sets the selected layer index.</summary>
    public int CurrentLayerIndex
    {
        get => currentLayerIndex;
        set
        {
            int normalized = Math.Clamp(value, 0, Math.Max(0, TumourLayers.Count - 1));
            if (!SetProperty(ref currentLayerIndex, normalized)) return;

            OnPropertyChanged(nameof(CurrentLayer));
            OnPropertyChanged(nameof(TumourParameterGraphVisible));
            OnPropertyChanged(nameof(TumourStartSliderMin));
            OnPropertyChanged(nameof(TumourRangeSliderMax));
            OnPropertyChanged(nameof(TumourRangeSliderSmallChange));
            QueuePreview();
        }
    }

    /// <summary>Gets or sets the layer selected by the details panel.</summary>
    public ObservableTumourLayer? CurrentLayer
    {
        get => CurrentLayerIndex >= 0 && CurrentLayerIndex < TumourLayers.Count
            ? TumourLayers[CurrentLayerIndex]
            : null;
        set
        {
            if (value is null) return;

            int index = TumourLayers.IndexOf(value);
            if (index >= 0) CurrentLayerIndex = index;
        }
    }

    /// <summary>Gets the generated maximum range for each layer.</summary>
    public IReadOnlyList<double> LayerRangeSliderMaxes { get; private set; } = [];

    /// <summary>Gets the current layer's minimum range slider value.</summary>
    public double TumourStartSliderMin => AdvancedOptions && CurrentLayerIndex >= 0 && CurrentLayerIndex < LayerRangeSliderMaxes.Count && CurrentLayer?.UseAbsoluteRange == true
        ? -LayerRangeSliderMaxes[CurrentLayerIndex]
        : AdvancedOptions
            ? -1
            : 0;

    /// <summary>Gets the current layer's maximum range slider value.</summary>
    public double TumourRangeSliderMax => CurrentLayerIndex >= 0 && CurrentLayerIndex < LayerRangeSliderMaxes.Count && CurrentLayer?.UseAbsoluteRange == true
        ? LayerRangeSliderMaxes[CurrentLayerIndex]
        : 1;

    /// <summary>Gets the range slider step matching relative or absolute units.</summary>
    public double TumourRangeSliderSmallChange => CurrentLayer?.UseAbsoluteRange == true ? 1 : 0.0001;

    /// <summary>Gets the latest validation, import, or run summary.</summary>
    [ObservableProperty]
    public partial string ResultSummary { get; private set; } = string.Empty;

    /// <summary>
    ///     Stops pending preview work and detaches layer event handlers owned by this view model.
    /// </summary>
    public void Dispose()
    {
        lock (previewGate)
        {
            if (disposed) return;

            disposed = true;
            isActive = false;
            previewCancellation?.Cancel();
            previewCancellation?.Dispose();
            previewCancellation = null;
        }

        TumourLayers.CollectionChanged -= OnLayersChanged;
        foreach (var layer in TumourLayers) layer.PropertyChanged -= OnLayerChanged;
    }

    /// <summary>Runs the current editor map through the QuickRun path.</summary>
    /// <param name="cancellationToken">Cancels lookup, generation, or saving.</param>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        if (!ValidateProject(out string error))
        {
            ResultSummary = error;
            return;
        }

        string? path = await currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        await RunWithStateAsync(() => RunPathsAsync(
            string.IsNullOrWhiteSpace(path) ? [] : [path],
            true,
            cancellationToken));
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public void Activate()
    {
        if (disposed) return;

        isActive = true;
        QueuePreview();
    }

    /// <inheritdoc />
    public void Deactivate()
    {
        CancellationTokenSource? cancellation;
        lock (previewGate)
        {
            isActive = false;
            cancellation = previewCancellation;
            previewCancellation = null;
        }

        cancellation?.Cancel();
        cancellation?.Dispose();
        IsProcessingPreview = false;
    }

    /// <inheritdoc />
    IProjectDefinition IShellProjectFeature.ProjectDefinition => definition;

    /// <inheritdoc />
    object IShellProjectFeature.Snapshot()
    {
        return Snapshot();
    }

    /// <inheritdoc />
    void IShellProjectFeature.Install(object project)
    {
        Install(project as TumourGeneratorProject ?? throw new InvalidDataException("Tumour Generator project is incomplete."));
    }

    /// <summary>Imports the selected, bookmarked, time-filtered, or complete sliders.</summary>
    [RelayCommand]
    private async Task ImportAsync()
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            await ShowMessageAsync("No beatmap is open in osu!.");
            return;
        }

        try
        {
            var result = await generator.ImportAsync(
                path,
                ImportModeSetting,
                TimeCode,
                CancellationToken.None);
            if (result.Sliders.Count == 0)
            {
                await ShowMessageAsync("Could not find any sliders in imported hit objects.");
                return;
            }

            PreviewHitObject = result.Sliders[0].DeepCopy();
            CircleSize = result.CircleSize;
            ResultSummary = "Successfully imported slider.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(exception.Message);
        }
    }

    /// <summary>Adds a default layer after the current layer.</summary>
    [RelayCommand]
    private void Add()
    {
        ObservableTumourLayer layer = new(TumourLayer.GetDefaultLayer());
        layer.Name = $"Layer {TumourLayers.Count + 1}";
        layer.TumourEnd = LayerRangeSliderMaxes.LastOrDefault(PreviewHitObject.PixelLength);
        InsertAfterCurrent(layer);
    }

    /// <summary>Copies the current layer after itself.</summary>
    [RelayCommand]
    private void Copy()
    {
        if (CurrentLayer is null) return;

        ObservableTumourLayer copy = new(CurrentLayer.Snapshot());
        copy.Name = $"{copy.Name} (Copy)";
        InsertAfterCurrent(copy);
    }

    /// <summary>Removes the current layer while retaining one minimum layer.</summary>
    [RelayCommand]
    private void Remove()
    {
        if (TumourLayers.Count <= 1 || CurrentLayer is null) return;

        int index = CurrentLayerIndex;
        CurrentLayerIndex = index == 0 ? 1 : index - 1;
        TumourLayers.RemoveAt(index);
    }

    /// <summary>Moves the current layer one position toward the end.</summary>
    [RelayCommand]
    private void Raise()
    {
        if (CurrentLayerIndex >= 0 && CurrentLayerIndex < TumourLayers.Count - 1)
        {
            TumourLayers.Move(CurrentLayerIndex, CurrentLayerIndex + 1);
            CurrentLayerIndex++;
        }
    }

    /// <summary>Moves the current layer one position toward the beginning.</summary>
    [RelayCommand]
    private void Lower()
    {
        if (CurrentLayerIndex > 0)
        {
            TumourLayers.Move(CurrentLayerIndex, CurrentLayerIndex - 1);
            CurrentLayerIndex--;
        }
    }

    /// <summary>Replaces the current layer's random seed with a new seed.</summary>
    [RelayCommand]
    private void Randomize()
    {
        if (CurrentLayer is not null) CurrentLayer.RandomSeed = Random.Shared.Next();
    }

    /// <summary>Validates the current project and stores a user-facing correction message.</summary>
    /// <returns><see langword="true" /> when all required project state is valid.</returns>
    public bool ValidateSettings()
    {
        if (!ValidateProject(out string error))
        {
            ResultSummary = error;
            return false;
        }

        ResultSummary = string.Empty;
        return true;
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        if (!ValidateProject(out string error))
        {
            ResultSummary = error;
            return;
        }

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
        if (HasErrors)
        {
            ResultSummary = "Correct the invalid Tumour Generator settings before running.";
            return false;
        }

        return ValidateSettings();
    }

    partial void OnScaleChanged(double value)
    {
        QueuePreview();
    }

    partial void OnJustMiddleAnchorsChanged(bool value)
    {
        QueuePreview();
    }

    partial void OnDebugConstructionChanged(bool value)
    {
        QueuePreview();
    }

    partial void OnFixSvChanged(bool value)
    {
        OnPropertyChanged(nameof(RemoveSliderTicksEnabled));
    }

    partial void OnDelegateToBpmChanged(bool value)
    {
        OnPropertyChanged(nameof(RemoveSliderTicksEnabled));
    }

    partial void OnAdvancedOptionsChanged(bool value)
    {
        OnPropertyChanged(nameof(TumourStartSliderMin));
        OnPropertyChanged(nameof(TumourRangeSliderMax));
    }

    private void InsertAfterCurrent(ObservableTumourLayer layer)
    {
        int index = Math.Clamp(CurrentLayerIndex + 1, 0, TumourLayers.Count);
        TumourLayers.Insert(index, layer);
        CurrentLayerIndex = index;
    }

    private void OnLayersChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.OldItems is not null)
            foreach (var layer in eventArgs.OldItems.OfType<ObservableTumourLayer>())
                layer.PropertyChanged -= OnLayerChanged;

        if (eventArgs.NewItems is not null)
            foreach (var layer in eventArgs.NewItems.OfType<ObservableTumourLayer>())
                layer.PropertyChanged += OnLayerChanged;

        CurrentLayerIndex = Math.Clamp(CurrentLayerIndex, 0, Math.Max(0, TumourLayers.Count - 1));
        OnPropertyChanged(nameof(CurrentLayer));
        OnPropertyChanged(nameof(TumourParameterGraphVisible));
        OnPropertyChanged(nameof(TumourStartSliderMin));
        OnPropertyChanged(nameof(TumourRangeSliderMax));
        OnPropertyChanged(nameof(TumourRangeSliderSmallChange));
        QueuePreview();
    }

    private void OnLayerChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (ReferenceEquals(sender, CurrentLayer) && eventArgs.PropertyName == nameof(TumourLayer.UseAbsoluteRange))
        {
            OnPropertyChanged(nameof(TumourStartSliderMin));
            OnPropertyChanged(nameof(TumourRangeSliderMax));
            OnPropertyChanged(nameof(TumourRangeSliderSmallChange));
        }

        if (ReferenceEquals(sender, CurrentLayer) && eventArgs.PropertyName is nameof(TumourLayer.TumourTemplateEnum) or nameof(TumourLayer.TumourTemplate))
            OnPropertyChanged(nameof(TumourParameterGraphVisible));

        QueuePreview();
    }

    private void QueuePreview()
    {
        CancellationTokenSource cancellation;
        lock (previewGate)
        {
            if (disposed || !isActive) return;

            previewCancellation?.Cancel();
            previewCancellation?.Dispose();
            previewCancellation = new CancellationTokenSource();
            cancellation = previewCancellation;
        }

        IsProcessingPreview = true;
        _ = RefreshPreviewAsync(cancellation);
    }

    private async Task RefreshPreviewAsync(CancellationTokenSource cancellation)
    {
        try
        {
            var options = SnapshotOptions();
            var result = await generator.PreviewAsync(
                PreviewHitObject,
                options,
                cancellation.Token);
            lock (previewGate)
            {
                if (disposed || !isActive || !ReferenceEquals(previewCancellation, cancellation)) return;
            }

            TumouredPreviewHitObject = result.HitObject;
            LayerRangeSliderMaxes = result.LayerLengths;
            OnPropertyChanged(nameof(LayerRangeSliderMaxes));
            OnPropertyChanged(nameof(TumourStartSliderMin));
            OnPropertyChanged(nameof(TumourRangeSliderMax));
            IsProcessingPreview = false;
        }
        catch (OperationCanceledException)
        {
            // A newer property change owns the next preview request.
        }
        catch (Exception exception)
        {
            lock (previewGate)
            {
                if (disposed || !isActive || !ReferenceEquals(previewCancellation, cancellation)) return;
            }

            ResultSummary = exception.Message;
            IsProcessingPreview = false;
        }
    }

    private async Task RunPathsAsync(
        IReadOnlyList<string> paths,
        bool quick,
        CancellationToken cancellationToken)
    {
        if (paths.Count == 0)
        {
            await ShowMessageAsync("Select at least one beatmap or open one in osu! before running Tumour Generator 2.");
            return;
        }

        var project = Snapshot();
        var execution = await Execution.ExecuteAsync(
            new ToolExecutionRequest<TumourRunResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    var result = await generator.RunAsync(
                        paths,
                        project,
                        quick,
                        new Progress<double>(value => context.ReportProgress(value, "Generating tumours")),
                        context.CancellationToken);
                    string summary = $"Successfully generated tumours on {result.SlidersTumourated} " + $"{(result.SlidersTumourated == 1 ? "slider" : "sliders")}" + "!";
                    return new ToolExecutionOutput<TumourRunResult>(
                        result,
                        quick ? null : summary,
                        quick);
                }),
            CreateProgress(),
            cancellationToken);

        if (execution.Status == ToolExecutionStatus.Succeeded && execution.Value is not null)
            ResultSummary = $"Successfully generated tumours on {execution.Value.SlidersTumourated} " + $"{(execution.Value.SlidersTumourated == 1 ? "slider" : "sliders")}" + "!";
        else if (execution.Status == ToolExecutionStatus.Failed)
            ResultSummary = execution.Exception?.Message ?? "Tumour Generator 2 failed.";
        else if (execution.Status == ToolExecutionStatus.Cancelled)
            ResultSummary = "Tumour Generator 2 was cancelled.";
        else if (execution.Status == ToolExecutionStatus.AlreadyRunning) ResultSummary = "Tumour Generator 2 is already running.";
    }

    private TumourGeneratorProject Snapshot()
    {
        TumourGeneratorProject project = new()
        {
            ImportModeSetting = ImportModeSetting,
            TimeCode = TimeCode,
            JustMiddleAnchors = JustMiddleAnchors,
            Scale = Scale,
            DebugConstruction = DebugConstruction,
            FixSv = FixSv,
            DelegateToBpm = DelegateToBpm,
            RemoveSliderTicks = RemoveSliderTicks,
        };
        project.TumourLayers = TumourLayers.Select(layer => layer.Snapshot()).ToList();
        return project;
    }

    private TumourGeneratorEngineOptions SnapshotOptions()
    {
        return new TumourGeneratorEngineOptions
        {
            TumourLayers = TumourLayers.Select(layer => layer.Snapshot()).ToList(),
            JustMiddleAnchors = JustMiddleAnchors,
            Scale = Scale,
            DebugConstruction = DebugConstruction,
        };
    }

    private void Install(TumourGeneratorProject project)
    {
        if (!ValidateProject(project, out string error)) throw new InvalidDataException(error);

        ImportModeSetting = project.ImportModeSetting;
        TimeCode = project.TimeCode ?? string.Empty;
        JustMiddleAnchors = project.JustMiddleAnchors;
        Scale = project.Scale;
        DebugConstruction = project.DebugConstruction;
        FixSv = project.FixSv;
        DelegateToBpm = project.DelegateToBpm;
        RemoveSliderTicks = project.RemoveSliderTicks;

        TumourLayers.Clear();
        foreach (var layer in project.TumourLayers) TumourLayers.Add(new ObservableTumourLayer(layer.Copy()));

        CurrentLayerIndex = 0;
        QueuePreview();
    }

    private bool ValidateProject(out string error)
    {
        return ValidateProject(Snapshot(), out error);
    }

    private static bool ValidateProject(TumourGeneratorProject? project, out string error)
    {
        if (project is null
            || project.TumourLayers is null
            || project.TumourLayers.Count == 0
            || !Enum.IsDefined(project.ImportModeSetting)
            || !double.IsFinite(project.Scale)
            || project.Scale < 0)
        {
            error = "Tumour Generator project is incomplete or contains invalid values.";
            return false;
        }

        foreach (var layer in project.TumourLayers)
            if (!Enum.IsDefined(layer.TumourTemplateEnum)
                || !Enum.IsDefined(layer.WrappingMode)
                || !Enum.IsDefined(layer.TumourSidedness)
                || layer.TumourCount < 0
                || !double.IsFinite(layer.TumourStart)
                || !double.IsFinite(layer.TumourEnd)
                || layer.TumourLength is null
                || layer.TumourScale is null
                || layer.TumourRotation is null
                || layer.TumourParameter is null
                || layer.TumourDistance is null
                || !IsValidGraph(layer.TumourLength)
                || !IsValidGraph(layer.TumourScale)
                || !IsValidGraph(layer.TumourRotation)
                || !IsValidGraph(layer.TumourParameter)
                || !IsValidGraph(layer.TumourDistance))
            {
                error = "Tumour Generator project contains an invalid layer.";
                return false;
            }

        error = string.Empty;
        return true;
    }

    private static bool IsValidGraph(GraphState graph)
    {
        if (!double.IsFinite(graph.MinX)
            || !double.IsFinite(graph.MinY)
            || !double.IsFinite(graph.MaxX)
            || !double.IsFinite(graph.MaxY)
            || graph.MinX > graph.MaxX
            || graph.MinY > graph.MaxY
            || graph.Anchors is null)
            return false;

        double previousX = double.NegativeInfinity;
        foreach (var anchor in graph.Anchors)
        {
            if (anchor is null
                || !double.IsFinite(anchor.Pos.X)
                || !double.IsFinite(anchor.Pos.Y)
                || !double.IsFinite(anchor.Tension)
                || anchor.Interpolator is null
                || !double.IsFinite(anchor.Interpolator.P)
                || anchor.Pos.X < previousX)
                return false;

            previousX = anchor.Pos.X;
        }

        return true;
    }

    private async Task ShowMessageAsync(string message)
    {
        await dialogs.ShowMessageAsync(
            new MessageDialogRequest<bool>(
                "Tumour Generator 2",
                message,
                [new DialogChoice<bool>("OK", true, true, true)],
                false));
    }
}
