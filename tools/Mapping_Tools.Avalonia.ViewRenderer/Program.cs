using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.Desktop;
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

Control view = options.View switch
{
    "MainWindow" => new MainWindow { DataContext = CreateMainViewModel() },
    "GetStartedView" => new Border
    {
        Padding = new Thickness(20),
        Background = new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.Parse("#303030")),
        Child = new GetStartedView { DataContext = CreateGetStartedViewModel() }
    },
    "MessageDialogWindow" => CreateMessageDialog(),
    "ValueDialogWindow" => CreateValueDialog(),
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

static ValueDialogWindow CreateValueDialog()
{
    return new ValueDialogWindow
    {
        DataContext = new ValueDialogViewModel(
            "Type value",
            "Value",
            string.Empty,
            "ACCEPT",
            "CANCEL",
            text => new ValueInputEvaluation(true, text, null),
            _ => { },
            () => { })
    };
}

static MainViewModel CreateMainViewModel()
{
    ApplicationSettings settings = new();
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
            () => new RendererPlaceholderViewModel())
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

    return new MainViewModel(
        new ShellFeatureRegistry(registrations),
        settings,
        new NoOpSettingsService(settings),
        notifications,
        new ImmediateDispatcher());
}

static GetStartedViewModel CreateGetStartedViewModel()
{
    ApplicationSettings settings = new();
    return new GetStartedViewModel(
        settings,
        new AcceptedLauncher(),
        new UserNotificationService());
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

internal sealed record RenderOptions(string View, string Output, double Width, double Height, bool List)
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
            args.Contains("--list"));
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

internal sealed class RendererPlaceholderViewModel : ViewModelBase
{
}
