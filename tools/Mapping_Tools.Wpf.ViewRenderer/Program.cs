using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Mapping_Tools;
using Mapping_Tools.Classes.SystemTools;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var options = RenderOptions.Parse(args);
        var viewTypes = typeof(App).Assembly.GetTypes()
            .Where(type => typeof(FrameworkElement).IsAssignableFrom(type) && !type.IsAbstract)
            .OrderBy(type => type.FullName).ToArray();
        if (options.List)
        {
            foreach (var viewType in viewTypes) Console.WriteLine(viewType.FullName);
            return;
        }

        var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.InitializeComponent();
        MainWindow.HttpClient = new HttpClient(new StaticJsonHandler());
        SettingsManager.Settings.RecentMaps.Clear();
        var type = viewTypes.SingleOrDefault(candidate =>
            candidate.Name.Equals(options.View, StringComparison.OrdinalIgnoreCase) ||
            candidate.FullName?.Equals(options.View, StringComparison.OrdinalIgnoreCase) == true)
            ?? throw new ArgumentException($"Unknown WPF view '{options.View}'. Use --list.");
        FrameworkElement view;
        try
        {
            view = (FrameworkElement)(CreateView(type)
                ?? throw new InvalidOperationException($"Could not construct '{type.FullName}'."));
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"'{type.FullName}' is not safe to construct in isolation. Add a deterministic render scenario before using it as a baseline.", exception);
        }

        FrameworkElement renderTarget;
        HwndSource? presentationSource = null;
        if (view is Window window)
        {
            var content = window.Content as FrameworkElement
                ?? throw new InvalidOperationException($"Window '{type.FullName}' does not have renderable content.");
            window.Content = null;

            var designerHost = new Border
            {
                Width = options.Width,
                Height = options.Height,
                Background = window.Background,
                DataContext = window.DataContext,
                Child = content,
            };
            TextElement.SetForeground(designerHost, window.Foreground);
            TextElement.SetFontFamily(designerHost, window.FontFamily);
            TextElement.SetFontSize(designerHost, window.FontSize);
            TextElement.SetFontWeight(designerHost, window.FontWeight);
            foreach (DictionaryEntry resource in window.Resources)
            {
                designerHost.Resources.Add(resource.Key, resource.Value);
            }

            presentationSource = new HwndSource(new HwndSourceParameters("Mapping Tools WPF View Renderer")
            {
                Width = (int)options.Width,
                Height = (int)options.Height,
                WindowStyle = 0,
                ExtendedWindowStyle = 0x08000000 | 0x00000080, // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
            });
            presentationSource.RootVisual = designerHost;
            renderTarget = designerHost;
        }
        else
        {
            renderTarget = new Border
            {
                Width = options.Width,
                Height = options.Height,
                Background = (Brush?)app.TryFindResource("MaterialDesignPaper") ?? Brushes.White,
                Padding = new Thickness(20),
                Child = view,
            };
        }

        renderTarget.Measure(new Size(options.Width, options.Height));
        renderTarget.Arrange(new Rect(0, 0, options.Width, options.Height));
        renderTarget.UpdateLayout();
        renderTarget.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
        var bitmap = new RenderTargetBitmap((int)options.Width, (int)options.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(renderTarget);
        Directory.CreateDirectory(Path.GetDirectoryName(options.Output)!);
        using var stream = File.Create(options.Output);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
        presentationSource?.Dispose();
        Console.WriteLine(options.Output);
        app.Shutdown();
    }

    private static object? CreateView(Type type)
    {
        if (type == typeof(MainWindow))
        {
            return Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: [false],
                culture: null);
        }

        return Activator.CreateInstance(type);
    }
}

internal sealed class StaticJsonHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]"),
        });
}

internal sealed record RenderOptions(string View, string Output, double Width, double Height, bool List)
{
    public static RenderOptions Parse(string[] args)
    {
        string? Value(string key) => args.SkipWhile(value => value != key).Skip(1).FirstOrDefault();
        var view = Value("--view") ?? "StandardView";
        return new RenderOptions(view,
            Path.GetFullPath(Value("--output") ?? Path.Combine("artifacts", "view-renders", $"wpf-{view}.png")),
            double.TryParse(Value("--width"), out var width) ? width : 1280,
            double.TryParse(Value("--height"), out var height) ? height : 800,
            args.Contains("--list"));
    }
}
