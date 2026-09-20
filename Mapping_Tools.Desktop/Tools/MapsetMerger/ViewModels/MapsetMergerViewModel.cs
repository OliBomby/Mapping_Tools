using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools.MapsetMerger;
using Mapping_Tools.Application.Tools.MapsetMerger.Contracts;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.Tools.MapsetMerger;
using Mapping_Tools.Core.Tools.MapsetMerger.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tools.MapsetMerger.Models;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.MapsetMerger.ViewModels;

/// <summary>
///     Owns Mapset Merger's multi-mapset form, safe export execution, and legacy
///     project persistence. The feature intentionally has no QuickRun target
///     because its input is a collection of source directories.
/// </summary>
public sealed partial class MapsetMergerViewModel : SingleRunToolViewModel, IShellProjectFeature<MapsetMergerProject>
{
    private readonly ICurrentBeatmapLocator currentBeatmap;

    private readonly IFilePicker filePicker;

    private readonly IMapsetMergerService merger;
    private readonly IUserNotificationService notifications;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>Creates the Mapset Merger presentation model.</summary>
    /// <param name="merger">Stages and commits the merger operation.</param>
    /// <param name="execution">Coordinates cancellation and background execution.</param>
    /// <param name="filePicker">Presents source and export folder pickers.</param>
    /// <param name="workspace">Supplies the selected beatmap used by the ordinary add action.</param>
    /// <param name="currentBeatmap">Finds the beatmap currently open in osu!.</param>
    /// <param name="directories">Supplies the default export folder.</param>
    /// <param name="notifications">Publishes user-facing validation, cancellation, and failure messages.</param>
    public MapsetMergerViewModel(
        IMapsetMergerService merger,
        IToolExecutionService execution,
        IFilePicker filePicker,
        IBeatmapWorkspace workspace,
        ICurrentBeatmapLocator currentBeatmap,
        IApplicationDirectories directories,
        IUserNotificationService notifications)
        : base(execution, MapsetMergerToolDefinition.Definition)
    {
        this.merger = merger ?? throw new ArgumentNullException(nameof(merger));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        ArgumentNullException.ThrowIfNull(directories);
        ExportPath = directories.Exports;
    }

    /// <summary>Gets the editable source mapset rows in merge order.</summary>
    public ObservableCollection<MapsetMergerItemViewModel> Mapsets { get; } = [];

    /// <summary>Gets or sets the export directory.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Select an export directory.")]
    public partial string ExportPath { get; set; }

    /// <summary>Gets or sets whether the first storyboard is embedded in beatmaps.</summary>
    [ObservableProperty]
    public partial bool MoveSbToBeatmap { get; set; }

    ProjectDefinition<MapsetMergerProject> IShellProjectFeature<MapsetMergerProject>.ProjectDefinition { get; } = new(
        "mapsetmergerproject.json",
        "Mapset Merger Projects",
        static () => new MapsetMergerProject(),
        "mapset-merger-project.json",
        ToolConfigSchema.ForTool(MapsetMergerToolDefinition.Definition.Id));

    MapsetMergerProject IShellProjectFeature<MapsetMergerProject>.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature<MapsetMergerProject>.Install(MapsetMergerProject project)
    {
        ExportPath = project.ExportPath;
        MoveSbToBeatmap = project.MoveSbToBeatmap;
        Mapsets.Clear();
        foreach (var item in project.Mapsets)
            Mapsets.Add(new MapsetMergerItemViewModel(
                filePicker,
                item.Name,
                item.Path));
    }

    /// <summary>Adds the directory containing the current osu! beatmap.</summary>
    [RelayCommand]
    private Task AddMapsetAsync()
    {
        return AddMapsetFromPathAsync(
            workspace.SelectedPaths.FirstOrDefault(),
            "Select a beatmap in the shell or hold Shift to fetch the current osu! beatmap.");
    }

    /// <summary>Adds the mapset containing the beatmap currently open in osu!.</summary>
    [RelayCommand]
    private async Task AddMapsetFromCurrentAsync()
    {
        try
        {
            await AddMapsetFromPathAsync(
                await currentBeatmap.FindCurrentBeatmapAsync(),
                "Open a beatmap in osu! or select one in the shell before adding a mapset.");
        }
        catch (Exception exception)
        {
            await PublishErrorAsync(
                "Could not read the current osu! beatmap",
                exception.Message,
                exception);
        }
    }

    private async Task AddMapsetFromPathAsync(string? beatmapPath, string unavailableMessage)
    {
        string? directory = string.IsNullOrWhiteSpace(beatmapPath)
            ? null
            : Path.GetDirectoryName(beatmapPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            await PublishWarningAsync(unavailableMessage);
            return;
        }

        Mapsets.Add(new MapsetMergerItemViewModel(
            filePicker,
            new DirectoryInfo(directory).Name,
            directory));
    }

    /// <summary>Removes selected rows, or the last row when none is selected.</summary>
    [RelayCommand]
    private void RemoveMapset()
    {
        var selected = Mapsets.Where(item => item.IsSelected).ToList();
        if (selected.Count > 0)
        {
            foreach (var item in selected) Mapsets.Remove(item);

            return;
        }

        if (Mapsets.Count > 0) Mapsets.RemoveAt(Mapsets.Count - 1);
    }

    /// <summary>Chooses the final export directory.</summary>
    [RelayCommand]
    private async Task BrowseExportPathAsync()
    {
        try
        {
            var paths = await filePicker.PickFoldersAsync(new OpenFolderPickerRequest
            {
                Title = "Select export path",
                SuggestedStartLocation = workspace.GetBeatmapPickerStartLocation(ExportPath),
                AllowMultiple = false,
            });
            string? path = paths.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(path)) ExportPath = path;
        }
        catch (Exception exception)
        {
            await PublishErrorAsync(
                "Could not select the export path",
                exception.Message,
                exception);
        }
    }

    /// <inheritdoc />
    protected override bool PrepareRun()
    {
        if (!base.PrepareRun())
        {
            PublishWarning("Select an export directory.");
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        ResolveDuplicateMapsetNames();
        var project = Snapshot();
        var result = await Execution.ExecuteAsync(
            new ToolExecutionRequest<MapsetMergerResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    var merged = await merger.MergeAsync(
                        project,
                        new Progress<double>(value => context.ReportProgress(value, "Merging mapsets")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<MapsetMergerResult>(
                        merged,
                        $"Successfully merged {merged.MapsetsMerged} " + $"{(merged.MapsetsMerged == 1 ? "mapset" : "mapsets")}!");
                }),
            CreateProgress());

        if (result.Status == ToolExecutionStatus.Cancelled)
            await PublishInformationAsync("Mapset Merger was cancelled.");
    }

    private MapsetMergerProject Snapshot()
    {
        return new MapsetMergerProject
        {
            ExportPath = ExportPath,
            MoveSbToBeatmap = MoveSbToBeatmap,
            Mapsets = Mapsets.Select(item => new MapsetMergerServiceOptions.MapsetItem
            {
                Name = item.Name,
                Path = item.Path,
            }).ToList(),
        };
    }

    private void ResolveDuplicateMapsetNames()
    {
        var inputs = Mapsets
            .Select(item => new MapsetMergerInput(item.Name, item.Path))
            .ToList();
        try
        {
            MapsetMergerEngine.ResolveDuplicateMapsetNames(inputs);
        }
        catch (ArgumentException)
        {
            // Keep source validation in the execution service so it can report the failure normally.
            return;
        }

        for (int index = 0; index < inputs.Count; index++) Mapsets[index].Name = inputs[index].Name;
    }

    private Task PublishWarningAsync(string message)
    {
        return notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Warning,
            Tool.DisplayName,
            message));
    }

    private void PublishWarning(string message)
    {
        PublishWarningAsync(message).GetAwaiter().GetResult();
    }

    private Task PublishInformationAsync(string message)
    {
        return notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Information,
            Tool.DisplayName,
            message));
    }

    private Task PublishErrorAsync(string title, string message, Exception exception)
    {
        return notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            title,
            message,
            exception));
    }
}
