using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Services;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Models;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;

/// <summary>Owns Rhythm Guide inputs, execution, projects, and auxiliary-window interaction.</summary>
public sealed partial class RhythmGuideViewModel : SingleRunToolViewModel,
    IShellProjectFeature<RhythmGuideProject>
{
    private readonly ICurrentBeatmapDialogService currentBeatmapService;
    private readonly ProjectDefinition<RhythmGuideProject> definition;
    private readonly IFilePicker filePicker;
    private readonly IFileRevealService fileRevealService;
    private readonly IBeatmapWorkspace workspace;

    private readonly IRhythmGuideService rhythmGuide;
    private readonly IRhythmGuideWindowService windowService;
    private IBeatDivisor[] beatDivisors = DefaultBeatDivisors();

    /// <summary>Creates a Rhythm Guide presentation model.</summary>
    /// <param name="rhythmGuide">Generates framework-independent guide beatmaps.</param>
    /// <param name="execution">Coordinates cancellation, backup, and notifications.</param>
    /// <param name="filePicker">Selects source and destination beatmap files.</param>
    /// <param name="fileRevealService">Reveals the completed beatmap in the platform file manager.</param>
    /// <param name="currentBeatmapService">Fetches the current beatmap and presents lookup feedback.</param>
    /// <param name="workspace">Supplies the shared default beatmap picker location.</param>
    /// <param name="windowService">Opens the auxiliary Rhythm Guide window.</param>
    /// <param name="directories">Supplies the default export directory.</param>
    public RhythmGuideViewModel(
        IRhythmGuideService rhythmGuide,
        IToolExecutionService execution,
        IFilePicker filePicker,
        IFileRevealService fileRevealService,
        ICurrentBeatmapDialogService currentBeatmapService,
        IBeatmapWorkspace workspace,
        IRhythmGuideWindowService windowService,
        IApplicationDirectories directories)
        : base(execution, RhythmGuideToolDefinition.Definition)
    {
        this.rhythmGuide = rhythmGuide ?? throw new ArgumentNullException(nameof(rhythmGuide));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.fileRevealService = fileRevealService ?? throw new ArgumentNullException(nameof(fileRevealService));
        this.currentBeatmapService = currentBeatmapService
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapService));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        ArgumentNullException.ThrowIfNull(directories);
        ExportPath = Path.Combine(directories.Exports, "rhythm_guide.osu");
        string defaultExportPath = ExportPath;
        definition = new ProjectDefinition<RhythmGuideProject>(
            "rhythmguideproject.json",
            "Rhythm Guide Projects",
            () => CreateDefaultProject(defaultExportPath),
            "rhythm-guide-project.json",
            ToolConfigSchema.ForTool(RhythmGuideToolDefinition.Definition.Id));
    }

    /// <summary>Gets or sets the source beatmap paths in selection order.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SourceCount))]
    public partial string[] SourcePaths { get; set; } = [];

    /// <summary>Gets or sets the destination beatmap path.</summary>
    [ObservableProperty]
    public partial string ExportPath { get; set; }

    /// <summary>Gets or sets whether the guide creates or extends a beatmap.</summary>
    [ObservableProperty]
    public partial RhythmGuideExportMode ExportMode { get; set; }

    /// <summary>Gets or sets the game mode assigned to a new guide beatmap.</summary>
    [ObservableProperty]
    public partial GameMode OutputGameMode { get; set; }

    /// <summary>Gets or sets the difficulty name assigned to a new guide beatmap.</summary>
    [ObservableProperty]
    public partial string OutputName { get; set; } = "Hitsounds";

    /// <summary>Gets or sets whether every generated object uses night-core timing.</summary>
    [ObservableProperty]
    public partial bool NcEverything { get; set; }

    /// <summary>Gets or sets which expanded source events become guide objects.</summary>
    [ObservableProperty]
    public partial RhythmGuideSelectionMode SelectionMode { get; set; } =
        RhythmGuideSelectionMode.HitsoundEvents;

    /// <summary>Gets every supported export mode for selection controls.</summary>
    public IReadOnlyList<RhythmGuideExportMode> ExportModes { get; } =
        Enum.GetValues<RhythmGuideExportMode>();

    /// <summary>Gets every supported osu! game mode for new guide beatmaps.</summary>
    public IReadOnlyList<GameMode> OutputGameModes { get; } = Enum.GetValues<GameMode>();

    /// <summary>Gets every supported rhythm event selection mode.</summary>
    public IReadOnlyList<RhythmGuideSelectionMode> SelectionModes { get; } =
        Enum.GetValues<RhythmGuideSelectionMode>();

    /// <summary>Gets the number of non-empty source beatmap paths.</summary>
    public int SourceCount => SourcePaths.Length;

    ProjectDefinition<RhythmGuideProject> IShellProjectFeature<RhythmGuideProject>.ProjectDefinition => definition;

    RhythmGuideProject IShellProjectFeature<RhythmGuideProject>.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature<RhythmGuideProject>.Install(RhythmGuideProject project)
    {
        Install(project);
    }

    [RelayCommand]
    private async Task BrowseSourcesAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(
            new OpenFilePickerRequest
            {
                Title = "Copy rhythm from",
                SuggestedStartLocation = workspace.GetBeatmapPickerStartLocation(
                    Path.GetDirectoryName(SourcePaths.FirstOrDefault())),
                AllowMultiple = true,
                Filters = [CommonFilePickerFilters.Beatmaps],
            });
        if (paths.Count > 0)
        {
            SourcePaths = paths.ToArray();
        }
    }

    [RelayCommand]
    private async Task UseCurrentSourceAsync()
    {
        string? path = await FetchCurrentBeatmapAsync();
        if (path is not null) SourcePaths = [path];
    }

    [RelayCommand]
    private async Task BrowseExportAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(
            new OpenFilePickerRequest
            {
                Title = "Copy rhythm to",
                SuggestedStartLocation = workspace.GetBeatmapPickerStartLocation(
                    Path.GetDirectoryName(ExportPath)),
                AllowMultiple = false,
                Filters = [CommonFilePickerFilters.Beatmaps],
            });
        if (paths.Count > 0)
        {
            ExportPath = paths[0];
        }
    }

    [RelayCommand]
    private async Task UseCurrentExportAsync()
    {
        string? path = await FetchCurrentBeatmapAsync();
        if (path is not null) ExportPath = path;
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        var options = CreateOptions();
        var result = await Execution.ExecuteAsync(
            new ToolExecutionRequest<RhythmGuideResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    context.ReportProgress(0.1, "Loading beatmaps");
                    var generated = await rhythmGuide.GenerateAsync(
                        options,
                        context.CancellationToken);
                    context.ReportProgress(1, "Complete");
                    return new ToolExecutionOutput<RhythmGuideResult>(
                        generated,
                        "Done!");
                }),
            CreateProgress());
        if (result is { Status: ToolExecutionStatus.Succeeded, Value: { } exported })
            await fileRevealService.RevealAsync(exported.ExportPath);
    }

    [RelayCommand]
    private void OpenAuxiliaryWindow()
    {
        windowService.Show(this);
    }

    private RhythmGuideProject Snapshot()
    {
        return new RhythmGuideProject
        {
            GuideGeneratorArgs = CreateOptions(),
        };
    }

    private RhythmGuideServiceOptions.RhythmGuideRunOptions CreateOptions()
    {
        return new RhythmGuideServiceOptions.RhythmGuideRunOptions
        {
            Paths = SourcePaths.ToArray(),
            ExportPath = ExportPath,
            ExportMode = ExportMode,
            OutputGameMode = OutputGameMode,
            OutputName = OutputName,
            NcEverything = NcEverything,
            SelectionMode = SelectionMode,
            BeatDivisors = beatDivisors.ToArray(),
        };
    }

    private void Install(RhythmGuideProject project)
    {
        var options = project.GuideGeneratorArgs
            ?? throw new InvalidDataException("The Rhythm Guide project is incomplete.");
        SourcePaths = options.Paths.ToArray();
        ExportPath = options.ExportPath;
        ExportMode = options.ExportMode;
        OutputGameMode = options.OutputGameMode;
        OutputName = options.OutputName;
        NcEverything = options.NcEverything;
        SelectionMode = options.SelectionMode;
        beatDivisors = options.BeatDivisors.ToArray();
    }

    private async Task<string?> FetchCurrentBeatmapAsync()
    {
        return await currentBeatmapService.FetchAsync();
    }

    private static RhythmGuideProject CreateDefaultProject(string exportPath)
    {
        return new RhythmGuideProject
        {
            GuideGeneratorArgs = new RhythmGuideServiceOptions.RhythmGuideRunOptions { ExportPath = exportPath },
        };
    }

    private static IBeatDivisor[] DefaultBeatDivisors()
    {
        return [new RationalBeatDivisor(16), new RationalBeatDivisor(12)];
    }

}
