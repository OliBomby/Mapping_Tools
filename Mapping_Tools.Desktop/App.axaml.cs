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
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop;

/// <summary>
///     Owns Avalonia resource initialization and bridges its classic desktop
///     lifetime to the .NET Generic Host.
/// </summary>
public partial class App : Avalonia.Application
{
    private IHost? host;

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
            try
            {
                host.Start();
                var settings =
                    host.Services.GetRequiredService<DesktopApplicationSettings>();
                host.Services
                    .GetRequiredService<IApplicationThemeService>()
                    .Apply(settings.Theme);
                var mainWindow =
                    host.Services.GetRequiredService<MainWindow>();
                mainWindow.DataContext =
                    host.Services.GetRequiredService<MainViewModel>();
                desktop.MainWindow = mainWindow;
                desktop.Exit += (_, _) => StopHost();
            }
            catch
            {
                host.Dispose();
                host = null;
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
    ///     Writes an unhandled-exception report to the legacy-compatible application
    ///     data directory so the Avalonia frontend has the same support handoff as WPF.
    /// </summary>
    /// <param name="exception">The exception that escaped normal application handling.</param>
    internal static void WriteCrashLog(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        try
        {
            string localApplicationData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localApplicationData)) localApplicationData = AppContext.BaseDirectory;

            string applicationData = Path.Combine(localApplicationData, "Mapping Tools");
            Directory.CreateDirectory(applicationData);

            List<string> lines =
            [
                exception.Message,
                exception.StackTrace ?? string.Empty,
                exception.Source ?? string.Empty,
            ];
            for (var inner = exception.InnerException;
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
        if (host is null) return;

        try
        {
            host.Services
                .GetRequiredService<MainViewModel>()
                .DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();
            host.StopAsync(TimeSpan.FromSeconds(5))
                .GetAwaiter()
                .GetResult();
        }
        finally
        {
            host.Dispose();
            host = null;
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
