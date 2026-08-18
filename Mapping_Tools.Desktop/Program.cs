using Avalonia;
using System;

namespace Mapping_Tools.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    /// <summary>
    /// Starts Mapping Tools with Avalonia's classic desktop lifetime.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to Avalonia.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception)
            {
                App.WriteCrashLog(exception);
            }
        };
        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
        {
            App.WriteCrashLog(eventArgs.Exception);
            eventArgs.SetObserved();
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            App.WriteCrashLog(exception);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    /// <summary>
    /// Creates the shared Avalonia configuration used by the executable and designer.
    /// </summary>
    /// <returns>An application builder configured for platform detection, Inter, and tracing.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
