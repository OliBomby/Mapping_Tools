using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading.Tasks;
using Mapping_Tools.Application;
using Mapping_Tools.Application.Services;
using Mapping_Tools.Application.Types;
using Mapping_Tools.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop;

static internal class Program {
    public static IHost AppHost { get; private set; } = null!;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args) {
        AppHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) => {
                services.AddApplicationServices();
                services.AddInfrastructure(ctx.Configuration); // different adapters
                services.AddPresentation(); // commands/parsers
            })
            .Build();

        // Invoke application lifetime events
        var appLifetime = AppHost.Services.GetRequiredService<IHostApplicationLifetime>();
        var lifecycle = AppHost.Services.GetRequiredService<IAppLifecycle>();

        appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () => await lifecycle.OnStartAsync()).GetAwaiter().GetResult();
        });

        appLifetime.ApplicationStopping.Register(() =>
        {
            // Has to be run in a different thread to avoid deadlocks
            Task.Run(async () => await lifecycle.OnShutdownAsync()).GetAwaiter().GetResult();
        });

        // Run Avalonia app
        await AppHost.StartAsync();
        int exitCode = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        await AppHost.StopAsync();

        return exitCode;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions())
            .With(new X11PlatformOptions())
            .With(new MacOSPlatformOptions())
            .LogToTrace()
            .UseReactiveUI();
}