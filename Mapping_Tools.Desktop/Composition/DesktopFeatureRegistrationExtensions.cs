using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopFeatureRegistrationExtensions
{
    public static IServiceCollection AddDesktopFeatures(this IServiceCollection services)
    {
        services.AddShellFeature<GetStartedViewModel>(
            "get-started",
            "Get started",
            "Home",
            "Onboarding, bundled changelog, support links, and recent beatmaps.",
            ["home", "help", "changelog", "recent", "faq"]);
        services.AddShellFeature<PreferencesViewModel>(
            "preferences",
            "Preferences",
            "Application",
            "Paths, backup policy, Editor Reader, and application theme.",
            ["settings", "paths", "backups", "editor reader", "theme"],
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<AutoFailDetectorViewModel>(
            MappingToolDefinitions.AutoFailDetector,
            true,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<MapCleanerViewModel>(
            MappingToolDefinitions.MapCleaner,
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<RhythmGuideViewModel>(
            MappingToolDefinitions.RhythmGuide,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<HitsoundPreviewHelperViewModel>(
            MappingToolDefinitions.HitsoundPreviewHelper,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<HitsoundStudioViewModel>(
            MappingToolDefinitions.HitsoundStudio,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<HitsoundCopierViewModel>(
            MappingToolDefinitions.HitsoundCopier,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<MetadataManagerViewModel>(
            MappingToolDefinitions.MetadataManager,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<PropertyTransformerViewModel>(
            MappingToolDefinitions.PropertyTransformer,
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<TimingCopierViewModel>(
            MappingToolDefinitions.TimingCopier,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<TimingHelperViewModel>(
            MappingToolDefinitions.TimingHelper,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<SliderCompletionatorViewModel>(
            MappingToolDefinitions.SliderCompletionator,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<SliderMergerViewModel>(
            MappingToolDefinitions.SliderMerger,
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<SliderPicturatorViewModel>(
            MappingToolDefinitions.SliderPicturator,
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<SlideratorViewModel>(
            MappingToolDefinitions.Sliderator,
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<TumourGeneratorViewModel>(
            MappingToolDefinitions.TumourGenerator,
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<ComboColourStudioViewModel>(
            MappingToolDefinitions.ComboColourStudio,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<MapsetMergerViewModel>(
            MappingToolDefinitions.MapsetMerger,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<PatternGalleryViewModel>(
            MappingToolDefinitions.PatternGallery,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<GeometryDashboardViewModel>(
            MappingToolDefinitions.GeometryDashboard,
            verticalScrollBarVisibility: ScrollBarVisibility.Disabled);

        services.AddSingleton<IShellFeatureRegistry>(provider =>
            new ShellFeatureRegistry(
                provider.GetServices<ShellFeatureRegistration>()));

        return services;
    }

    public static IServiceCollection AddShellFeature<TViewModel>(
        this IServiceCollection services,
        string id,
        string displayName,
        string category,
        string description,
        IEnumerable<string> searchTerms,
        bool startsSection = false,
        ScrollBarVisibility horizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Disabled)
        where TViewModel : ObservableObject
    {
        services.AddSingleton<TViewModel>();
        services.AddSingleton(provider => new ShellFeatureRegistration(
            id,
            displayName,
            category,
            description,
            searchTerms,
            provider.GetRequiredService<TViewModel>,
            startsSection,
            horizontalScrollBarVisibility,
            verticalScrollBarVisibility));

        return services;
    }

    public static IServiceCollection AddMappingTool<TViewModel>(
        this IServiceCollection services,
        ToolDefinition definition,
        bool startsSection = false,
        ScrollBarVisibility horizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Disabled)
        where TViewModel : ObservableObject
    {
        ArgumentNullException.ThrowIfNull(definition);
        services.AddShellFeature<TViewModel>(
            definition.Id,
            definition.DisplayName,
            "Tools",
            definition.Description,
            definition.SearchTerms,
            startsSection,
            horizontalScrollBarVisibility,
            verticalScrollBarVisibility);

        if (definition.QuickRunTargets is not null)
            services.AddSingleton(provider => new MappingToolQuickRunRegistration(
                definition,
                cancellationToken => (provider.GetRequiredService<TViewModel>() as IQuickRun
                                      ?? throw new InvalidOperationException(
                                          $"Feature '{definition.Id}' declares QuickRun but {typeof(TViewModel).Name} does not implement IQuickRun.")).RunQuickAsync(cancellationToken)
            ));

        return services;
    }
}
