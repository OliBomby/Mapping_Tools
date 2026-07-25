using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopServiceRegistration
{
    /// <summary>
    /// Registers the Avalonia shell, platform adapters, application paths, and
    /// settings pipeline as desktop-lifetime singletons.
    /// </summary>
    /// <param name="services">The collection that owns the desktop composition root.</param>
    /// <returns>The same collection for registration chaining.</returns>
    public static IServiceCollection AddMappingToolsDesktop(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<IFilePicker>(provider =>
        {
            MainWindow window = provider.GetRequiredService<MainWindow>();
            return new AvaloniaFilePicker(() => window.StorageProvider);
        });
        services.AddSingleton<IClipboardService>(provider =>
        {
            MainWindow window = provider.GetRequiredService<MainWindow>();
            return new AvaloniaClipboardService(() => window.Clipboard);
        });
        services.AddSingleton<IPlatformLauncher>(provider =>
        {
            MainWindow window = provider.GetRequiredService<MainWindow>();
            return new AvaloniaPlatformLauncher(() => window.Launcher);
        });
        services.AddSingleton<IFileRevealService, WindowsFileRevealService>();
        services.AddSingleton<IApplicationDirectories, ApplicationDirectories>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<ISettingsPathEnvironment, WindowsSettingsPathEnvironment>();
        services.AddSingleton<ISettingsPathService, SettingsPathService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        return services;
    }
}
