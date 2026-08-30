using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.Models;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels.Adapters;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;

/// <summary>
///     Owns Combo Colour Studio editing, project persistence, imports, ordinary
///     execution, and QuickRun routing.
/// </summary>
public sealed partial class ComboColourStudioViewModel : SingleRunToolViewModel,
    IShellProjectFeature<ComboColourProject>,
    IQuickRun
{
    private readonly ICurrentBeatmapLocator currentBeatmap;

    private readonly ProjectDefinition<ComboColourProject> definition = new(
        "combocolourproject.json",
        "Combo Colour Studio Projects",
        () => new ComboColourProject(),
        "combo-colour-studio-project.json",
        ToolConfigSchema.ForTool(ComboColourStudioToolDefinition.Definition.Id));

    private readonly IFilePicker filePicker;
    private readonly ILiveBeatmapReader liveReader;

    private readonly IComboColourStudioService studio;
    private readonly IBeatmapWorkspace workspace;
    private ObservableColourPoint? selectedColourPoint;

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
        : base(execution, ComboColourStudioToolDefinition.Definition)
    {
        this.studio = studio ?? throw new ArgumentNullException(nameof(studio));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.liveReader = liveReader ?? throw new ArgumentNullException(nameof(liveReader));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        RebuildPresentation();
    }

    /// <summary>Gets or sets the editable project.</summary>
    [ObservableProperty]
    public partial ComboColourProject Project { get; set; } = new();

    /// <summary>Gets the Desktop-adapted colour points shown by the editing grid.</summary>
    public ObservableCollection<ObservableColourPoint> ColourPoints { get; } = [];

    /// <summary>Gets the palette entries shown by the sequence editor.</summary>
    public ObservableCollection<ObservableSpecialColour> ComboColours { get; } = [];

    /// <summary>Gets or sets the optional source path used by imports.</summary>
    [ObservableProperty]
    public partial string ImportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the combo colour selected for sequence insertion.</summary>
    [ObservableProperty]
    public partial ObservableSpecialColour? SelectedSequenceColour { get; set; }

    /// <summary>Gets or sets the point selected by the editing grid.</summary>
    public ObservableColourPoint? SelectedColourPoint
    {
        get => selectedColourPoint;
        set
        {
            if (ReferenceEquals(selectedColourPoint, value)) return;

            SetProperty(ref selectedColourPoint, value);
            SelectedSequenceColour = ComboColours.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedSequence));
        }
    }

    /// <summary>Gets the selected point's sequence for the sequence editor.</summary>
    public IReadOnlyList<ObservableSpecialColour> SelectedSequence =>
        SelectedColourPoint?.ColourSequence ?? [];

    /// <summary>Gets the latest validation or execution summary.</summary>
    [ObservableProperty]
    public partial string ResultSummary { get; private set; } = string.Empty;

    /// <inheritdoc />
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(path))
        {
            ResultSummary = "Open a target beatmap in osu! before using QuickRun.";
            return;
        }

        await RunWithStateAsync(() => RunPathsAsync(
            [path],
            true,
            cancellationToken));
    }

    ProjectDefinition<ComboColourProject> IShellProjectFeature<ComboColourProject>.ProjectDefinition => definition;

    ComboColourProject IShellProjectFeature<ComboColourProject>.Snapshot()
    {
        return SnapshotProject();
    }

    void IShellProjectFeature<ComboColourProject>.Install(ComboColourProject project)
    {
        Project = project;
        Project.MatchComboColourReferences();
        RebuildPresentation();
        SelectedColourPoint = ColourPoints.FirstOrDefault();
    }

    /// <summary>Adds a new normal point after the selected or last point.</summary>
    [RelayCommand]
    private void AddColourPoint()
    {
        double time = ColourPoints.Count > 1
            ? SelectedColourPoint?.Time ?? ColourPoints[^1].Time
            : 0;
        SelectedColourPoint = AddPresentationPoint(Project.AddColourPoint(time));
    }

    /// <summary>Adds a point at the current Editor Reader playhead when available.</summary>
    [RelayCommand]
    private async Task AddColourPointAtEditorTimeAsync()
    {
        double time = 0;
        try
        {
            var snapshot = await liveReader.ReadAsync();
            if (snapshot?.EditorTime is { } editorTime) time = editorTime;
        }
        catch
        {
            // The legacy action falls back to zero when the editor cannot be read.
        }

        SelectedColourPoint = AddPresentationPoint(Project.AddColourPoint(time));
    }

    /// <summary>Removes selected points or the last point.</summary>
    [RelayCommand]
    private void RemoveColourPoint()
    {
        if (SelectedColourPoint is not null)
            ColourPoints.Remove(SelectedColourPoint);
        else if (ColourPoints.Count > 0) ColourPoints.RemoveAt(ColourPoints.Count - 1);

        SyncProjectFromPresentation();
        SelectedColourPoint = ColourPoints.LastOrDefault();
    }

    /// <summary>Adds a palette colour while retaining the legacy eight-colour cap.</summary>
    [RelayCommand]
    private void AddComboColour()
    {
        Project.AddComboColour();
        RebuildPalette();
        SelectedSequenceColour ??= ComboColours.LastOrDefault();
    }

    /// <summary>Removes the last palette colour.</summary>
    [RelayCommand]
    private void RemoveComboColour()
    {
        Project.RemoveLastComboColour();
        RebuildPalette();
        SelectedSequenceColour = ComboColours.LastOrDefault();
    }

    /// <summary>Adds the selected palette colour to a point's ordered sequence.</summary>
    /// <param name="point">The destination point, or the selected point when omitted.</param>
    [RelayCommand]
    private void AddSequenceColour(ObservableColourPoint? point)
    {
        point ??= SelectedColourPoint;
        if (point is null || SelectedSequenceColour is null) return;

        point.ColourSequence.Add(SelectedSequenceColour);
        SyncProjectFromPresentation();
        OnPropertyChanged(nameof(SelectedSequence));
    }

    /// <summary>Removes a selected sequence entry.</summary>
    /// <param name="colour">The entry to remove, or the last entry when omitted.</param>
    [RelayCommand]
    private void RemoveSequenceColour(ObservableSpecialColour? colour)
    {
        if (SelectedColourPoint is null || SelectedColourPoint.ColourSequence.Count == 0) return;

        if (colour is null)
            SelectedColourPoint.ColourSequence.RemoveAt(SelectedColourPoint.ColourSequence.Count - 1);
        else
            SelectedColourPoint.ColourSequence.Remove(colour);

        SyncProjectFromPresentation();
        OnPropertyChanged(nameof(SelectedSequence));
    }

    /// <summary>Opens a beatmap picker and imports its palette.</summary>
    [RelayCommand]
    private async Task ImportColoursAsync()
    {
        await ImportAsync(false);
    }

    /// <summary>Opens a beatmap picker and infers colour-hax points.</summary>
    [RelayCommand]
    private async Task ImportColourHaxAsync()
    {
        await ImportAsync(true);
    }

    /// <summary>Uses the current osu! map as the import source.</summary>
    [RelayCommand]
    private async Task UseCurrentImportAsync()
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync();
        if (!string.IsNullOrWhiteSpace(path)) ImportPath = path;
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        await RunPathsAsync(workspace.SelectedPaths, false, CancellationToken.None);
    }

    partial void OnProjectChanged(ComboColourProject value)
    {
        value.MatchComboColourReferences();
        RebuildPresentation();
        OnPropertyChanged(nameof(SelectedSequence));
    }

    private async Task ImportAsync(bool colourHax)
    {
        string path = ImportPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
            {
                Title = colourHax ? "Import colour hax" : "Import colours",
                AllowMultiple = false,
                Filters = [CommonFilePickerFilters.BeatmapsAndStoryboards],
            });
            path = paths.FirstOrDefault() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(path)) ImportPath = path;
        }

        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            ComboColourEngineOptions imported = colourHax
                ? await studio.ImportColourHaxAsync(path, Project.MaxBurstLength)
                : await studio.ImportComboColoursAsync(path);

            InstallImportedProject(imported, colourHax);

            RebuildPresentation();
            SelectedColourPoint = ColourPoints.FirstOrDefault();
            ResultSummary = colourHax ? "Imported colour hax." : "Imported combo colours.";
        }
        catch (Exception exception)
        {
            ResultSummary = exception.Message;
        }
    }

    private void InstallImportedProject(ComboColourEngineOptions imported, bool replaceColourPoints)
    {
        ArgumentNullException.ThrowIfNull(imported);

        if (replaceColourPoints)
        {
            ComboColourProject replacement = new() { MaxBurstLength = imported.MaxBurstLength };
            replacement.ComboColours.AddRange(imported.ComboColours.Select(colour => (SpecialColour)colour.Clone()));
            replacement.ColourPoints.AddRange(imported.ColourPoints.Select(point => (ColourPoint)point.Clone()));
            replacement.MatchComboColourReferences();
            Project = replacement;
            return;
        }

        Project.ComboColours = imported.ComboColours
            .Select(colour => (SpecialColour)colour.Clone())
            .ToList();
        Project.MatchComboColourReferences();
    }

    private async Task RunPathsAsync(
        IReadOnlyList<string> paths,
        bool quick,
        CancellationToken cancellationToken)
    {
        var project = SnapshotProject();
        var execution = await Execution.ExecuteAsync(
            new ToolExecutionRequest<ComboColourStudioRunResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    var result = await studio.ApplyAsync(
                        paths,
                        project,
                        new Progress<double>(value => context.ReportProgress(value, "Exporting colours")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<ComboColourStudioRunResult>(
                        result,
                        quick ? null : $"Successfully exported colours to {result.ProcessedCount} " + $"{(result.ProcessedCount == 1 ? "beatmap" : "beatmaps")}!",
                        quick);
                }),
            CreateProgress(),
            cancellationToken);

        if (execution.Status == ToolExecutionStatus.Succeeded && execution.Value is not null)
            ResultSummary = $"Successfully exported colours to {execution.Value.ProcessedCount} " + $"{(execution.Value.ProcessedCount == 1 ? "beatmap" : "beatmaps")}!";
    }

    private ObservableColourPoint AddPresentationPoint(ColourPoint point)
    {
        ObservableColourPoint adapter = new(point);
        ColourPoints.Add(adapter);
        SyncProjectFromPresentation();
        return adapter;
    }

    private void RebuildPresentation()
    {
        ColourPoints.Clear();
        foreach (var point in Project.ColourPoints) ColourPoints.Add(new ObservableColourPoint(point));

        RebuildPalette();
    }

    private void RebuildPalette()
    {
        ComboColours.Clear();
        foreach (var colour in Project.ComboColours)
        {
            ObservableSpecialColour adapter = new(colour);
            ComboColours.Add(adapter);
        }
    }

    private void SyncProjectFromPresentation()
    {
        Project.ColourPoints = ColourPoints.Select(point => point.Snapshot()).ToList();
    }

    private ComboColourProject SnapshotProject()
    {
        SyncProjectFromPresentation();
        return Project.Copy();
    }
}
