using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Infrastructure.Logging;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Application.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mapping_Tools.Desktop;

/// <summary>
///     Owns Avalonia resource initialization and bridges its classic desktop
///     lifetime to the .NET Generic Host.
/// </summary>
public partial class App : Avalonia.Application
{
    private IHost? host;
    private static ILogger<App>? processLogger;

    static App()
    {
        InputElement.PointerPressedEvent.AddClassHandler<Window>(ClearFocusFromBackground);
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    ///     Starts hosted services after Avalonia initialization, resolves the main
    ///     window from the host, and joins host shutdown to the desktop Exit event.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            host = DesktopHostFactory.Create(desktop.Args ?? []);
            processLogger = host.Services.GetRequiredService<ILogger<App>>();
            try
            {
                processLogger.LogInformation("Mapping Tools starting. Version {Version}; OS {OS}; architecture {Architecture}",
                    typeof(App).Assembly.GetName().Version, Environment.OSVersion, System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);
                var settings = host.Services.GetRequiredService<DesktopApplicationSettings>();
                TranslationManager.SetLanguage(settings.Language);
                host.Start();
                host.Services
                    .GetRequiredService<IApplicationThemeService>()
                    .Apply(settings.Theme);
                var mainWindow =
                    host.Services.GetRequiredService<MainWindow>();
                mainWindow.DataContext =
                    host.Services.GetRequiredService<MainViewModel>();
                desktop.MainWindow = mainWindow;
                processLogger.LogInformation("Main window ready");
                desktop.Exit += (_, _) => StopHost();
            }
            catch (Exception exception)
            {
                processLogger.LogCritical(exception, "Mapping Tools startup failed");
                host.Dispose();
                host = null;
                processLogger = null;
                throw;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDispatcherUnhandledException(
        object? sender,
        DispatcherUnhandledExceptionEventArgs eventArgs)
    {
        WriteCrashLog(eventArgs.Exception);
        eventArgs.Handled = true;
    }

    /// <summary>
    ///     Writes an unhandled exception to the normal retained log, including failures
    ///     that occur before the .NET host is available.
    /// </summary>
    /// <param name="exception">The exception that escaped normal application handling.</param>
    internal static void WriteCrashLog(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        try
        {
            if (processLogger is not null)
            {
                processLogger.LogCritical(exception, "Unhandled application exception");
                return;
            }

            string logsPath = Path.Combine(new ApplicationDirectories().ApplicationData, "Logs");
            using SessionFileLoggerProvider provider = new(logsPath);
            provider.CreateLogger(typeof(App).FullName!).LogCritical(exception, "Unhandled application exception before host startup");
        }
        catch (Exception loggingException)
        {
            Trace.TraceError(
                "Could not write the Mapping Tools crash log: {0}",
                loggingException);
        }
    }

    private void StopHost()
    {
        if (host is null) return;

        try
        {
            processLogger?.LogInformation("Mapping Tools shutdown started");
            host.Services
                .GetRequiredService<MainViewModel>()
                .DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();
            host.StopAsync(TimeSpan.FromSeconds(5))
                .GetAwaiter()
                .GetResult();
            processLogger?.LogInformation("Mapping Tools shutdown completed");
        }
        finally
        {
            host.Dispose();
            host = null;
            processLogger = null;
        }
    }

    private static void ClearFocusFromBackground(
        Window window,
        PointerPressedEventArgs eventArgs)
    {
        if (!eventArgs.GetCurrentPoint(window).Properties.IsLeftButtonPressed) return;

        for (var current = eventArgs.Source as Visual;
             current is not null && current != window;
             current = current.GetVisualParent())
            if (current is InputElement { Focusable: true })
                return;

        TopLevel.GetTopLevel(window)?.FocusManager.Focus(null);
    }
}
