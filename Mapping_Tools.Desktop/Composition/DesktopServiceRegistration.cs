using System.Reflection;
using Avalonia.Controls;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Audio;
using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Application.Updates;
using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Services.Hosted;
using Mapping_Tools.Desktop.Services.Platform;
using Mapping_Tools.Desktop.Services.Updates;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Infrastructure.Audio;
using Mapping_Tools.Infrastructure.Backups;
using Mapping_Tools.Infrastructure.Editor;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Projects;
using Mapping_Tools.Infrastructure.Settings;
using Mapping_Tools.Infrastructure.Updates;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopServiceRegistration
{
    /// <summary>
    ///     Registers the Avalonia shell, platform adapters, application paths,
    ///     settings pipeline, and typed project persistence as desktop-lifetime
    ///     singletons.
    /// </summary>
    /// <param name="services">The collection that owns the desktop composition root.</param>
    /// <param name="toolAssemblies">The assemblies that contain the tools to be registered.</param>
    /// <returns>The same collection for registration chaining.</returns>
    public static IServiceCollection AddMappingToolsDesktop(
        this IServiceCollection services,
        IEnumerable<Assembly>? toolAssemblies = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MainWindow>();
        services.AddSingleton<Func<Window>>(provider => () => provider.GetRequiredService<MainWindow>());
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IUpdateGateway, OnovaUpdateGateway>();
        services.AddSingleton<IUpdateService, UpdateService>();
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IUpdaterInteractionService>(provider =>
                new AvaloniaUpdaterInteractionService(
                    () => provider.GetRequiredService<MainWindow>(),
                    provider.GetRequiredService<IUpdateService>(),
                    provider.GetRequiredService<IUserNotificationService>(),
                    () => provider.GetRequiredService<IDialogService>(),
                    provider.GetRequiredService<IUiDispatcher>()));
        }
        services.AddSingleton<BeatmapWorkspaceViewModel>();
        services.AddDesktopFeatures(
            toolAssemblies ?? [typeof(DesktopServiceRegistration).Assembly]);
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddSingleton<IFilePicker>(provider =>
        {
            var window = provider.GetRequiredService<MainWindow>();
            return new AvaloniaFilePicker(() => window.StorageProvider);
        });
        services.AddSingleton<IClipboardService>(provider =>
        {
            var window = provider.GetRequiredService<MainWindow>();
            return new AvaloniaClipboardService(() => window.Clipboard);
        });
        services.AddSingleton<IPlatformLauncher>(provider =>
        {
            var window = provider.GetRequiredService<MainWindow>();
            return new AvaloniaPlatformLauncher(() => window.Launcher);
        });
        services.AddSingleton<IFileRevealService, PortableFileRevealService>();
        services.AddSingleton<IApplicationThemeService, ApplicationThemeService>();
        services.AddSingleton<IApplicationDirectories, ApplicationDirectories>();
        services.AddSingleton<ISettingsStore>(provider =>
            new JsonSettingsStore(
                provider.GetRequiredService<IApplicationDirectories>(),
                typeof(DesktopApplicationSettings)));
        services.AddSingleton<ISettingsPathEnvironment, PortableSettingsPathEnvironment>();
        services.AddSingleton<ISettingsPathService, SettingsPathService>();
        services.AddSingleton<Func<ApplicationSettings>>(
            static _ => static () => new DesktopApplicationSettings());
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<DesktopApplicationSettings>(provider =>
            (DesktopApplicationSettings)provider
                .GetRequiredService<ISettingsService>()
                .LoadOrCreate()
                .Settings);
        services.AddSingleton<ApplicationSettings>(provider =>
            provider.GetRequiredService<DesktopApplicationSettings>());
        services.AddSingleton<SettingsPersistenceHostedService>();
        services.AddHostedService(provider =>
            provider.GetRequiredService<SettingsPersistenceHostedService>());
        services.AddHostedService<MappingToolQuickRunHostedService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<PhysicalBeatmapsetFileSystem>();
        services.AddSingleton<IBeatmapsetFileSystem>(provider =>
            provider.GetRequiredService<PhysicalBeatmapsetFileSystem>());
        services.AddSingleton<ITextFileStore>(provider =>
            provider.GetRequiredService<PhysicalBeatmapsetFileSystem>());
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
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IGlobalHotkeyService, WindowsGlobalHotkeyService>();
        }
        else
        {
            services.AddSingleton<IGlobalHotkeyService, UnsupportedPlatformGlobalHotkeyService>();
        }

        services.AddSingleton<GlobalHotkeyHostedService>();
        services.AddSingleton<IHotkeyBindingCoordinator>(provider =>
            provider.GetRequiredService<GlobalHotkeyHostedService>());
        services.AddSingleton<IBeatmapBackupStore, FileSystemBeatmapBackupStore>();
        services.AddSingleton<IBeatmapBackupService, BeatmapBackupService>();
        services.AddSingleton<IQuickUndoCommandService, QuickUndoCommandService>();
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<WindowsEditorReaderAdapter>();
            services.AddSingleton<ILiveBeatmapReader>(provider =>
                provider.GetRequiredService<WindowsEditorReaderAdapter>());
            services.AddSingleton<ICurrentBeatmapLocator>(provider =>
                provider.GetRequiredService<WindowsEditorReaderAdapter>());
            services.AddSingleton<IEditorReloadService, WindowsOsuEditorReloadService>();
        }
        else
        {
            services.AddSingleton<ILiveBeatmapReader, UnsupportedPlatformLiveBeatmapReader>();
            services.AddSingleton<ICurrentBeatmapLocator, UnsupportedPlatformCurrentBeatmapLocator>();
            services.AddSingleton<IEditorReloadService, UnsupportedPlatformEditorReloadService>();
        }

        services.AddSingleton<IBeatmapEditingGateway, BeatmapEditingGateway>();
        services.AddSingleton<IBetterSaveService, BetterSaveService>();
        services.AddSingleton<IAudioClipMixer, NaudioAudioClipMixer>();
        services.AddSingleton<IAudioDecoder, NaudioAudioDecoder>();
        services.AddSingleton<ISoundFontRenderer, NaudioSoundFontRenderer>();
        services.AddSingleton<IAudioGenerator, NaudioAudioGenerator>();
        services.AddSingleton<IAudioExporter, NaudioAudioExporter>();
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IAudioPlaybackService, NaudioAudioPlaybackService>();
        }
        else
        {
            services.AddSingleton<IAudioPlaybackService, ProcessAudioPlaybackService>();
        }
        services.AddSingleton<IMidiService, NaudioMidiService>();
        services.AddSingleton<AudioExportService>();
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IBetterSaveOverrideService, WindowsBetterSaveOverrideService>();
        }
        else
        {
            services.AddSingleton<IBetterSaveOverrideService, UnsupportedPlatformBetterSaveOverrideService>();
        }
        services.AddSingleton<IBeatmapWorkspace, BeatmapWorkspace>();
        services.AddSingleton<IProjectSerializer, VersionedProjectJsonSerializer>();
        services.AddSingleton<IProjectStore, FileSystemProjectStore>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<ProjectAutosaveCoordinator>();

        return services;
    }
}
