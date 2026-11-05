using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
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
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Application.Tools.TimingHelper;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Mapping_Tools.Core.Tools.HitsoundStudio;
using Mapping_Tools.Infrastructure.Audio;
using Mapping_Tools.Infrastructure.Backups;
using Mapping_Tools.Infrastructure.Editor;
using Mapping_Tools.Infrastructure.Editor.Mtipc;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Projects;
using Mapping_Tools.Infrastructure.Settings;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Infrastructure.Tools.HitsoundCopier;
using Mapping_Tools.Infrastructure.Tools.MapCleaner;
using Mapping_Tools.Infrastructure.Tools.PatternGallery;
using Mapping_Tools.Infrastructure.Tools.SliderPicturator;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Cli.Composition;

/// <summary>Composes Application use cases and Infrastructure adapters for one CLI invocation without Desktop presentation services.</summary>
internal static class CliServices
{
    /// <summary>Builds the service provider owned and disposed by the invocation context.</summary>
    /// <param name="settings">Effective CLI settings after saved defaults and explicit overrides.</param>
    /// <param name="saved">Receives each successfully saved file path for completed-edit reporting.</param>
    /// <param name="geometry">Prepared Geometry Dashboard preferences to register for its service, or null for other tools.</param>
    /// <returns>A provider whose lifetime must cover imports and execution, then be disposed.</returns>
    internal static ServiceProvider Create(
        ApplicationSettings settings,
        Action<string> saved,
        GeometryDashboardServiceOptions? geometry = null)
    {
        ServiceCollection services = new();
        services.AddSingleton(settings);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(_ => new HttpClient());

        services.AddSingleton<IApplicationDirectories, ApplicationDirectories>();
        services.AddSingleton<ISettingsPathEnvironment, PortableSettingsPathEnvironment>();
        services.AddSingleton<ISettingsPathService, SettingsPathService>();

        services.AddSingleton<IBeatmapsetFileSystem, PhysicalBeatmapsetFileSystem>();
        services.AddSingleton<ITextFileStore, PhysicalBeatmapsetFileSystem>();
        services.AddSingleton<IBeatmapDecoder, BeatmapDecoder>();
        services.AddSingleton<IBeatmapEncoder, BeatmapEncoder>();
        services.AddSingleton<IStoryboardDecoder, StoryboardDecoder>();
        services.AddSingleton<IStoryboardEncoder, StoryboardEncoder>();

        services.AddSingleton<IBeatmapBackupStore, FileSystemBeatmapBackupStore>();
        services.AddSingleton<IBeatmapBackupService, BeatmapBackupService>();

        services.AddSingleton<IProjectSerializer, VersionedProjectJsonSerializer>();
        services.AddSingleton<IProjectStore, FileSystemProjectStore>();

        services.AddSingleton<IAudioDecoder, NaudioAudioDecoder>();
        services.AddSingleton<ISoundFontRenderer, NaudioSoundFontRenderer>();
        services.AddSingleton<IAudioGenerator, NaudioAudioGenerator>();
        services.AddSingleton<IAudioExporter, NaudioAudioExporter>();
        services.AddSingleton<IAudioClipMixer, NaudioAudioClipMixer>();
        services.AddSingleton<IMidiService, NaudioMidiService>();

        services.AddSingleton<IImageFileService, SkiaSharpImageFileService>();
        services.AddSingleton<IHitsoundSampleService, PhysicalHitsoundSampleService>();
        services.AddSingleton<IMapCleanerSampleService, PhysicalMapCleanerSampleService>();

        services.AddSingleton<IPatternGalleryService, PatternGalleryService>();
        services.AddSingleton<IPatternGalleryFileService, PatternGalleryFileService>();
        services.AddSingleton<IPatternGalleryArchiveService, PatternGalleryArchiveService>();
        services.AddSingleton<IAutoFailService, AutoFailService>();
        services.AddSingleton<IComboColourStudioService, ComboColourStudioService>();
        services.AddSingleton<IHitsoundCopierService, HitsoundCopierService>();
        services.AddSingleton<IHitsoundPreviewHelperService, HitsoundPreviewHelperService>();
        services.AddSingleton<IHitsoundStudioService, HitsoundStudioService>();
        services.AddSingleton<IMapCleanerService, MapCleanerService>();
        services.AddSingleton<IMapsetMergerService, MapsetMergerService>();
        services.AddSingleton<IMetadataManagerService, MetadataManagerService>();
        services.AddSingleton<IPropertyTransformerService, PropertyTransformerService>();
        services.AddSingleton<IRhythmGuideService, RhythmGuideService>();
        services.AddSingleton<ISlideratorService, SlideratorService>();
        services.AddSingleton<ISliderCompletionatorService, SliderCompletionatorService>();
        services.AddSingleton<ISliderMergerService, SliderMergerService>();
        services.AddSingleton<ISliderPicturatorService, SliderPicturatorService>();
        services.AddSingleton<ITimingCopierService, TimingCopierService>();
        services.AddSingleton<ITimingHelperService, TimingHelperService>();
        services.AddSingleton<ITumourGeneratorService, TumourGeneratorService>();

        services.AddSingleton<IGeometryDashboardProcessDiscovery, WindowsOsuProcessDiscovery>();
        services.AddSingleton<IGeometryDashboardScreenService, WindowsGeometryDashboardScreenService>();
        services.AddSingleton<IGeometryDashboardWindowService, WindowsGeometryDashboardWindowService>();
        services.AddSingleton<IGeometryDashboardRuntime, WindowsGeometryDashboardRuntimeService>();
        services.AddSingleton<IGeometryDashboardInputService, WindowsGeometryDashboardInputService>();
        services.AddSingleton<IGeometryDashboardOverlayService, WindowsGeometryDashboardOverlayService>();

        services.AddSingleton<StableMemoryCurrentBeatmapLocator>();
        services.AddSingleton<StableMemoryLiveBeatmapReader>();
        services.AddSingleton<MtipcCurrentBeatmapLocator>();
        services.AddSingleton<MtipcLiveBeatmapReader>();
        services.AddSingleton<MtipcEditorReloadService>();
        services.AddSingleton<GosumemoryCurrentBeatmapLocator>();
        services.AddSingleton<LazerExternalEditBeatmapLocator>();

        services.AddSingleton<WindowsGeometryDashboardCoordinateContext>();
        services.AddSingleton<HitsoundStudioEngine>();

        services.AddSingleton<ILiveBeatmapReader>(provider => new ConfiguredLiveBeatmapReader(
            settings,
            provider.GetRequiredService<StableMemoryLiveBeatmapReader>(),
            provider.GetRequiredService<MtipcLiveBeatmapReader>()));

        services.AddSingleton<ICurrentBeatmapLocator>(provider => new ConfiguredCurrentBeatmapLocator(
            settings,
            provider.GetRequiredService<StableMemoryCurrentBeatmapLocator>(),
            provider.GetRequiredService<MtipcCurrentBeatmapLocator>(),
            provider.GetRequiredService<GosumemoryCurrentBeatmapLocator>(),
            provider.GetRequiredService<LazerExternalEditBeatmapLocator>()));

        services.AddSingleton<IEditorReloadService>(provider => new ConfiguredEditorReloadService(
            settings,
            OperatingSystem.IsWindows() ? new WindowsOsuEditorReloadService() : new UnsupportedPlatformEditorReloadService(),
            provider.GetRequiredService<MtipcEditorReloadService>()));

        services.AddSingleton<IBeatmapEditingGateway>(provider =>
        {
            var gateway = ActivatorUtilities.CreateInstance<BeatmapEditingGateway>(provider);
            gateway.FileSaved += saved;

            return gateway;
        });

        if (geometry is not null)
            services.AddSingleton(geometry);

        services.AddSingleton<IGeometryDashboardService, GeometryDashboardService>();

        return services.BuildServiceProvider();
    }
}
