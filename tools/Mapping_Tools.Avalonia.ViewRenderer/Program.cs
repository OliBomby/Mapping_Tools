using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.AutoFail;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.RhythmGuide;
using Mapping_Tools.Application.SafetyCopies;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Timeline;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Avalonia.ViewRenderer;
using Mapping_Tools.Desktop;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Desktop.Views.Dialogs;
using ApplicationInvariantInt32Converter = Mapping_Tools.Application.Interactions.Converters.InvariantInt32Converter;

var options = RenderOptions.Parse(args);
if (options.List)
{
    foreach (var type in typeof(App).Assembly.GetTypes().Where(type => typeof(Control).IsAssignableFrom(type) && !type.IsAbstract))
        Console.WriteLine(type.FullName);
    return;
}

AppBuilder.Configure<App>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();
new AvaloniaApplicationThemeService().Apply(
    options.Scenario.EndsWith("light", StringComparison.OrdinalIgnoreCase)
        ? ApplicationTheme.Light
        : ApplicationTheme.Dark);

Control view = options.View switch
{
    "MainWindow" => new MainWindow { DataContext = CreateMainViewModel(options.Scenario) },
    "GetStartedView" => new Border
    {
        Padding = new Thickness(20),
        Background = new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.Parse("#303030")),
        Child = new GetStartedView { DataContext = CreateGetStartedViewModel(options.Scenario) }
    },
    "PreferencesView" => CreatePreferencesView(options.Scenario),
    "RhythmGuideView" => CreateRhythmGuideView(options.Scenario),
    "AutoFailDetectorView" => CreateAutoFailDetectorView(options.Scenario),
    "MapCleanerView" => CreateMapCleanerView(options.Scenario),
    "MessageDialogWindow" => CreateMessageDialog(),
    "ValueDialogWindow" => CreateValueDialog(options.Scenario),
    _ => CreateParameterlessView(options.View),
};
var host = view as Window ?? new Window { Content = view };
host.Width = options.Width;
host.Height = options.Height;
host.SizeToContent = SizeToContent.Manual;
host.Show();
host.Width = options.Width;
host.Height = options.Height;
if (options.Scenario.Equals("help", StringComparison.OrdinalIgnoreCase))
{
    Button helpButton = host.GetVisualDescendants()
        .OfType<Button>()
        .First(button => button.Flyout is not null);
    helpButton.Flyout!.ShowAt(helpButton);
}
if (options.Scenario.Equals("invalid", StringComparison.OrdinalIgnoreCase) &&
    view is MapCleanerView)
{
    TextBox field = host.GetVisualDescendants().OfType<TextBox>().Single();
    field.Focus();
    field.Text = "1/16, nope";
    host.GetVisualDescendants().OfType<ToolRunButton>().Single().Focus();
}
if (options.Scenario.Equals("running", StringComparison.OrdinalIgnoreCase))
{
    foreach (ToolProgressBar progressBar in host.GetVisualDescendants()
                 .OfType<ToolProgressBar>())
    {
        progressBar.Value = 0;
        progressBar.Value = 45;
    }
}
var frame = host.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("Avalonia did not produce a rendered frame.");
Directory.CreateDirectory(Path.GetDirectoryName(options.Output)!);
using (FileStream output = File.Create(options.Output))
{
    frame.Save(output, PngBitmapEncoderOptions.Default);
}
host.Close();
Console.WriteLine(options.Output);

static MessageDialogWindow CreateMessageDialog()
{
    DialogChoiceViewModel[] choices =
    [
        new("OK", isDefault: true, isCancel: false, () => { }),
        new("I WANNA SPEAK TO YOUR MANAGER", isDefault: false, isCancel: true, () => { })
    ];
    return new MessageDialogWindow
    {
        DataContext = new MessageDialogViewModel(
            "Confirm",
            "A project already exists at the selected location. Continuing will replace that file.",
            null,
            choices)
    };
}

static ValueDialogWindow CreateValueDialog(string scenario)
{
    bool invalid = scenario.Equals("invalid", StringComparison.OrdinalIgnoreCase);
    ValueDialogViewModel viewModel = new(
        "Type value",
        "Value",
        invalid ? 0 : 1000,
        "ACCEPT",
        "CANCEL",
        value => invalid
            ? new ValidationResult("Enter a whole number.")
            : ValidationResult.Success,
        _ => { },
        () => { });
    ValueDialogConverter converter = new(
        new ApplicationInvariantInt32Converter(),
        viewModel.SetConversionError);
    ValueDialogWindow window = new()
    {
        DataContext = viewModel
    };
    window.BindValue(converter);
    return window;
}

static MainViewModel CreateMainViewModel(string scenario)
{
    ApplicationSettings settings = CreateSettings(scenario);
    IUserNotificationService notifications = new UserNotificationService();
    RendererBeatmapWorkspace workspace = new(settings);
    GetStartedViewModel getStarted = new(workspace);
    string[] toolNames =
    [
        "Auto-fail Detector",
        "Combo Colour Studio",
        "Geometry Dashboard",
        "Hitsound Copier",
        "Hitsound Preview Helper",
        "Hitsound Studio",
        "Map Cleaner",
        "Mapset Merger",
        "Metadata Manager",
        "Pattern Gallery",
        "Property Transformer",
        "Rhythm Guide",
        "Slider Completionator",
        "Slider Merger",
        "Slider Picturator",
        "Sliderator",
        "Timing Copier",
        "Timing Helper",
        "Tumour Generator 2"
    ];
    PreferencesViewModel preferences = CreatePreferencesViewModel(settings, scenario);
    List<ShellFeatureRegistration> registrations =
    [
        new(
            "get-started",
            "Get started",
            "Home",
            "Onboarding, changelog, support links, and recent beatmaps.",
            ["home", "help", "changelog", "recent", "faq"],
            () => getStarted),
        new(
            "preferences",
            "Preferences",
            "Application",
            "Application preferences.",
            ["settings"],
            () => preferences)
    ];
    registrations.AddRange(toolNames.Select((name, index) =>
        new ShellFeatureRegistration(
            $"render-tool-{index}",
            name,
            "Tools",
            $"Open {name}.",
            [name],
            () => new RendererPlaceholderViewModel(),
            startsSection: index == 0)));

    MainViewModel shell = new(
        new ShellFeatureRegistry(registrations),
        settings,
        notifications,
        new AcceptedLauncher(),
        new ImmediateDispatcher(),
        CreateWorkspaceViewModel(settings, notifications, workspace),
        new RendererBetterSaveService());
    if (scenario.StartsWith("preferences", StringComparison.OrdinalIgnoreCase))
    {
        shell.SelectedFeature = shell.FeatureItems.Single(item => item.Id == "preferences");
    }

    return shell;
}

static GetStartedViewModel CreateGetStartedViewModel(string scenario)
{
    ApplicationSettings settings = CreateSettings(scenario);
    return new GetStartedViewModel(new RendererBeatmapWorkspace(settings));
}

static BeatmapWorkspaceViewModel CreateWorkspaceViewModel(
    ApplicationSettings settings,
    IUserNotificationService notifications,
    IBeatmapWorkspace workspace) =>
    new(
        workspace,
        new RendererBackupService(),
        new RendererQuickUndoService(),
        new RendererFilePicker(),
        new RendererFileRevealService(),
        new RendererApplicationDirectories(),
        settings,
        new RendererDialogService(),
        notifications,
        new ImmediateDispatcher());

static Control CreatePreferencesView(string scenario)
{
    ApplicationSettings settings = CreateSettings(scenario);
    if (scenario.EndsWith("light", StringComparison.OrdinalIgnoreCase))
    {
        settings.Theme = ApplicationTheme.Light;
        new AvaloniaApplicationThemeService().Apply(ApplicationTheme.Light);
    }

    return new Border
    {
        Padding = new Thickness(20),
        Background = new Avalonia.Media.SolidColorBrush(
            settings.Theme == ApplicationTheme.Light
                ? Avalonia.Media.Color.Parse("#FAFAFA")
                : Avalonia.Media.Color.Parse("#303030")),
        Child = new PreferencesView
        {
            DataContext = CreatePreferencesViewModel(settings, scenario)
        }
    };
}

static Control CreateRhythmGuideView(string scenario)
{
    RhythmGuideViewModel viewModel = new(
        RendererStub<IRhythmGuideService>.Create(),
        RendererStub<IToolExecutionService>.Create(),
        new RendererFilePicker(),
        RendererStub<ICurrentBeatmapLocator>.Create(),
        RendererStub<IProjectService>.Create(),
        new RendererDialogService(),
        new RendererFileRevealService(),
        RendererStub<IRhythmGuideWindowService>.Create(),
        new UserNotificationService(),
        new RendererApplicationDirectories());
    viewModel.SourcePathsText = string.Join('|', Enumerable.Range(1, 12)
        .Select(index => $@"C:\Songs\Fixture Map {index}.osu"));
    if (scenario.Equals("running", StringComparison.OrdinalIgnoreCase))
    {
        SetProperty(viewModel, nameof(RhythmGuideViewModel.IsRunning), true);
        SetProperty(viewModel, nameof(RhythmGuideViewModel.Progress), 45d);
    }

    return new RhythmGuideView { DataContext = viewModel };
}

static Control CreateAutoFailDetectorView(string scenario)
{
    ApplicationSettings settings = CreateSettings(string.Empty);
    AutoFailDetectorViewModel viewModel = new(
        RendererStub<IAutoFailService>.Create(),
        RendererStub<IToolExecutionService>.Create(),
        new RendererBeatmapWorkspace(settings),
        RendererStub<ICurrentBeatmapLocator>.Create(),
        settings,
        new RendererDialogService(),
        new QuickRunCommandRegistry(),
        new AcceptedLauncher());
    PrepareToolState(viewModel, scenario);

    return new AutoFailDetectorView { DataContext = viewModel };
}

static Control CreateMapCleanerView(string scenario)
{
    ApplicationSettings settings = CreateSettings(string.Empty);
    MapCleanerViewModel viewModel = new(
        RendererStub<IMapCleanerService>.Create(),
        RendererStub<IToolExecutionService>.Create(),
        new RendererBeatmapWorkspace(settings),
        RendererStub<ICurrentBeatmapLocator>.Create(),
        settings,
        new QuickRunCommandRegistry(),
        RendererStub<IProjectService>.Create(),
        new RendererDialogService(),
        new UserNotificationService(),
        new AcceptedLauncher());
    PrepareToolState(viewModel, scenario);

    return new MapCleanerView { DataContext = viewModel };
}

static void PrepareToolState(object viewModel, string scenario)
{
    if (scenario.Equals("running", StringComparison.OrdinalIgnoreCase))
    {
        SetProperty(viewModel, "IsRunning", true);
        SetProperty(viewModel, "Progress", 45d);
    }

    if (scenario.Equals("timeline", StringComparison.OrdinalIgnoreCase))
    {
        SetProperty(viewModel, "HasRun", true);
        SetProperty(viewModel, "EndTime", 60_000d);
        SetProperty(
            viewModel,
            "Markers",
            new TimelineMarker[]
            {
                new(6_000, TimelineMarkerKind.Added, "Greenline added"),
                new(24_000, TimelineMarkerKind.Changed, "Greenline changed"),
                new(48_000, TimelineMarkerKind.Removed, "Greenline removed")
            });
    }
}

static void SetProperty(object target, string propertyName, object value)
{
    PropertyInfo property = target.GetType().GetProperty(propertyName)
        ?? throw new InvalidOperationException(
            $"Renderer state property '{propertyName}' was not found on {target.GetType().Name}.");
    property.SetValue(target, value);
}

static PreferencesViewModel CreatePreferencesViewModel(
    ApplicationSettings settings,
    string scenario)
{
    PreferencesViewModel viewModel = new(
        settings,
        new RendererFilePicker(),
        new RendererThemeService(),
        new UserNotificationService(),
        new QuickRunCommandRegistry(),
        new RendererHotkeyBindingCoordinator(),
        new RendererBetterSaveOverrideService());
    if (scenario.Equals("preferences-invalid", StringComparison.OrdinalIgnoreCase))
    {
        viewModel.OsuPath = string.Empty;
        viewModel.MaxBackupFiles = 0;
        viewModel.PeriodicBackupInterval = TimeSpan.Zero;
    }
    else if (scenario.Equals(
                 "preferences-periodic-off",
                 StringComparison.OrdinalIgnoreCase))
    {
        viewModel.MakePeriodicBackups = false;
    }

    return viewModel;
}

static ApplicationSettings CreateSettings(string scenario)
{
    ApplicationSettings settings = new()
    {
        OsuPath = @"C:\Games\osu!",
        SongsPath = @"C:\Games\osu!\Songs",
        OsuConfigPath = @"C:\Games\osu!\osu!.Fixture.cfg",
        BackupsPath = @"C:\Mapping Tools\Backups",
        MaxBackupFiles = 1000,
        MakeBackups = true,
        MakePeriodicBackups = true,
        PeriodicBackupInterval = TimeSpan.FromMinutes(10),
        CurrentBeatmapDefaultFolder = true,
        UseEditorReader = true,
        Theme = ApplicationTheme.Dark
    };
    if (scenario.EndsWith("light", StringComparison.OrdinalIgnoreCase))
    {
        settings.Theme = ApplicationTheme.Light;
    }
    if (scenario.Equals("recent-maps", StringComparison.OrdinalIgnoreCase))
    {
        settings.RecentMaps =
        [
            new RecentBeatmap(
                @"C:\Songs\Artist - A Very Long Beatmap Name (Mapper) [Difficulty].osu",
                "26-07-2026 12:34:56"),
            new RecentBeatmap(
                @"C:\Songs\Short Map.osu",
                "25-07-2026 09:10:11")
        ];
    }
    if (scenario.Equals("favorites", StringComparison.OrdinalIgnoreCase))
    {
        settings.FavoriteTools = ["render-tool-2", "render-tool-5"];
    }

    return settings;
}

static Control CreateParameterlessView(string name)
{
    var type = typeof(App).Assembly.GetTypes().SingleOrDefault(candidate =>
        typeof(Control).IsAssignableFrom(candidate) &&
        (candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
         candidate.FullName?.Equals(name, StringComparison.OrdinalIgnoreCase) == true));
    return type is null
        ? throw new ArgumentException($"Unknown Avalonia view '{name}'. Use --list or add a deterministic factory to Program.cs.")
        : (Control)(Activator.CreateInstance(type)
                    ?? throw new InvalidOperationException($"Could not construct '{type.FullName}'. Add a deterministic factory to Program.cs."));
}

namespace Mapping_Tools.Avalonia.ViewRenderer {
    internal class RendererStub<TService> : DispatchProxy
        where TService : class
    {
        public static TService Create() => DispatchProxy.Create<TService, RendererStub<TService>>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Type returnType = targetMethod?.ReturnType ?? typeof(void);
            if (returnType == typeof(Task))
            {
                return Task.CompletedTask;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                Type resultType = returnType.GetGenericArguments()[0];
                object? result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                return typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, [result]);
            }

            return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
        }
    }

    internal sealed record RenderOptions(
        string View,
        string Output,
        double Width,
        double Height,
        bool List,
        string Scenario)
    {
        /// <summary>
        /// Parses renderer command-line options and supplies deterministic defaults.
        /// </summary>
        /// <param name="args">The renderer command-line arguments.</param>
        /// <returns>The requested view, output path, dimensions, and list mode.</returns>
        public static RenderOptions Parse(string[] args)
        {
            string? Value(string key) => args.SkipWhile(value => value != key).Skip(1).FirstOrDefault();
            var view = Value("--view") ?? "MainWindow";
            return new RenderOptions(view,
                Path.GetFullPath(Value("--output") ?? Path.Combine("artifacts", "view-renders", $"avalonia-{view}.png")),
                double.TryParse(Value("--width"), out var width) ? width : 1280,
                double.TryParse(Value("--height"), out var height) ? height : 800,
                args.Contains("--list"),
                Value("--scenario") ?? string.Empty);
        }
    }

    internal sealed class AcceptedLauncher : IPlatformLauncher
    {
        public Task<bool> OpenUriAsync(Uri uri, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> OpenFileAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> OpenFolderAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    internal sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void Post(Action action) => action();
    }

    internal sealed class RendererThemeService : IApplicationThemeService
    {
        public void Apply(ApplicationTheme theme)
        {
        }
    }

    internal sealed class RendererFilePicker : IFilePicker
    {
        public bool CanOpenFiles => false;

        public bool CanSaveFiles => false;

        public bool CanPickFolders => false;

        public Task<IReadOnlyList<string>> PickOpenFilesAsync(
            OpenFilePickerRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string?> PickSaveFileAsync(
            SaveFilePickerRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<IReadOnlyList<string>> PickFoldersAsync(
            OpenFolderPickerRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }

    internal sealed class RendererPlaceholderViewModel : ObservableObject
    {
    }

    internal sealed class RendererBeatmapWorkspace : IBeatmapWorkspace
    {
        private readonly ApplicationSettings _settings;
        private string[] _selectedPaths = [];

        public RendererBeatmapWorkspace(ApplicationSettings settings)
        {
            _settings = settings;
        }

        public event EventHandler<BeatmapSelectionChangedEventArgs>? SelectionChanged;

        public IReadOnlyList<string> SelectedPaths => _selectedPaths;

        public IReadOnlyList<RecentBeatmap> RecentMaps => _settings.RecentMaps;

        public bool RestoreMostRecent()
        {
            if (_settings.RecentMaps.FirstOrDefault() is not { } recent)
            {
                return false;
            }

            SetSelection(
                recent.Path.Split('|', StringSplitOptions.RemoveEmptyEntries),
                BeatmapSelectionSource.Startup);
            return true;
        }

        public void SetSelection(
            IEnumerable<string> paths,
            BeatmapSelectionSource source = BeatmapSelectionSource.Programmatic)
        {
            _selectedPaths = paths.ToArray();
            SelectionChanged?.Invoke(
                this,
                new BeatmapSelectionChangedEventArgs(_selectedPaths, source));
        }

        public void ClearSelection(
            BeatmapSelectionSource source = BeatmapSelectionSource.Programmatic) =>
            SetSelection([], source);

        public bool RemoveRecent(string path) => false;

        public IReadOnlyList<string> GetMissingSelectedPaths() => [];

        public Task<bool> PickBeatmapsAsync(
            bool allowMultiple,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<CurrentBeatmapSelectionResult> SelectCurrentBeatmapAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentBeatmapSelectionResult(
                CurrentBeatmapSelectionStatus.Unavailable,
                null));
    }

    internal sealed class RendererBackupService : IBeatmapBackupService
    {
        public Task<BeatmapBackupResult> CreateAsync(
            IEnumerable<string> sourcePaths,
            BeatmapBackupReason reason,
            bool force = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BeatmapBackupResult([], false));

        public Task<BeatmapBackupResult> CreateAsync(
            BeatmapEditingSession session,
            BeatmapBackupReason reason,
            bool force = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BeatmapBackupResult([], false));

        public Task<BeatmapBackupArtifact?> CreatePeriodicIfChangedAsync(
            BeatmapEditingSession session,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BeatmapBackupArtifact?>(null);

        public Task<BeatmapRestoreResult> RestoreAsync(
            string backupPath,
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BeatmapRestoreResult?> QuickUndoAsync(
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BeatmapRestoreResult?>(null);
    }

    internal sealed class RendererQuickUndoService : IQuickUndoCommandService
    {
        public Task<QuickUndoCommandResult> ExecuteAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new QuickUndoCommandResult(QuickUndoCommandStatus.NoBackup));
    }

    internal sealed class RendererBetterSaveService : IBetterSaveService
    {
        public Task<BetterSaveResult> ExecuteAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BetterSaveResult(BetterSaveStatus.NoCurrentBeatmap));
    }

    internal sealed class RendererHotkeyBindingCoordinator : IHotkeyBindingCoordinator
    {
        public void ApplyQuickRun(HotkeySettings? hotkey)
        {
        }

        public void ApplyQuickUndo(HotkeySettings? hotkey)
        {
        }

        public void ApplyBetterSave(HotkeySettings? hotkey)
        {
        }
    }

    internal sealed class RendererBetterSaveOverrideService : IBetterSaveOverrideService
    {
        public void Configure(string songsPath, bool enabled)
        {
        }

        public void Stop()
        {
        }
    }

    internal sealed class RendererFileRevealService : IFileRevealService
    {
        public Task<bool> RevealAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    internal sealed class RendererApplicationDirectories : IApplicationDirectories
    {
        public string LocalApplicationData => @"C:\Local";

        public string ApplicationData => @"C:\Local\Mapping Tools";

        public string Exports => @"C:\Local\Mapping Tools\Exports";

        public string ConfigurationFile => @"C:\Local\Mapping Tools\config.json";

        public void EnsureCreated()
        {
        }
    }

    internal sealed class RendererDialogService : IDialogService
    {
        public Task<TResult> ShowMessageAsync<TResult>(
            MessageDialogRequest<TResult> request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(request.DismissResult);

        public Task<ValueDialogResult<TValue>> ShowValueAsync<TValue>(
            ValueDialogRequest<TValue> request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ValueDialogResult<TValue>(false, default));
    }
}
