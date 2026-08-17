using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.AutoFail;
using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.ComboColourStudio;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.HitsoundPreviewHelper;
using Mapping_Tools.Application.HitsoundCopier;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Application.MetadataManager;
using Mapping_Tools.Application.MapsetMerger;
using Mapping_Tools.Application.PropertyTransformer;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.RhythmGuide;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.SliderCompletionator;
using Mapping_Tools.Application.SliderMerger;
using Mapping_Tools.Application.SliderPicturator;
using Mapping_Tools.Application.TimingCopier;
using Mapping_Tools.Application.TimingHelper;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Infrastructure.Backups;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Editor;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Projects;
using Mapping_Tools.Infrastructure.Settings;
using Mapping_Tools.Infrastructure.Images;
using Mapping_Tools.Infrastructure.Workspace;
using Mapping_Tools.Infrastructure.MapsetMerger;
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
        services.AddSingleton<BeatmapWorkspaceViewModel>();
        services.AddDesktopFeatures();
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
        services.AddSingleton<IRhythmGuideWindowService>(provider =>
            new AvaloniaRhythmGuideWindowService(
                provider.GetRequiredService<MainWindow>));
        services.AddSingleton<IApplicationDirectories, ApplicationDirectories>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<ISettingsPathEnvironment, WindowsSettingsPathEnvironment>();
        services.AddSingleton<ISettingsPathService, SettingsPathService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton(provider =>
            provider.GetRequiredService<ISettingsService>().LoadOrCreate().Settings);
        services.AddSingleton<SettingsPersistenceHostedService>();
        services.AddHostedService(provider =>
            provider.GetRequiredService<SettingsPersistenceHostedService>());
        services.AddHostedService<MappingToolQuickRunHostedService>();
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
        services.AddSingleton<GlobalHotkeyHostedService>();
        services.AddSingleton<IHotkeyBindingCoordinator>(provider =>
            provider.GetRequiredService<GlobalHotkeyHostedService>());
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
        services.AddSingleton<IBetterSaveService, BetterSaveService>();
        services.AddSingleton<IRhythmGuideService, RhythmGuideService>();
        services.AddSingleton<IHitsoundPreviewHelperService, HitsoundPreviewHelperService>();
        services.AddSingleton<IHitsoundCopierService, HitsoundCopierService>();
        services.AddSingleton<IHitsoundSampleService, PhysicalHitsoundSampleService>();
        services.AddSingleton<IAutoFailService, AutoFailService>();
        services.AddSingleton<IMapCleanerService, MapCleanerService>();
        services.AddSingleton<IMetadataManagerService, MetadataManagerService>();
        services.AddSingleton<IPropertyTransformerService, PropertyTransformerService>();
        services.AddSingleton<ITimingCopierService, TimingCopierService>();
        services.AddSingleton<ITimingHelperService, TimingHelperService>();
        services.AddSingleton<ISliderCompletionatorService, SliderCompletionatorService>();
        services.AddSingleton<ISliderMergerService, SliderMergerService>();
        services.AddSingleton<ISliderPicturatorService, SliderPicturatorService>();
        services.AddSingleton<IComboColourStudioService, ComboColourStudioService>();
        services.AddSingleton<IMapsetMergerService, MapsetMergerService>();
        services.AddSingleton<IMapsetFileSystem, PhysicalMapsetFileSystem>();
        services.AddSingleton<IImageFileService, SystemDrawingImageFileService>();
        services.AddSingleton<IMapCleanerSampleService, PhysicalMapCleanerSampleService>();
        services.AddSingleton<IBetterSaveOverrideService, WindowsBetterSaveOverrideService>();
        services.AddSingleton<IBeatmapFileSystem, PhysicalBeatmapFileSystem>();
        services.AddSingleton<IBeatmapWorkspace, BeatmapWorkspace>();
        services.AddSingleton<IProjectSerializer, LegacyProjectJsonSerializer>();
        services.AddSingleton<IProjectStore, FileSystemProjectStore>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<ProjectAutosaveCoordinator>();

        return services;
    }
}
