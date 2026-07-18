using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Mapping_Tools.Desktop;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;

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
    _ => CreateParameterlessView(options.View),
};
var host = view as Window ?? new Window { Content = view };
host.Width = options.Width;
host.Height = options.Height;
host.Show();
var frame = host.CaptureRenderedFrame()
    ?? throw new InvalidOperationException("Avalonia did not produce a rendered frame.");
Directory.CreateDirectory(Path.GetDirectoryName(options.Output)!);
frame.Save(options.Output);
host.Close();
Console.WriteLine(options.Output);

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
