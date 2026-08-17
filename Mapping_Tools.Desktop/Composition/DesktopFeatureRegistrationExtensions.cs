using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls.Primitives;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Composition;

static internal class DesktopFeatureRegistrationExtensions {
    public static IServiceCollection AddDesktopFeatures(this IServiceCollection services) {
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
            "auto-fail-detector",
            "Auto-fail Detector",
            "Detect incorrect object loading in overlapping patterns.",
            ["auto fail", "2b", "unloading", "objects"],
            startsSection: true,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto,
            quickRunTargets: QuickRunTargets.Always);
        services.AddMappingTool<MapCleanerViewModel>(
            "map-cleaner",
            "Map Cleaner",
            "Rebuild useful greenlines and optionally resnap map content.",
            ["clean", "greenline", "resnap", "samples"],
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto,
            quickRunTargets: QuickRunTargets.Always);
        services.AddMappingTool<RhythmGuideViewModel>(
            "rhythm-guide",
            "Rhythm Guide",
            "Make a beatmap with circles from the rhythm of multiple maps.",
            ["rhythm", "hitsound", "guide", "reference"],
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<HitsoundPreviewHelperViewModel>(
            "hitsound-preview-helper",
            "Hitsound Preview Helper",
            "Place provisional hitsounds from positional zones.",
            ["hitsound", "preview", "zone", "sample", "position"],
            verticalScrollBarVisibility: ScrollBarVisibility.Auto,
            quickRunTargets: QuickRunTargets.Always);
        services.AddMappingTool<HitsoundCopierViewModel>(
            "hitsound-copier",
            "Hitsound Copier",
            "Copy hitsounds, samples, and storyboard sounds between beatmaps.",
            ["hitsound", "copy", "sample", "storyboard", "mute", "multi-map"],
            verticalScrollBarVisibility: ScrollBarVisibility.Auto,
            quickRunTargets: QuickRunTargets.Always);
        services.AddMappingTool<MetadataManagerViewModel>(
            "metadata-manager",
            "Metadata Manager",
            "Edit metadata once and apply it to multiple beatmaps.",
            ["metadata", "artist", "title", "tags", "colours"],
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<PropertyTransformerViewModel>(
            "property-transformer",
            "Property Transformer",
            "Multiply and add to timing, object, bookmark, and storyboard properties.",
            ["properties", "transform", "timing", "offset", "multiplier"],
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<TimingCopierViewModel>(
            "timing-copier",
            "Timing Copier",
            "Copy timing between beatmaps with optional object resnapping.",
            ["timing", "copy", "resnap", "beat divisors", "multi-map"],
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);
        services.AddMappingTool<TimingHelperViewModel>(
            "timing-helper",
            "Timing Helper",
            "Adjust BPM and add redlines so selected markers become snapped.",
            ["timing", "redlines", "BPM", "markers", "beat divisors"],
            verticalScrollBarVisibility: ScrollBarVisibility.Auto,
            quickRunTargets: QuickRunTargets.Always);
        services.AddMappingTool<SliderCompletionatorViewModel>(
            "slider-completionator",
            "Slider Completionator",
            "Change slider length and duration while calculating slider velocity.",
            ["slider", "completion", "duration", "length", "velocity"],
            verticalScrollBarVisibility: ScrollBarVisibility.Auto,
            quickRunTargets: QuickRunTargets.AnySelection);
        services.AddMappingTool<SliderMergerViewModel>(
            "slider-merger",
            "Slider Merger",
            "Merge selected sliders and circles into one connected slider.",
            ["slider", "merge", "bezier", "connection", "circles"],
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto,
            quickRunTargets: QuickRunTargets.MultipleSelection);
        services.AddMappingTool<SliderPicturatorViewModel>(
            "slider-picturator",
            "Slider Picturator",
            "Generate a slider path that reproduces an imported image.",
            ["slider", "picture", "image", "picturator", "render"],
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto,
            quickRunTargets: QuickRunTargets.AnySelection);

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
        where TViewModel : ObservableObject {
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
        string id,
        string displayName,
        string description,
        IEnumerable<string> searchTerms,
        bool startsSection = false,
        ScrollBarVisibility horizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        QuickRunTargets? quickRunTargets = null)
        where TViewModel : ObservableObject {
        services.AddShellFeature<TViewModel>(
            id,
            displayName,
            "Tools",
            description,
            searchTerms,
            startsSection,
            horizontalScrollBarVisibility,
            verticalScrollBarVisibility);

        if (quickRunTargets is not null) {
            services.AddSingleton(provider => new MappingToolQuickRunRegistration(
                id,
                displayName,
                quickRunTargets.Value,
                cancellationToken => (provider.GetRequiredService<TViewModel>() as IQuickRun ??
                                      throw new InvalidOperationException(
                                          $"Feature '{id}' declares QuickRun but {typeof(TViewModel).Name} does not implement IQuickRun.")).RunQuickAsync(cancellationToken)
            ));
        }

        return services;
    }
}
