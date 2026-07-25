using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Mapping_Tools.Desktop;
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
    "MainWindow" => new MainWindow { DataContext = new MainViewModel() },
    "MessageDialogWindow" => CreateMessageDialog(),
    "ValueDialogWindow" => CreateValueDialog(),
    _ => CreateParameterlessView(options.View),
};
var host = view as Window ?? new Window { Content = view };
host.Width = options.Width;
host.Height = options.Height;
host.SizeToContent = SizeToContent.Manual;
host.Show();
var frame = host.CaptureRenderedFrame()
    ?? throw new InvalidOperationException("Avalonia did not produce a rendered frame.");
Directory.CreateDirectory(Path.GetDirectoryName(options.Output)!);
frame.Save(options.Output);
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
