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
    private static readonly FilePickerFilter BeatmapFilter = new(
        "osu! beatmap",
        ["*.osu"],
        ["application/x-osu-beatmap"]);

    private readonly IRhythmGuideService _rhythmGuide;
    private readonly IToolExecutionService _execution;
    private readonly IFilePicker _filePicker;
    private readonly ICurrentBeatmapLocator _currentBeatmapLocator;
    private readonly IProjectService _projects;
    private readonly IDialogService _dialogs;
    private readonly IFileRevealService _fileReveal;
    private readonly IRhythmGuideWindowService _windowService;
    private readonly IUserNotificationService _notifications;
    private readonly ProjectDefinition<RhythmGuideProject> _definition;
    private IBeatDivisor[] _beatDivisors = DefaultBeatDivisors();
    private bool _installing;
    private bool _loadedAutosave;

    [ObservableProperty]
    private string _sourcePathsText = string.Empty;

    [ObservableProperty]
    private string _exportPath;

    [ObservableProperty]
    private RhythmGuideExportMode _exportMode;

    [ObservableProperty]
    private GameMode _outputGameMode;

    [ObservableProperty]
    private string _outputName = "Hitsounds";

    [ObservableProperty]
    private bool _ncEverything;

    [ObservableProperty]
    private RhythmGuideSelectionMode _selectionMode = RhythmGuideSelectionMode.HitsoundEvents;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isDirty;

    public RhythmGuideViewModel(
        IRhythmGuideService rhythmGuide,
        IToolExecutionService execution,
        IFilePicker filePicker,
        ICurrentBeatmapLocator currentBeatmapLocator,
        IProjectService projects,
        IDialogService dialogs,
        IFileRevealService fileReveal,
        IRhythmGuideWindowService windowService,
        IUserNotificationService notifications,
        IApplicationDirectories directories)
    {
        _rhythmGuide = rhythmGuide ?? throw new ArgumentNullException(nameof(rhythmGuide));
        _execution = execution ?? throw new ArgumentNullException(nameof(execution));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _currentBeatmapLocator = currentBeatmapLocator ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _fileReveal = fileReveal ?? throw new ArgumentNullException(nameof(fileReveal));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        ArgumentNullException.ThrowIfNull(directories);
        _exportPath = Path.Combine(directories.Exports, "rhythm_guide.osu");
        string defaultExportPath = _exportPath;
        _definition = new ProjectDefinition<RhythmGuideProject>(
            "rhythmguideproject.json",
            "Rhythm Guide Projects",
            () => CreateDefaultProject(defaultExportPath));
    }

    public IReadOnlyList<RhythmGuideExportMode> ExportModes { get; } =
        Enum.GetValues<RhythmGuideExportMode>();

    public IReadOnlyList<GameMode> OutputGameModes { get; } = Enum.GetValues<GameMode>();

    public IReadOnlyList<RhythmGuideSelectionMode> SelectionModes { get; } =
        Enum.GetValues<RhythmGuideSelectionMode>();

    public int SourceCount => ParsePaths(SourcePathsText).Length;

    public void Activate()
    {
        if (!_loadedAutosave)
        {
            _loadedAutosave = true;
            _ = LoadAutosaveAsync();
        }
    }

    public void Deactivate() => _ = AutoSaveSafelyAsync();

    public async Task SaveProjectAsync(CancellationToken cancellationToken = default)
    {
        string? path = await _projects.SaveAsAsync(
            _definition,
            Snapshot(),
            "rhythm-guide-project.json",
            cancellationToken);
        if (path is not null)
        {
            IsDirty = false;
        }
    }

    public async Task OpenProjectAsync(CancellationToken cancellationToken = default)
    {
        if (!await ConfirmDiscardAsync(cancellationToken))
        {
            return;
        }

        ProjectOpenResult<RhythmGuideProject>? opened = await _projects.OpenAsync(
            _definition,
            cancellationToken);
        if (opened is not null)
        {
            ValidateProject(opened.Project);
            Install(opened.Project);
        }
    }

    public async Task NewProjectAsync(CancellationToken cancellationToken = default)
    {
        if (await ConfirmDiscardAsync(cancellationToken))
        {
            Install(_projects.CreateNew(_definition));
        }
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
                Filters = [BeatmapFilter]
            });
        if (paths.Count > 0)
        {
            SourcePathsText = string.Join('|', paths);
        }
    }

    [RelayCommand]
    private async Task UseCurrentSourceAsync()
    {
        string? path = await _currentBeatmapLocator.FindCurrentBeatmapAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            SourcePathsText = path;
        }
    }

    [RelayCommand]
    private async Task BrowseExportAsync()
    {
        if (ExportMode == RhythmGuideExportMode.AddToMap)
        {
            IReadOnlyList<string> paths = await _filePicker.PickOpenFilesAsync(
                new OpenFilePickerRequest
                {
                    Title = "Copy rhythm to",
                    SuggestedStartLocation = ExportPath,
                    AllowMultiple = false,
                    Filters = [BeatmapFilter]
                });
            if (paths.Count > 0)
            {
                ExportPath = paths[0];
            }
            return;
        }

        string? path = await _filePicker.PickSaveFileAsync(
            new SaveFilePickerRequest
            {
                Title = "Save rhythm guide",
                SuggestedStartLocation = ExportPath,
                SuggestedFileName = Path.GetFileName(ExportPath),
                DefaultExtension = "osu",
                ShowOverwritePrompt = true,
                Filters = [BeatmapFilter]
            });
        if (path is not null)
        {
            ExportPath = path;
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
                            $"Added {generated.AddedObjectCount} rhythm-guide objects.",
                            reloadEditor: generated.ExportMode == RhythmGuideExportMode.AddToMap);
                    }),
                progress);
            if (result.Status == ToolExecutionStatus.Succeeded &&
                result.Value?.ExportMode == RhythmGuideExportMode.NewMap)
            {
                await _fileReveal.RevealAsync(result.Value.ExportPath);
            }
        }
        finally
        {
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
        Paths = ParsePaths(SourcePathsText),
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
        _installing = true;
        SourcePathsText = string.Join('|', options.Paths);
        ExportPath = options.ExportPath;
        ExportMode = options.ExportMode;
        OutputGameMode = options.OutputGameMode;
        OutputName = options.OutputName;
        NcEverything = options.NcEverything;
        SelectionMode = options.SelectionMode;
        _beatDivisors = options.BeatDivisors.ToArray();
        _installing = false;
        IsDirty = false;
    }

    private async Task<bool> ConfirmDiscardAsync(CancellationToken cancellationToken)
    {
        if (!IsDirty)
        {
            return true;
        }
        return await _dialogs.ShowMessageAsync(
            new MessageDialogRequest<bool>(
                "Confirm new project",
                "All unsaved Rhythm Guide changes will be lost. Continue?",
                [
                    new DialogChoice<bool>("Continue", true, IsDefault: true),
                    new DialogChoice<bool>("Cancel", false, IsCancel: true)
                ],
                dismissResult: false),
            cancellationToken);
    }

    private async Task LoadAutosaveAsync()
    {
        try
        {
            RhythmGuideProject project = await _projects.LoadAsync<RhythmGuideProject>(
                _projects.GetAutoSavePath(_definition));
            if (!IsDirty)
            {
                Install(project);
            }
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

    private string? FirstPathOrNull() => ParsePaths(SourcePathsText).FirstOrDefault();

    private static string[] ParsePaths(string value) => value.Split(
        '|',
        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

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

    partial void OnSourcePathsTextChanged(string value)
    {
        OnPropertyChanged(nameof(SourceCount));
        MarkDirty();
    }

    partial void OnExportPathChanged(string value) => MarkDirty();
    partial void OnExportModeChanged(RhythmGuideExportMode value) => MarkDirty();
    partial void OnOutputGameModeChanged(GameMode value) => MarkDirty();
    partial void OnOutputNameChanged(string value) => MarkDirty();
    partial void OnNcEverythingChanged(bool value) => MarkDirty();
    partial void OnSelectionModeChanged(RhythmGuideSelectionMode value) => MarkDirty();

    private void MarkDirty()
    {
        if (!_installing)
        {
            IsDirty = true;
        }
    }
}
