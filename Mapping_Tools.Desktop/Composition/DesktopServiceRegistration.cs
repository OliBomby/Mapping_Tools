using Mapping_Tools.ApplicationServices.Abstractions;
using Mapping_Tools.ApplicationServices.Backups;
using Mapping_Tools.ApplicationServices.BeatmapEditing;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.Interactions;
using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Projects;
using Mapping_Tools.ApplicationServices.QuickRun;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.ApplicationServices.Workspace;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Editor;
using Mapping_Tools.Infrastructure.Backups;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Projects;
using Mapping_Tools.Infrastructure.Settings;
using Mapping_Tools.Infrastructure.Workspace;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopServiceRegistration
{
    /// <summary>
    /// Registers the Avalonia shell, platform adapters, application paths,
    /// settings pipeline, and typed project persistence as desktop-lifetime
    /// singletons.
    /// </summary>
    /// <param name="services">The collection that owns the desktop composition root.</param>
    /// <returns>The same collection for registration chaining.</returns>
    public static IServiceCollection AddMappingToolsDesktop(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MainWindow>();
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<GetStartedViewModel>();
        services.AddSingleton<PreferencesViewModel>();
        services.AddSingleton<IShellFeatureRegistry>(provider =>
            new ShellFeatureRegistry(
            [
                new ShellFeatureRegistration(
                    "get-started",
                    "Get started",
                    "Home",
                    "Onboarding, bundled changelog, support links, and recent beatmaps.",
                    ["home", "help", "changelog", "recent", "faq"],
                    provider.GetRequiredService<GetStartedViewModel>),
                new ShellFeatureRegistration(
                    "preferences",
                    "Preferences",
                    "Application",
                    "Paths, backup policy, Editor Reader, and application theme.",
                    ["settings", "paths", "backups", "editor reader", "theme"],
                    provider.GetRequiredService<PreferencesViewModel>)
            ]));
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IDialogService>(provider =>
        {
            MainWindow window = provider.GetRequiredService<MainWindow>();
            return new AvaloniaDialogService(() => window);
        });

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
        services.AddSingleton<IApplicationThemeService, AvaloniaApplicationThemeService>();
        services.AddSingleton<IApplicationDirectories, ApplicationDirectories>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<ISettingsPathEnvironment, WindowsSettingsPathEnvironment>();
        services.AddSingleton<ISettingsPathService, SettingsPathService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton(provider =>
            provider.GetRequiredService<ISettingsService>().LoadOrCreate().Settings);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITextFileStore, FileSystemFileStore>();
        services.AddSingleton<IUserNotificationService, UserNotificationService>();
        services.AddSingleton<ToolExecutionService>();
        services.AddSingleton<IToolExecutionService>(provider =>
            provider.GetRequiredService<ToolExecutionService>());
        services.AddSingleton<QuickRunCommandRegistry>();
        services.AddSingleton<IQuickRunCommandRegistry>(provider =>
            provider.GetRequiredService<QuickRunCommandRegistry>());
        services.AddSingleton<QuickRunService>();
        services.AddSingleton<IQuickRunService>(provider =>
            provider.GetRequiredService<QuickRunService>());
        services.AddSingleton<IGlobalHotkeyService, WindowsGlobalHotkeyService>();
        services.AddSingleton<IBeatmapBackupStore, FileSystemBeatmapBackupStore>();
        services.AddSingleton<IBeatmapBackupService, BeatmapBackupService>();
        services.AddSingleton<IQuickUndoCommandService, QuickUndoCommandService>();
        services.AddSingleton<WindowsEditorReaderAdapter>();
        services.AddSingleton<ILiveBeatmapReader>(provider =>
            provider.GetRequiredService<WindowsEditorReaderAdapter>());
        services.AddSingleton<ICurrentBeatmapLocator>(provider =>
            provider.GetRequiredService<WindowsEditorReaderAdapter>());
        services.AddSingleton<IEditorReloadService, WindowsOsuEditorReloadService>();
        services.AddSingleton<IBeatmapEditingGateway, BeatmapEditingGateway>();
        services.AddSingleton<IBeatmapFileSystem, PhysicalBeatmapFileSystem>();
        services.AddSingleton<IBeatmapWorkspace, BeatmapWorkspace>();
        services.AddSingleton<IProjectSerializer, LegacyProjectJsonSerializer>();
        services.AddSingleton<IProjectStore, FileSystemProjectStore>();
        services.AddSingleton<IProjectService, ProjectService>();

        return services;
    }
}
