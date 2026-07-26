using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.ApplicationServices.Workspace;
using Mapping_Tools.Desktop;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Desktop.Views.Dialogs;

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
    return new ValueDialogWindow
    {
        DataContext = new ValueDialogViewModel(
            "Type value",
            "Value",
            invalid ? "not-a-number" : "1000",
            "ACCEPT",
            "CANCEL",
            text => invalid
                ? new ValueInputEvaluation(
                    false,
                    null,
                    "Enter a whole number.")
                : new ValueInputEvaluation(true, text, null),
            _ => { },
            () => { })
    };
}

static MainViewModel CreateMainViewModel(string scenario)
{
    ApplicationSettings settings = CreateSettings(scenario);
    IUserNotificationService notifications = new UserNotificationService();
    GetStartedViewModel getStarted = new(
        settings,
        new AcceptedLauncher(),
        notifications);
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
        new NoOpSettingsService(settings),
        notifications,
        new ImmediateDispatcher());
    if (scenario.StartsWith("preferences", StringComparison.OrdinalIgnoreCase))
    {
        shell.SelectedFeature = shell.FeatureItems.Single(item => item.Id == "preferences");
    }

    return shell;
}

static GetStartedViewModel CreateGetStartedViewModel(string scenario)
{
    ApplicationSettings settings = CreateSettings(scenario);
    return new GetStartedViewModel(
        settings,
        new AcceptedLauncher(),
        new UserNotificationService());
}

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

static PreferencesViewModel CreatePreferencesViewModel(
    ApplicationSettings settings,
    string scenario)
{
    PreferencesViewModel viewModel = new(
        settings,
        new NoOpSettingsService(settings),
        new RendererFilePicker(),
        new RendererThemeService(),
        new UserNotificationService());
    if (scenario.Equals("preferences-invalid", StringComparison.OrdinalIgnoreCase))
    {
        viewModel.OsuPath = string.Empty;
        viewModel.MaxBackupFilesText = "0";
        viewModel.PeriodicBackupIntervalText = "soon";
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

internal sealed class NoOpSettingsService(ApplicationSettings settings) : ISettingsService
{
    public SettingsLoadResult LoadOrCreate() => new(settings, false, false);

    public void Save(ApplicationSettings applicationSettings)
    {
    }
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

internal sealed class RendererPlaceholderViewModel : ViewModelBase
{
}
