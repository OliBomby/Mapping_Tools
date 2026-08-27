using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Interactions;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Models;
using Mapping_Tools.Desktop.Shell;

using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;

/// <summary>Owns Rhythm Guide inputs, execution, projects, and auxiliary-window interaction.</summary>
public sealed partial class RhythmGuideViewModel : SingleRunToolViewModel,
    IShellProjectFeature
{
    private readonly ICurrentBeatmapLocator currentBeatmapLocator;
    private readonly ProjectDefinition<RhythmGuideProject> definition;
    private readonly IFilePicker filePicker;

    private readonly IRhythmGuideService rhythmGuide;
    private readonly IRhythmGuideWindowService windowService;
    private IBeatDivisor[] beatDivisors = DefaultBeatDivisors();

    /// <summary>Creates a Rhythm Guide presentation model.</summary>
    /// <param name="rhythmGuide">Generates framework-independent guide beatmaps.</param>
    /// <param name="execution">Coordinates cancellation, backup, and notifications.</param>
    /// <param name="filePicker">Selects source and destination beatmap files.</param>
    /// <param name="currentBeatmapLocator">Finds the beatmap open in osu!.</param>
    /// <param name="windowService">Opens the auxiliary Rhythm Guide window.</param>
    /// <param name="directories">Supplies the default export directory.</param>
    public RhythmGuideViewModel(
        IRhythmGuideService rhythmGuide,
        IToolExecutionService execution,
        IFilePicker filePicker,
        ICurrentBeatmapLocator currentBeatmapLocator,
        IRhythmGuideWindowService windowService,
        IApplicationDirectories directories)
        : base(execution, RhythmGuideToolDefinition.Definition)
    {
        this.rhythmGuide = rhythmGuide ?? throw new ArgumentNullException(nameof(rhythmGuide));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.currentBeatmapLocator = currentBeatmapLocator ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        this.windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        ArgumentNullException.ThrowIfNull(directories);
        ExportPath = Path.Combine(directories.Exports, "rhythm_guide.osu");
        string defaultExportPath = ExportPath;
        definition = new ProjectDefinition<RhythmGuideProject>(
            "rhythmguideproject.json",
            "Rhythm Guide Projects",
            () => CreateDefaultProject(defaultExportPath),
            "rhythm-guide-project.json");
    }

    /// <summary>Gets or sets the source beatmap paths in selection order.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SourceCount))]
    public partial string[] SourcePaths { get; set; } = [];

    /// <summary>Gets or sets the destination beatmap path.</summary>
    [ObservableProperty]
    public partial string ExportPath { get; set; } = string.Empty;

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

    IProjectDefinition IShellProjectFeature.ProjectDefinition => definition;

    object IShellProjectFeature.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature.Install(object project)
    {
        Install((RhythmGuideProject)project);
    }

    [RelayCommand]
    private async Task BrowseSourcesAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(
            new OpenFilePickerRequest
            {
                Title = "Copy rhythm from",
                SuggestedStartLocation = FirstPathOrNull(),
                AllowMultiple = true,
                Filters = [CommonFilePickerFilters.Beatmaps],
            });
        if (paths.Count > 0) SourcePaths = paths.ToArray();
    }

    [RelayCommand]
    private async Task UseCurrentSourceAsync()
    {
        string? path = await currentBeatmapLocator.FindCurrentBeatmapAsync();
        if (!string.IsNullOrWhiteSpace(path)) SourcePaths = [path];
    }

    [RelayCommand]
    private async Task BrowseExportAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(
            new OpenFilePickerRequest
            {
                Title = "Copy rhythm to",
                SuggestedStartLocation = ExportPath,
                AllowMultiple = false,
                Filters = [CommonFilePickerFilters.Beatmaps],
            });
        if (paths.Count > 0) ExportPath = paths[0];
    }

    [RelayCommand]
    private async Task UseCurrentExportAsync()
    {
        string? path = await currentBeatmapLocator.FindCurrentBeatmapAsync();
        if (!string.IsNullOrWhiteSpace(path)) ExportPath = path;
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        var options = CreateOptions();
        await Execution.ExecuteAsync(
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
                        generated.ExportMode == RhythmGuideExportMode.AddToMap ? "Done!" : null);
                }),
            CreateProgress());
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

    private RhythmGuideProject.RhythmGuideRunOptions CreateOptions()
    {
        return new RhythmGuideProject.RhythmGuideRunOptions
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
        var options = project?.GuideGeneratorArgs
            ?? throw new InvalidDataException("The Rhythm Guide project is incomplete.");
        SourcePaths = options.Paths?.ToArray() ?? [];
        ExportPath = options.ExportPath ?? string.Empty;
        ExportMode = options.ExportMode;
        OutputGameMode = options.OutputGameMode;
        OutputName = options.OutputName ?? string.Empty;
        NcEverything = options.NcEverything;
        SelectionMode = options.SelectionMode;
        beatDivisors = options.BeatDivisors?.ToArray() ?? [];
    }

    private string? FirstPathOrNull()
    {
        return SourcePaths.FirstOrDefault();
    }

    private static RhythmGuideProject CreateDefaultProject(string exportPath)
    {
        return new RhythmGuideProject
        {
            GuideGeneratorArgs = new RhythmGuideProject.RhythmGuideRunOptions { ExportPath = exportPath },
        };
    }

    private static IBeatDivisor[] DefaultBeatDivisors()
    {
        return [new RationalBeatDivisor(16), new RationalBeatDivisor(12)];
    }

}
