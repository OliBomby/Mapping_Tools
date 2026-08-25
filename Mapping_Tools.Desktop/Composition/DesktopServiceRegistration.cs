using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Audio;
using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Interactions.Dialogs;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.HitsoundStudio;
using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Tools.MapsetMerger;
using Mapping_Tools.Application.Tools.MapsetMerger.Contracts;
using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Application.Tools.PatternGallery;
using Mapping_Tools.Application.Tools.PatternGallery.Contracts;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Application.Tools.TimingHelper;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Application.Updates;
using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.Tools.HitsoundStudio;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.Interactions.GeometryDashboard;
using Mapping_Tools.Desktop.Interactions.HitsoundStudio;
using Mapping_Tools.Desktop.Interactions.PatternGallery;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Updates;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.ViewModels.GeometryDashboard;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Infrastructure.Audio;
using Mapping_Tools.Infrastructure.Backups;
using Mapping_Tools.Infrastructure.Editor;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Images;
using Mapping_Tools.Infrastructure.MapsetMerger;
using Mapping_Tools.Infrastructure.PatternGallery;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Platform.GeometryDashboard;
using Mapping_Tools.Infrastructure.Projects;
using Mapping_Tools.Infrastructure.Settings;
using Mapping_Tools.Infrastructure.Updates;
using Mapping_Tools.Infrastructure.Workspace;
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
    /// <returns>The same collection for registration chaining.</returns>
    public static IServiceCollection AddMappingToolsDesktop(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MainWindow>();
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IUpdateGateway, OnovaUpdateGateway>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IUpdaterInteractionService>(provider =>
            new AvaloniaUpdaterInteractionService(
                () => provider.GetRequiredService<MainWindow>(),
                provider.GetRequiredService<IUpdateService>(),
                provider.GetRequiredService<IUserNotificationService>(),
                () => provider.GetRequiredService<IDialogService>(),
                provider.GetRequiredService<IUiDispatcher>()));
        services.AddSingleton<BeatmapWorkspaceViewModel>();
        services.AddDesktopFeatures();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IDialogService, AvaloniaDialogService>();
        services.AddSingleton<IPatternGalleryInputDialog, PatternGalleryInputDialog>();
        services.AddSingleton<IHitsoundStudioDialogService>(provider =>
            new HitsoundStudioDialogService(
                provider.GetRequiredService<IFilePicker>()));
        services.AddSingleton<IGeometryDashboardDialogService>(provider =>
            new GeometryDashboardDialogService(() => provider.GetRequiredService<MainWindow>()));

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
        services.AddSingleton<IGeometryDashboardProcessDiscovery, WindowsOsuProcessDiscovery>();
        services.AddSingleton<IGeometryDashboardInputService, WindowsGeometryDashboardInputService>();
        services.AddSingleton<IGeometryDashboardScreenService, WindowsGeometryDashboardScreenService>();
        services.AddSingleton<IGeometryDashboardWindowService, WindowsGeometryDashboardWindowService>();
        services.AddSingleton<IGeometryDashboardRuntime, GeometryDashboardRuntimeService>();
        services.AddSingleton<IGeometryDashboardOverlayHostFactory>(provider =>
            new WindowsGeometryDashboardOverlayHostFactory(
                provider.GetRequiredService<IGeometryDashboardWindowService>()));
        services.AddSingleton<GlobalHotkeyHostedService>();
        services.AddSingleton<IHotkeyBindingCoordinator>(provider =>
            provider.GetRequiredService<GlobalHotkeyHostedService>());
        services.AddSingleton<IBeatmapBackupStore, FileSystemBeatmapBackupStore>();
        services.AddSingleton<IBeatmapBackupService, BeatmapBackupService>();
        services.AddSingleton<IQuickUndoCommandService, QuickUndoCommandService>();
        services.AddSingleton<WindowsEditorReaderAdapter>();
        services.AddSingleton<ILiveBeatmapReader>(provider =>
            provider.GetRequiredService<WindowsEditorReaderAdapter>());
        services.AddSingleton<IGeometryDashboardEditorReader>(provider =>
            provider.GetRequiredService<WindowsEditorReaderAdapter>());
        services.AddSingleton<ICurrentBeatmapLocator>(provider =>
            provider.GetRequiredService<WindowsEditorReaderAdapter>());
        services.AddSingleton<IEditorReloadService, WindowsOsuEditorReloadService>();
        services.AddSingleton<IBeatmapEditingGateway, BeatmapEditingGateway>();
        services.AddSingleton<IBetterSaveService, BetterSaveService>();
        services.AddSingleton<IRhythmGuideService, RhythmGuideService>();
        services.AddSingleton<IHitsoundPreviewHelperService, HitsoundPreviewHelperService>();
        services.AddSingleton<HitsoundStudioEngine>();
        services.AddSingleton<IAudioClipMixer, NaudioAudioClipMixer>();
        services.AddSingleton<IHitsoundStudioFileSystem, PhysicalHitsoundStudioFileSystem>();
        services.AddSingleton<IHitsoundStudioService, HitsoundStudioService>();
        services.AddSingleton<IHitsoundCopierService, HitsoundCopierService>();
        services.AddSingleton<IHitsoundSampleService, PhysicalHitsoundSampleService>();
        services.AddSingleton<IAudioDecoder, NaudioAudioDecoder>();
        services.AddSingleton<IAudioEffectService, NaudioAudioEffectService>();
        services.AddSingleton<ISoundFontRenderer, NaudioSoundFontRenderer>();
        services.AddSingleton<IAudioGenerator, NaudioAudioGenerator>();
        services.AddSingleton<IAudioExporter, NaudioAudioExporter>();
        services.AddSingleton<IAudioPlaybackService, NaudioAudioPlaybackService>();
        services.AddSingleton<IMidiService, NaudioMidiService>();
        services.AddSingleton<ISpectrumCalculator, FastFourierSpectrumCalculator>();
        services.AddSingleton<AudioPreviewService>();
        services.AddSingleton<AudioExportService>();
        services.AddSingleton<IAutoFailService, AutoFailService>();
        services.AddSingleton<IMapCleanerService, MapCleanerService>();
        services.AddSingleton<IMetadataManagerService, MetadataManagerService>();
        services.AddSingleton<IPropertyTransformerService, PropertyTransformerService>();
        services.AddSingleton<ITimingCopierService, TimingCopierService>();
        services.AddSingleton<ITimingHelperService, TimingHelperService>();
        services.AddSingleton<ISliderCompletionatorService, SliderCompletionatorService>();
        services.AddSingleton<ISliderMergerService, SliderMergerService>();
        services.AddSingleton<ISliderPicturatorService, SliderPicturatorService>();
        services.AddSingleton<ISlideratorService, SlideratorService>();
        services.AddSingleton<ITumourGeneratorService, TumourGeneratorService>();
        services.AddSingleton<IComboColourStudioService, ComboColourStudioService>();
        services.AddSingleton<IMapsetMergerService, MapsetMergerService>();
        services.AddSingleton<IPatternGalleryService, PatternGalleryService>();
        services.AddSingleton<IPatternGalleryFileService, PatternGalleryFileService>();
        services.AddSingleton<IPatternGalleryArchiveService, PatternGalleryArchiveService>();
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
