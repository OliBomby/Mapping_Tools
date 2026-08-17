using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.ComboColourStudio;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Owns Combo Colour Studio editing, project persistence, imports, preview,
/// ordinary execution, and QuickRun routing.
/// </summary>
public sealed partial class ComboColourStudioViewModel : SingleRunToolViewModel,
    IShellProjectFeature,
    IQuickRun
{
    internal const string OperationId = "combo-colour-studio";

    private readonly IComboColourStudioService _studio;
    private readonly IBeatmapWorkspace _workspace;
    private readonly ICurrentBeatmapLocator _currentBeatmap;
    private readonly ILiveBeatmapReader _liveReader;
    private readonly IFilePicker _filePicker;
    private readonly ProjectDefinition<ComboColourProject> _definition = new(
        "combocolourproject.json",
        "Combo Colour Studio Projects",
        () => new ComboColourProject(),
        "combo-colour-studio-project.json");
    private ColourPoint? _selectedColourPoint;
    private ComboColourProject? _observedProject;

    /// <summary>Gets or sets the editable project.</summary>
    [ObservableProperty]
    public partial ComboColourProject Project { get; set; } = new();

    /// <summary>Gets or sets the optional source path used by imports.</summary>
    [ObservableProperty]
    public partial string ImportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the combo colour selected for sequence insertion.</summary>
    [ObservableProperty]
    public partial SpecialColour? SelectedSequenceColour { get; set; }

    /// <summary>Gets or sets the point selected by the editing grid.</summary>
    public ColourPoint? SelectedColourPoint
    {
        get => _selectedColourPoint;
        set
        {
            if (ReferenceEquals(_selectedColourPoint, value))
            {
                return;
            }

            SetProperty(ref _selectedColourPoint, value);
            SelectedSequenceColour = Project.ComboColours.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedSequence));
        }
    }

    /// <summary>Gets the selected point's sequence for the editing preview.</summary>
    public IReadOnlyList<SpecialColour> SelectedSequence =>
        SelectedColourPoint?.ColourSequence ?? [];

    /// <summary>Gets the current sequence preview entries in time order.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<ComboColourPreviewEntry> PreviewItems { get; private set; } = [];

    /// <summary>Gets whether the configured points contain previewable colours.</summary>
    public bool HasPreviewItems => PreviewItems.Count > 0;

    /// <summary>Gets the latest validation or execution summary.</summary>
    [ObservableProperty]
    public partial string ResultSummary { get; private set; } = string.Empty;

    /// <summary>Creates the Combo Colour Studio presentation model.</summary>
    /// <param name="studio">Runs framework-neutral imports and transformations.</param>
    /// <param name="execution">Coordinates cancellation and notifications.</param>
    /// <param name="workspace">Supplies ordinary-run target maps.</param>
    /// <param name="currentBeatmap">Finds the map open in osu! for QuickRun.</param>
    /// <param name="liveReader">Supplies the current editor time for point insertion.</param>
    /// <param name="filePicker">Presents the import picker.</param>
    public ComboColourStudioViewModel(
        IComboColourStudioService studio,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace,
        ICurrentBeatmapLocator currentBeatmap,
        ILiveBeatmapReader liveReader,
        IFilePicker filePicker)
        : base(execution, OperationId)
    {
        _studio = studio ?? throw new ArgumentNullException(nameof(studio));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        _liveReader = liveReader ?? throw new ArgumentNullException(nameof(liveReader));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        ObserveProject();
        RefreshPreview();
    }

    /// <summary>Adds a new normal point after the selected or last point.</summary>
    [RelayCommand]
    private void AddColourPoint()
    {
        double time = Project.ColourPoints.Count > 1
            ? Project.ColourPoints.Where(point => point.IsSelected).Select(point => (double?)point.Time).Max()
              ?? Project.ColourPoints[^1].Time
            : 0;
        SelectedColourPoint = Project.AddColourPoint(time);
        RefreshPreview();
    }

    /// <summary>Adds a point at the current Editor Reader playhead when available.</summary>
    [RelayCommand]
    private async Task AddColourPointAtEditorTimeAsync()
    {
        double time = 0;
        try
        {
            LiveBeatmapSnapshot? snapshot = await _liveReader.ReadAsync();
            if (snapshot?.EditorTime is double editorTime)
            {
                time = editorTime;
            }
        }
        catch
        {
            // The legacy action falls back to zero when the editor cannot be read.
        }

        SelectedColourPoint = Project.AddColourPoint(time);
        RefreshPreview();
    }

    /// <summary>Removes selected points or the last point.</summary>
    [RelayCommand]
    private void RemoveColourPoint()
    {
        Project.RemoveSelectedOrLastColourPoints();
        SelectedColourPoint = Project.ColourPoints.LastOrDefault();
        RefreshPreview();
    }

    /// <summary>Adds a palette colour while retaining the legacy eight-colour cap.</summary>
    [RelayCommand]
    private void AddComboColour()
    {
        Project.AddComboColour();
        SelectedSequenceColour ??= Project.ComboColours.LastOrDefault();
        RefreshPreview();
    }

    /// <summary>Removes the last palette colour.</summary>
    [RelayCommand]
    private void RemoveComboColour()
    {
        Project.RemoveLastComboColour();
        SelectedSequenceColour = Project.ComboColours.LastOrDefault();
        RefreshPreview();
    }

    /// <summary>Adds the selected palette colour to a point's ordered sequence.</summary>
    /// <param name="point">The destination point, or the selected point when omitted.</param>
    [RelayCommand]
    private void AddSequenceColour(ColourPoint? point)
    {
        point ??= SelectedColourPoint;
        if (point is null || SelectedSequenceColour is null)
        {
            return;
        }

        point.ColourSequence.Add(SelectedSequenceColour);
        RefreshPreview();
        OnPropertyChanged(nameof(SelectedSequence));
    }

    /// <summary>Removes a selected sequence entry.</summary>
    /// <param name="colour">The entry to remove, or the last entry when omitted.</param>
    [RelayCommand]
    private void RemoveSequenceColour(SpecialColour? colour)
    {
        if (SelectedColourPoint is null || SelectedColourPoint.ColourSequence.Count == 0)
        {
            return;
        }

        if (colour is null)
        {
            SelectedColourPoint.ColourSequence.RemoveAt(SelectedColourPoint.ColourSequence.Count - 1);
        }
        else
        {
            SelectedColourPoint.ColourSequence.Remove(colour);
        }

        RefreshPreview();
        OnPropertyChanged(nameof(SelectedSequence));
    }

    /// <summary>Opens a beatmap picker and imports its palette.</summary>
    [RelayCommand]
    private async Task ImportColoursAsync() => await ImportAsync(colourHax: false);

    /// <summary>Opens a beatmap picker and infers colour-hax points.</summary>
    [RelayCommand]
    private async Task ImportColourHaxAsync() => await ImportAsync(colourHax: true);

    /// <summary>Uses the current osu! map as the import source.</summary>
    [RelayCommand]
    private async Task UseCurrentImportAsync()
    {
        string? path = await _currentBeatmap.FindCurrentBeatmapAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            ImportPath = path;
        }
    }

    /// <inheritdoc/>
    protected override bool PrepareRun()
    {
        IReadOnlyList<string> errors = Project.ValidateForExport();
        if (errors.Count > 0)
        {
            ResultSummary = string.Join(" ", errors);
            return false;
        }

        if (_workspace.SelectedPaths.Count == 0)
        {
            ResultSummary = "Select at least one beatmap or open one in osu! before running Combo Colour Studio.";
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    protected override async Task RunCoreAsync() =>
        await RunPathsAsync(_workspace.SelectedPaths, quick: false, CancellationToken.None);

    /// <inheritdoc/>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await _currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(path))
        {
            ResultSummary = "Open a target beatmap in osu! before using QuickRun.";
            return;
        }

        await RunWithStateAsync(() => RunPathsAsync(
            [path],
            quick: true,
            cancellationToken: cancellationToken));
    }

    IProjectDefinition IShellProjectFeature.ProjectDefinition => _definition;
    string IQuickRun.OperationId => OperationId;
    object IShellProjectFeature.Snapshot() => Project.Copy();

    void IShellProjectFeature.Install(object project)
    {
        if (project is not ComboColourProject typed)
        {
            throw new InvalidDataException("Combo Colour Studio project is incomplete.");
        }

        Project = typed;
        Project.MatchComboColourReferences();
        SelectedColourPoint = Project.ColourPoints.FirstOrDefault();
        RefreshPreview();
    }

    partial void OnProjectChanged(ComboColourProject value)
    {
        value.MatchComboColourReferences();
        ObserveProject();
        RefreshPreview();
        OnPropertyChanged(nameof(SelectedSequence));
    }

    private async Task ImportAsync(bool colourHax)
    {
        string path = ImportPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            IReadOnlyList<string> paths = await _filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
            {
                Title = colourHax ? "Import colour hax" : "Import colours",
                AllowMultiple = false,
                Filters = [CommonFilePickerFilters.BeatmapsAndStoryboards]
            });
            path = paths.FirstOrDefault() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(path))
            {
                ImportPath = path;
            }
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (colourHax)
            {
                await _studio.ImportColourHaxAsync(path, Project);
            }
            else
            {
                await _studio.ImportComboColoursAsync(path, Project);
            }

            SelectedColourPoint = Project.ColourPoints.FirstOrDefault();
            ResultSummary = colourHax ? "Imported colour hax." : "Imported combo colours.";
            RefreshPreview();
        }
        catch (Exception exception)
        {
            ResultSummary = exception.Message;
        }
    }

    private async Task RunPathsAsync(
        IReadOnlyList<string> paths,
        bool quick,
        CancellationToken cancellationToken)
    {
        ComboColourProject project = Project.Copy();
        ToolExecutionResult<ComboColourStudioRunResult> execution = await Execution.ExecuteAsync(
            new ToolExecutionRequest<ComboColourStudioRunResult>(
                OperationId,
                "Combo Colour Studio",
                async context =>
                {
                    ComboColourStudioRunResult result = await _studio.ApplyAsync(
                        paths,
                        project,
                        new Progress<double>(value => context.ReportProgress(value, "Exporting colours")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<ComboColourStudioRunResult>(
                        result,
                        quick ? null : $"Successfully exported colours to {result.ProcessedCount} " +
                                       $"{(result.ProcessedCount == 1 ? "beatmap" : "beatmaps")}!",
                        reloadEditor: quick);
                }),
            CreateProgress(),
            cancellationToken);

        if (execution.Status == ToolExecutionStatus.Succeeded && execution.Value is not null)
        {
            ResultSummary = $"Successfully exported colours to {execution.Value.ProcessedCount} " +
                            $"{(execution.Value.ProcessedCount == 1 ? "beatmap" : "beatmaps")}!";
        }
    }

    private void RefreshPreview()
    {
        PreviewItems = Project.ColourPoints
            .OrderBy(point => point.Time)
            .SelectMany(point => point.ColourSequence.Select(colour => new ComboColourPreviewEntry(
                point.Time,
                point.Mode,
                colour.Name ?? string.Empty,
                colour.Color)))
            .Take(256)
            .ToArray();
        OnPropertyChanged(nameof(HasPreviewItems));
    }

    private void ObserveProject()
    {
        if (_observedProject is not null)
        {
            _observedProject.ColourPoints.CollectionChanged -= OnProjectCollectionChanged;
            _observedProject.ComboColours.CollectionChanged -= OnProjectCollectionChanged;
            foreach (ColourPoint point in _observedProject.ColourPoints)
            {
                point.PropertyChanged -= OnPointChanged;
                point.ColourSequence.CollectionChanged -= OnProjectCollectionChanged;
            }

            foreach (SpecialColour colour in _observedProject.ComboColours)
            {
                colour.PropertyChanged -= OnColourChanged;
            }
        }

        _observedProject = Project;
        _observedProject.ColourPoints.CollectionChanged += OnProjectCollectionChanged;
        _observedProject.ComboColours.CollectionChanged += OnProjectCollectionChanged;
        foreach (ColourPoint point in _observedProject.ColourPoints)
        {
            point.PropertyChanged += OnPointChanged;
            point.ColourSequence.CollectionChanged += OnProjectCollectionChanged;
        }

        foreach (SpecialColour colour in _observedProject.ComboColours)
        {
            colour.PropertyChanged += OnColourChanged;
        }
    }

    private void OnProjectCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        ObserveProject();
        RefreshPreview();
    }

    private void OnPointChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(ColourPoint.ColourSequence))
        {
            ObserveProject();
        }

        RefreshPreview();
    }

    private void OnColourChanged(object? sender, PropertyChangedEventArgs eventArgs) => RefreshPreview();
}
