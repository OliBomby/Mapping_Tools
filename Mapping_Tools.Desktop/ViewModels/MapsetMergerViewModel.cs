using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.MapsetMerger;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.MapsetMerger;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Owns Mapset Merger's multi-mapset form, safe export execution, and legacy
/// project persistence. The feature intentionally has no QuickRun target
/// because its input is a collection of source directories.
/// </summary>
public sealed partial class MapsetMergerViewModel : SingleRunToolViewModel, IShellProjectFeature
{
    internal const string OperationId = "mapset-merger";

    private readonly IMapsetMergerService _merger;
    private readonly IFilePicker _filePicker;
    private readonly IBeatmapWorkspace _workspace;
    private readonly ICurrentBeatmapLocator _currentBeatmap;
    private readonly ProjectDefinition<MapsetMergerProject> _definition = new(
        "mapsetmergerproject.json",
        "Mapset Merger Projects",
        static () => new MapsetMergerProject(),
        "mapset-merger-project.json");

    /// <summary>Gets the editable source mapset rows in merge order.</summary>
    public ObservableCollection<MapsetMergerItemViewModel> Mapsets { get; } = [];

    /// <summary>Gets or sets the export directory.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Select an export directory.")]
    public partial string ExportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the first storyboard is embedded in beatmaps.</summary>
    [ObservableProperty]
    public partial bool MoveSbToBeatmap { get; set; }

    /// <summary>Gets the latest success or validation summary.</summary>
    [ObservableProperty]
    public partial string ResultSummary { get; private set; } = string.Empty;

    /// <summary>Creates the Mapset Merger presentation model.</summary>
    /// <param name="merger">Stages and commits the merger operation.</param>
    /// <param name="execution">Coordinates cancellation and background execution.</param>
    /// <param name="filePicker">Presents source and export folder pickers.</param>
    /// <param name="workspace">Supplies the selected beatmap used by the ordinary add action.</param>
    /// <param name="currentBeatmap">Finds the beatmap currently open in osu!.</param>
    /// <param name="directories">Supplies the default export folder.</param>
    public MapsetMergerViewModel(
        IMapsetMergerService merger,
        IToolExecutionService execution,
        IFilePicker filePicker,
        IBeatmapWorkspace workspace,
        ICurrentBeatmapLocator currentBeatmap,
        IApplicationDirectories directories)
        : base(execution, OperationId)
    {
        _merger = merger ?? throw new ArgumentNullException(nameof(merger));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        ArgumentNullException.ThrowIfNull(directories);
        ExportPath = directories.Exports;
    }

    /// <summary>Adds the directory containing the current osu! beatmap.</summary>
    [RelayCommand]
    private Task AddMapsetAsync() => AddMapsetFromPathAsync(
        _workspace.SelectedPaths.FirstOrDefault(),
        "Select a beatmap in the shell or hold Shift to fetch the current osu! beatmap.");

    /// <summary>Adds the mapset containing the beatmap currently open in osu!.</summary>
    [RelayCommand]
    private async Task AddMapsetFromCurrentAsync()
    {
        try
        {
            await AddMapsetFromPathAsync(
                await _currentBeatmap.FindCurrentBeatmapAsync(),
                "Open a beatmap in osu! or select one in the shell before adding a mapset.");
        }
        catch (Exception exception)
        {
            ResultSummary = $"Could not read the current osu! beatmap: {exception.Message}";
        }
    }

    private Task AddMapsetFromPathAsync(string? beatmapPath, string unavailableMessage)
    {
        string? directory = string.IsNullOrWhiteSpace(beatmapPath)
            ? null
            : System.IO.Path.GetDirectoryName(beatmapPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            ResultSummary = unavailableMessage;
            return Task.CompletedTask;
        }

        Mapsets.Add(new MapsetMergerItemViewModel(
            _filePicker,
            new DirectoryInfo(directory).Name,
            directory));

        return Task.CompletedTask;
    }

    /// <summary>Removes selected rows, or the last row when none is selected.</summary>
    [RelayCommand]
    private void RemoveMapset()
    {
        List<MapsetMergerItemViewModel> selected = Mapsets.Where(item => item.IsSelected).ToList();
        if (selected.Count > 0)
        {
            foreach (MapsetMergerItemViewModel item in selected)
            {
                Mapsets.Remove(item);
            }

            return;
        }

        if (Mapsets.Count > 0)
        {
            Mapsets.RemoveAt(Mapsets.Count - 1);
        }
    }

    /// <summary>Chooses the final export directory.</summary>
    [RelayCommand]
    private async Task BrowseExportPathAsync()
    {
        try
        {
            IReadOnlyList<string> paths = await _filePicker.PickFoldersAsync(new OpenFolderPickerRequest
            {
                Title = "Select export path",
                SuggestedStartLocation = Directory.Exists(ExportPath) ? ExportPath : null,
                AllowMultiple = false
            });
            string? path = paths.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(path))
            {
                ExportPath = path;
            }
        }
        catch (Exception exception)
        {
            ResultSummary = $"Could not select the export path: {exception.Message}";
        }
    }

    /// <inheritdoc/>
    protected override bool PrepareRun()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            ResultSummary = "Select an export directory.";
            return false;
        }

        if (Mapsets.Count == 0)
        {
            ResultSummary = "Add at least one mapset.";
            return false;
        }

        if (Mapsets.Any(item => string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Path)))
        {
            ResultSummary = "Every mapset needs a name and source directory.";
            return false;
        }

        foreach (MapsetMergerItemViewModel item in Mapsets)
        {
            try
            {
                MapsetMergerEngine.ValidateMapsetName(item.Name);
            }
            catch (ArgumentException)
            {
                ResultSummary = $"Mapset name '{item.Name}' is not a safe folder name.";
                return false;
            }

            if (!Directory.Exists(item.Path))
            {
                ResultSummary = $"Mapset directory '{item.Path}' was not found.";
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    protected override async Task RunCoreAsync()
    {
        MapsetMergerProject project = Snapshot();
        ToolExecutionResult<MapsetMergerResult> result = await Execution.ExecuteAsync(
            new ToolExecutionRequest<MapsetMergerResult>(
                OperationId,
                "Mapset Merger",
                async context =>
                {
                    MapsetMergerResult merged = await _merger.MergeAsync(
                        project,
                        new Progress<double>(value => context.ReportProgress(value, "Merging mapsets")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<MapsetMergerResult>(
                        merged,
                        $"Successfully merged {merged.MapsetsMerged} " +
                        $"{(merged.MapsetsMerged == 1 ? "mapset" : "mapsets")}!");
                }),
            CreateProgress());

        if (result.Status == ToolExecutionStatus.Succeeded && result.Value is not null)
        {
            ResultSummary = $"Successfully merged {result.Value.MapsetsMerged} " +
                            $"{(result.Value.MapsetsMerged == 1 ? "mapset" : "mapsets")}!";
        }
        else if (result.Status == ToolExecutionStatus.Cancelled)
        {
            ResultSummary = "Mapset Merger was cancelled.";
        }
        else if (result.Status == ToolExecutionStatus.Failed)
        {
            ResultSummary = result.Exception?.Message ?? "Mapset Merger failed.";
        }
    }

    IProjectDefinition IShellProjectFeature.ProjectDefinition => _definition;

    object IShellProjectFeature.Snapshot() => Snapshot();

    void IShellProjectFeature.Install(object project)
    {
        if (project is not MapsetMergerProject typed)
        {
            throw new InvalidDataException("Mapset Merger project is incomplete.");
        }

        ExportPath = typed.ExportPath ?? string.Empty;
        MoveSbToBeatmap = typed.MoveSbToBeatmap;
        Mapsets.Clear();
        foreach (MapsetMergerProject.MapsetItem item in typed.Mapsets ?? [])
        {
            Mapsets.Add(new MapsetMergerItemViewModel(
                _filePicker,
                item.Name ?? string.Empty,
                item.Path ?? string.Empty));
        }
    }

    private MapsetMergerProject Snapshot() => new()
    {
        ExportPath = ExportPath,
        MoveSbToBeatmap = MoveSbToBeatmap,
        Mapsets = Mapsets.Select(item => new MapsetMergerProject.MapsetItem
        {
            Name = item.Name,
            Path = item.Path
        }).ToList()
    };
}
