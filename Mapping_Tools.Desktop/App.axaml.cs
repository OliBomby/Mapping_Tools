using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Mapping_Tools.Desktop;

/// <summary>
/// Owns Avalonia resource initialization and bridges its classic desktop
/// lifetime to the .NET Generic Host.
/// </summary>
public partial class App : Avalonia.Application
{
    private IHost? _host;

    static App()
    {
        InputElement.PointerPressedEvent.AddClassHandler<Window>(ClearFocusFromBackground);
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Starts hosted services after Avalonia initialization, resolves the main
    /// window from the host, and joins host shutdown to the desktop Exit event.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _host = DesktopHostFactory.Create(desktop.Args ?? []);
            try
            {
                _host.Start();
                ApplicationSettings settings =
                    _host.Services.GetRequiredService<ApplicationSettings>();
                _host.Services
                    .GetRequiredService<IApplicationThemeService>()
                    .Apply(settings.Theme);
                MainWindow mainWindow =
                    _host.Services.GetRequiredService<MainWindow>();
                mainWindow.DataContext =
                    _host.Services.GetRequiredService<MainViewModel>();
                desktop.MainWindow = mainWindow;
                desktop.Exit += (_, _) => StopHost();
            }
            catch
            {
                _host.Dispose();
                _host = null;
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
    /// Writes an unhandled-exception report to the legacy-compatible application
    /// data directory so the Avalonia frontend has the same support handoff as WPF.
    /// </summary>
    /// <param name="exception">The exception that escaped normal application handling.</param>
    internal static void WriteCrashLog(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        try
        {
            string localApplicationData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localApplicationData))
            {
                localApplicationData = AppContext.BaseDirectory;
            }

            string applicationData = Path.Combine(localApplicationData, "Mapping Tools");
            Directory.CreateDirectory(applicationData);

            List<string> lines = [
                exception.Message,
                exception.StackTrace ?? string.Empty,
                exception.Source ?? string.Empty
            ];
            for (Exception? inner = exception.InnerException;
                 inner is not null;
                 inner = inner.InnerException)
            {
                lines.Add(string.Empty);
                lines.Add("Inner exception:");
                lines.Add(inner.Message);
                lines.Add(inner.StackTrace ?? string.Empty);
                lines.Add(inner.Source ?? string.Empty);
            }

            File.WriteAllLines(Path.Combine(applicationData, "crash-log.txt"), lines);
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
        if (_host is null)
        {
            return;
        }

        try
        {
            _host.StopAsync(TimeSpan.FromSeconds(5))
                .GetAwaiter()
                .GetResult();
        }
        finally
        {
            _host.Dispose();
            _host = null;
        }
    }

    private static void ClearFocusFromBackground(
        Window window,
        PointerPressedEventArgs eventArgs)
    {
        if (!eventArgs.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            return;
        }

        for (Visual? current = eventArgs.Source as Visual;
             current is not null && current != window;
             current = current.GetVisualParent())
        {
            if (current is InputElement { Focusable: true })
            {
                return;
            }
        }

        TopLevel.GetTopLevel(window)?.FocusManager?.Focus(null);
    }
}
