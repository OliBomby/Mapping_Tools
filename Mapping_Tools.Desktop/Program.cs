using Avalonia;
using Avalonia.ReactiveUI;
using System;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop;

class Program {
    public static IHost AppHost { get; private set; } = default!;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) {
        AppHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services => {
                services.AddPlatformServices();

                services.AddSingleton<MainWindowViewModel>();
                services.AddTransient<HomeViewModel>();
                services.AddTransient<SettingsViewModel>();
            })
            .Build();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions { AllowEglInitialization = true })
            .With(new X11PlatformOptions())
            .With(new MacOSPlatformOptions())
            .LogToTrace()
            .UseReactiveUI();
}

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddPlatformServices(this IServiceCollection s) {
        // if (OperatingSystem.IsWindows()) {
        //     s.AddSingleton<IFileDialogService, FileDialogServiceWin>();
        // } else if (OperatingSystem.IsLinux()) {
        //     s.AddSingleton<IFileDialogService, FileDialogServiceLinux>();
        // } else if (OperatingSystem.IsMacOS()) {
        //     s.AddSingleton<IFileDialogService, FileDialogServiceMac>();
        // }

        return s;
    }
}