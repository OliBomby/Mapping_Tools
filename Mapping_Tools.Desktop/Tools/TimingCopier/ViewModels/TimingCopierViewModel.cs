using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Tools.TimingCopier.Models;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tools.TimingCopier.Models;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.TimingCopier.ViewModels;

/// <summary>
///     Owns Timing Copier form state, native file selection, project persistence, and execution.
/// </summary>
public sealed partial class TimingCopierViewModel : SingleRunToolViewModel,
    IShellProjectFeature<TimingCopierProject>
{
    private readonly ICurrentBeatmapDialogService currentBeatmapService;

    private readonly IFilePicker filePicker;
    private readonly IUserNotificationService notifications;

    private readonly ITimingCopierService timingCopier;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>
    ///     Creates a Timing Copier presentation model.
    /// </summary>
    /// <param name="timingCopier">Runs the framework-independent timing transformation.</param>
    /// <param name="execution">Coordinates background execution, cancellation, and notifications.</param>
    /// <param name="filePicker">Presents native beatmap file dialogs.</param>
    /// <param name="currentBeatmapService">Fetches the current beatmap and presents lookup feedback.</param>
    /// <param name="notifications">Publishes picker failures.</param>
    /// <param name="workspace">Supplies the current shell map selection for picker locations.</param>
    public TimingCopierViewModel(
        ITimingCopierService timingCopier,
        IToolExecutionService execution,
        IFilePicker filePicker,
        ICurrentBeatmapDialogService currentBeatmapService,
        IUserNotificationService notifications,
        IBeatmapWorkspace workspace)
        : base(execution, TimingCopierToolDefinition.Definition)
    {
        this.timingCopier = timingCopier ?? throw new ArgumentNullException(nameof(timingCopier));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.currentBeatmapService = currentBeatmapService
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapService));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    /// <summary>Gets the three object-placement choices in display order.</summary>
    public IReadOnlyList<TimingCopierResnapMode> ResnapModes { get; } =
        Enum.GetValues<TimingCopierResnapMode>();

    /// <summary>Gets or sets the source beatmap path.</summary>
    [ObservableProperty]
    public partial string ImportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets vertical-bar-separated target beatmap paths.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExportMapCountText))]
    public partial string ExportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets how target markers are positioned after timing is copied.</summary>
    [ObservableProperty]
    public partial TimingCopierResnapMode ResnapMode { get; set; } = TimingCopierResnapMode.PreserveBeatSpacing;

    /// <summary>Gets or sets the positive beat snap divisors used during resnapping.</summary>
    [ObservableProperty]
    public partial IBeatDivisor[] BeatDivisors { get; set; } =
        RationalBeatDivisor.GetDefaultBeatDivisors();

    /// <summary>Gets the legacy singular or plural target-map count label.</summary>
    public string ExportMapCountText
    {
        get
        {
            int count = string.IsNullOrEmpty(ExportPath)
                ? 0
                : ExportPath.Split('|').Length;
            return count == 1 ? "(1) map total" : $"({count}) maps total";
        }
    }

    ProjectDefinition<TimingCopierProject> IShellProjectFeature<TimingCopierProject>.ProjectDefinition { get; } = new(
        "timingcopierproject.json",
        "Timing Copier Projects",
        () => new TimingCopierProject(),
        "timing-copier-project.json",
        ToolConfigSchema.ForTool(TimingCopierToolDefinition.Definition.Id));

    TimingCopierProject IShellProjectFeature<TimingCopierProject>.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature<TimingCopierProject>.Install(TimingCopierProject project)
    {
        Install(project);
    }

    /// <summary>Fetches the current osu! beatmap into the source field.</summary>
    [RelayCommand]
    private async Task ImportLoadAsync()
    {
        string? path = await currentBeatmapService.FetchAsync();
        if (path is not null) ImportPath = path;
    }

    /// <summary>Opens a native picker for the source beatmap.</summary>
    [RelayCommand]
    private async Task ImportBrowseAsync()
    {
        await PickBeatmapsAsync(
            "Copy timing from",
            workspace.GetBeatmapPickerStartLocation(Path.GetDirectoryName(ImportPath)),
            false,
            paths => ImportPath = paths[0]);
    }

    /// <summary>Fetches the current osu! beatmap into the target field.</summary>
    [RelayCommand]
    private async Task ExportLoadAsync()
    {
        string? path = await currentBeatmapService.FetchAsync();
        if (path is not null) ExportPath = path;
    }

    /// <summary>Opens a native multi-select picker for target beatmaps.</summary>
    [RelayCommand]
    private async Task ExportBrowseAsync()
    {
        string? suggestedStartLocation = Path.GetDirectoryName(ImportPath);
        if (string.IsNullOrWhiteSpace(suggestedStartLocation))
        {
            string? currentDirectory = ExportPath
                .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(Path.GetDirectoryName)
                .FirstOrDefault();
            suggestedStartLocation = workspace.GetBeatmapPickerStartLocation(currentDirectory);
        }

        await PickBeatmapsAsync(
            "Copy timing to",
            suggestedStartLocation,
            true,
            paths => ExportPath = string.Join('|', paths));
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        var options = Snapshot();
        await Execution.ExecuteAsync(
                new ToolExecutionRequest<TimingCopierResult>(
                    Tool.Id,
                    Tool.DisplayName,
                    async context =>
                    {
                        var result = await timingCopier.CopyAsync(
                            options,
                            new Progress<double>(value =>
                                context.ReportProgress(value, "Copying timing")),
                            context.CancellationToken);
                        return new ToolExecutionOutput<TimingCopierResult>(
                            result,
                            $"Successfully copied timing to {result.ProcessedCount} " + $"{(result.ProcessedCount == 1 ? "beatmap" : "beatmaps")}!");
                    }),
                CreateProgress())
            .ConfigureAwait(false);
    }

    private TimingCopierProject Snapshot()
    {
        return new TimingCopierProject
        {
            ImportPath = ImportPath,
            ExportPath = ExportPath,
            ResnapMode = ResnapMode,
            BeatDivisors = BeatDivisors.ToArray(),
        };
    }

    private void Install(TimingCopierProject project)
    {
        ImportPath = project.ImportPath;
        ExportPath = project.ExportPath;
        ResnapMode = project.ResnapMode;
        BeatDivisors = project.BeatDivisors.ToArray();
    }

    private async Task PickBeatmapsAsync(
        string title,
        string? suggestedStartLocation,
        bool allowMultiple,
        Action<IReadOnlyList<string>> apply)
    {
        try
        {
            var paths = await filePicker.PickOpenFilesAsync(
                new OpenFilePickerRequest
                {
                    Title = title,
                    SuggestedStartLocation = suggestedStartLocation,
                    AllowMultiple = allowMultiple,
                    Filters = [CommonFilePickerFilters.BeatmapsAndStoryboards],
                });
            if (paths.Count > 0) apply(paths);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync(
                "Could not select beatmaps",
                "The file picker could not return local beatmap paths.",
                exception);
        }
    }

    private Task PublishFailureAsync(string title, string message, Exception exception)
    {
        return notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            title,
            message,
            exception));
    }
}
