using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.RhythmGuide;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Owns Rhythm Guide inputs, execution, projects, and auxiliary-window interaction.</summary>
public sealed partial class RhythmGuideViewModel : ObservableObject,
    IShellFeatureActivation,
    IShellProjectFeature
{
    private const string OperationId = "rhythm-guide";

    private readonly IRhythmGuideService _rhythmGuide;
    private readonly IToolExecutionService _execution;
    private readonly IFilePicker _filePicker;
    private readonly ICurrentBeatmapLocator _currentBeatmapLocator;
    private readonly IProjectService _projects;
    private readonly IRhythmGuideWindowService _windowService;
    private readonly IUserNotificationService _notifications;
    private readonly ProjectDefinition<RhythmGuideProject> _definition;
    private IBeatDivisor[] _beatDivisors = DefaultBeatDivisors();
    private bool _loadedAutosave;

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

    /// <summary>Gets whether generation is currently running.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    public partial bool IsRunning { get; private set; }

    /// <summary>Gets the current generation completion percentage.</summary>
    [ObservableProperty]
    public partial double Progress { get; private set; }

    /// <summary>Creates a Rhythm Guide presentation model.</summary>
    /// <param name="rhythmGuide">Generates framework-independent guide beatmaps.</param>
    /// <param name="execution">Coordinates cancellation, backup, and notifications.</param>
    /// <param name="filePicker">Selects source and destination beatmap files.</param>
    /// <param name="currentBeatmapLocator">Finds the beatmap open in osu!.</param>
    /// <param name="projects">Loads, saves, and autosaves typed projects.</param>
    /// <param name="windowService">Opens the auxiliary Rhythm Guide window.</param>
    /// <param name="notifications">Publishes project persistence failures.</param>
    /// <param name="directories">Supplies the default export directory.</param>
    public RhythmGuideViewModel(
        IRhythmGuideService rhythmGuide,
        IToolExecutionService execution,
        IFilePicker filePicker,
        ICurrentBeatmapLocator currentBeatmapLocator,
        IProjectService projects,
        IRhythmGuideWindowService windowService,
        IUserNotificationService notifications,
        IApplicationDirectories directories)
    {
        _rhythmGuide = rhythmGuide ?? throw new ArgumentNullException(nameof(rhythmGuide));
        _execution = execution ?? throw new ArgumentNullException(nameof(execution));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _currentBeatmapLocator = currentBeatmapLocator ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        ArgumentNullException.ThrowIfNull(directories);
        ExportPath = Path.Combine(directories.Exports, "rhythm_guide.osu");
        string defaultExportPath = ExportPath;
        _definition = new ProjectDefinition<RhythmGuideProject>(
            "rhythmguideproject.json",
            "Rhythm Guide Projects",
            () => CreateDefaultProject(defaultExportPath));
    }

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

    /// <summary>Restores the autosaved project the first time this feature is activated.</summary>
    public void Activate()
    {
        if (!_loadedAutosave)
        {
            _loadedAutosave = true;
            _ = LoadAutosaveAsync();
        }
    }

    /// <summary>Schedules autosave when the feature leaves the shell content area.</summary>
    public void Deactivate() => _ = AutoSaveSafelyAsync();

    /// <summary>Prompts for a destination and saves the current Rhythm Guide project.</summary>
    /// <param name="cancellationToken">Cancels the picker or save.</param>
    /// <returns>A task that completes after the save attempt.</returns>
    public async Task SaveProjectAsync(CancellationToken cancellationToken = default)
    {
        await _projects.SaveAsAsync(
            _definition,
            Snapshot(),
            "rhythm-guide-project.json",
            cancellationToken);
    }

    /// <summary>Opens a Rhythm Guide project selected by the user.</summary>
    /// <param name="cancellationToken">Cancels picking or loading.</param>
    /// <returns>A task that completes after the open attempt.</returns>
    public async Task OpenProjectAsync(CancellationToken cancellationToken = default)
    {
        ProjectOpenResult<RhythmGuideProject>? opened = await _projects.OpenAsync(
            _definition,
            cancellationToken);
        if (opened is not null)
        {
            ValidateProject(opened.Project);
            Install(opened.Project);
        }
    }

    /// <summary>Installs a new default Rhythm Guide project.</summary>
    /// <param name="cancellationToken">Unused cancellation token retained by the project-feature contract.</param>
    /// <returns>A task that completes after the new-project attempt.</returns>
    public Task NewProjectAsync(CancellationToken cancellationToken = default)
    {
        Install(_projects.CreateNew(_definition));
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task BrowseSourcesAsync()
    {
        IReadOnlyList<string> paths = await _filePicker.PickOpenFilesAsync(
            new OpenFilePickerRequest
            {
                Title = "Copy rhythm from",
                SuggestedStartLocation = FirstPathOrNull(),
                AllowMultiple = true,
                Filters = [CommonFilePickerFilters.Beatmaps]
            });
        if (paths.Count > 0)
        {
            SourcePaths = paths.ToArray();
        }
    }

    [RelayCommand]
    private async Task UseCurrentSourceAsync()
    {
        string? path = await _currentBeatmapLocator.FindCurrentBeatmapAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            SourcePaths = [path];
        }
    }

    [RelayCommand]
    private async Task BrowseExportAsync()
    {
        IReadOnlyList<string> paths = await _filePicker.PickOpenFilesAsync(
            new OpenFilePickerRequest
            {
                Title = "Copy rhythm to",
                SuggestedStartLocation = ExportPath,
                AllowMultiple = false,
                Filters = [CommonFilePickerFilters.Beatmaps]
            });
        if (paths.Count > 0)
        {
            ExportPath = paths[0];
        }
    }

    [RelayCommand]
    private async Task UseCurrentExportAsync()
    {
        string? path = await _currentBeatmapLocator.FindCurrentBeatmapAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            ExportPath = path;
        }
    }

    private bool CanRun() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        RhythmGuideOptions options = CreateOptions();
        IsRunning = true;
        Progress = 0;
        Progress<ToolExecutionProgress> progress = new(value => Progress = value.Percent);
        try
        {
            ToolExecutionResult<RhythmGuideResult> result = await _execution.ExecuteAsync(
                new ToolExecutionRequest<RhythmGuideResult>(
                    OperationId,
                    "Rhythm Guide",
                    async context =>
                    {
                        context.ReportProgress(10, "Loading beatmaps");
                        RhythmGuideResult generated = await _rhythmGuide.GenerateAsync(
                            options,
                            context.CancellationToken);
                        context.ReportProgress(100, "Complete");
                        return new ToolExecutionOutput<RhythmGuideResult>(
                            generated,
                            generated.ExportMode == RhythmGuideExportMode.AddToMap ? "Done!" : null);
                    }),
                progress);
        }
        finally
        {
            Progress = 0;
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _execution.Cancel(OperationId);

    [RelayCommand]
    private void OpenAuxiliaryWindow() => _windowService.Show(this);

    private RhythmGuideProject Snapshot() => new()
    {
        GuideGeneratorArgs = CreateOptions()
    };

    private RhythmGuideOptions CreateOptions() => new()
    {
        Paths = SourcePaths.ToArray(),
        ExportPath = ExportPath,
        ExportMode = ExportMode,
        OutputGameMode = OutputGameMode,
        OutputName = OutputName,
        NcEverything = NcEverything,
        SelectionMode = SelectionMode,
        BeatDivisors = _beatDivisors.ToArray()
    };

    private void Install(RhythmGuideProject project)
    {
        ValidateProject(project);
        RhythmGuideOptions options = project.GuideGeneratorArgs;
        SourcePaths = options.Paths.ToArray();
        ExportPath = options.ExportPath;
        ExportMode = options.ExportMode;
        OutputGameMode = options.OutputGameMode;
        OutputName = options.OutputName;
        NcEverything = options.NcEverything;
        SelectionMode = options.SelectionMode;
        _beatDivisors = options.BeatDivisors.ToArray();
    }

    private async Task LoadAutosaveAsync()
    {
        try
        {
            RhythmGuideProject project = await _projects.LoadAsync<RhythmGuideProject>(
                _projects.GetAutoSavePath(_definition));
            Install(project);
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception exception)
        {
            await PublishProjectFailureAsync("Project could not be loaded", exception);
        }
    }

    private async Task AutoSaveSafelyAsync()
    {
        try
        {
            await _projects.AutoSaveAsync(_definition, Snapshot());
        }
        catch (Exception exception)
        {
            await PublishProjectFailureAsync("Project could not be saved", exception);
        }
    }

    private Task PublishProjectFailureAsync(string message, Exception exception) =>
        _notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            "Rhythm Guide",
            message,
            exception));

    private string? FirstPathOrNull() => SourcePaths.FirstOrDefault();

    private static RhythmGuideProject CreateDefaultProject(string exportPath) => new()
    {
        GuideGeneratorArgs = new RhythmGuideOptions { ExportPath = exportPath }
    };

    private static IBeatDivisor[] DefaultBeatDivisors() =>
        [new RationalBeatDivisor(16), new RationalBeatDivisor(12)];

    private static void ValidateProject(RhythmGuideProject project)
    {
        if (project?.GuideGeneratorArgs is null ||
            project.GuideGeneratorArgs.Paths is null ||
            project.GuideGeneratorArgs.BeatDivisors is null ||
            project.GuideGeneratorArgs.BeatDivisors.Length == 0 ||
            string.IsNullOrWhiteSpace(project.GuideGeneratorArgs.OutputName) ||
            string.IsNullOrWhiteSpace(project.GuideGeneratorArgs.ExportPath) ||
            !Enum.IsDefined(project.GuideGeneratorArgs.ExportMode) ||
            !Enum.IsDefined(project.GuideGeneratorArgs.SelectionMode) ||
            !Enum.IsDefined(project.GuideGeneratorArgs.OutputGameMode))
        {
            throw new InvalidDataException("The Rhythm Guide project is incomplete.");
        }
    }

}
